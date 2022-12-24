using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using Debug = UnityEngine.Debug;
using Unity.Jobs.LowLevel.Unsafe;

public interface IBulletsController : IService
{
    public void AddBullet(BulletWrapper shootedBulletStruct);
}

[System.Serializable]
public class BulletWrapper
{
    private PoolObject _bullet;
    private ITargetable _target;

    private float _lifeTime;
    private float _currentTime;
    private float _velocity;
    private Vector3 _direction;

    private int _damage;
    private int _colliderInstanceId;
    private bool _toReturn;

    public Vector3 Direction => _direction;
    public float Velocity => _velocity;
    public bool ToReturn => _toReturn;
    public Transform Transform => _bullet.transform;
    public int ColliderInstanceId => _colliderInstanceId;

    public event Action<Vector3, Vector3> OnHit;

    public BulletWrapper(Vector3 direction, PoolObject bullet, ITargetable target, float lifeTime, float velocity, int damage)
    {
        _bullet = bullet;
        _target = target;
        _lifeTime = lifeTime;
        _damage = damage;
        _direction= direction;
        _velocity = velocity;

        _colliderInstanceId = target.ColliderID;
    }

    public void ExecuteHit(Vector3 point, Vector3 normal)
    {
        OnHit?.Invoke(point, normal);
        _target.DealDamage(_damage);
        Death();
    }

    public bool UpdateLifeTime()
    {
        _currentTime += Time.deltaTime;
        if (_currentTime > _lifeTime)
        {
            Death();
            return true;
        }

        return false;
    }

    private void Death()
    {
        _toReturn = true;
        _bullet.DisableObject();
    }
}

public class BulletsController : MonoBehaviour, IBulletsController
{
    [SerializeField] private LayerMask _layerMask;

    private List<BulletWrapper> _shootedBullets = new List<BulletWrapper>();

    private NativeArray<RaycastHit> _results;
    private NativeArray<int> _colliderIDs;
    private NativeArray<RaycastCommand> _raycastCommands;

    private float _deltaTime;
    private int _prevBulletCount;

    public void AddBullet(BulletWrapper bulletWrapper)
    {
        _shootedBullets.Add(bulletWrapper);
    }

    private void Update()
    {
        CollectData();
        SheludeMoving();
        Raycasts();
        UpdateLifeTime();
        DisposeData();
    }

    private void CollectData()
    {
        _colliderIDs = new NativeArray<int>(_shootedBullets.Count, Allocator.TempJob);
        _raycastCommands = new NativeArray<RaycastCommand>(_shootedBullets.Count, Allocator.TempJob);
        _results = new NativeArray<RaycastHit>(_raycastCommands.Length, Allocator.TempJob);
        _deltaTime = Time.deltaTime;

        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            _colliderIDs[i] = _shootedBullets[i].ColliderInstanceId;
        }
    }

    private void DisposeData()
    {
        _colliderIDs.Dispose();
        _results.Dispose();
        _raycastCommands.Dispose();
    }

    private void SheludeMoving()
    {
        _deltaTime = Time.deltaTime;

        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            Vector3 pos = _shootedBullets[i].Transform.position;
            Vector3 moveDelta = _shootedBullets[i].Direction * _shootedBullets[i].Velocity * _deltaTime;

            _raycastCommands[i] = new RaycastCommand(pos, moveDelta, moveDelta.magnitude, _layerMask.value);
            

            _shootedBullets[i].Transform.position += moveDelta;
        }
    }

    private void Raycasts()
    {
        NativeList<int> filtered = new NativeList<int>(_shootedBullets.Count, Allocator.TempJob);

        RaycastResultJob raycastResultJob = new RaycastResultJob()
        {
            results = _results,
            colliderIDs = _colliderIDs,

            filtered = filtered.AsParallelWriter()
        };

        var raycasts = RaycastCommand.ScheduleBatch(_raycastCommands, _results, 1);
        JobHandle jobHandle = raycastResultJob.Schedule(_shootedBullets.Count, 32, raycasts);

        raycasts.Complete();
        jobHandle.Complete();

        for (int i = 0; i < filtered.Length; i++)
        {
            int id = filtered[i];
            _shootedBullets[id].ExecuteHit(_results[id].point, _results[id].normal);
        }

        filtered.Dispose();
    }

    private void UpdateLifeTime()
    {
        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            if (_shootedBullets[i].UpdateLifeTime() || _shootedBullets[i].ToReturn)
            {
                _shootedBullets.RemoveAt(i);
                i--;
            }
        }
    }
}

[BurstCompile]
public struct RaycastResultJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<RaycastHit> results;
    [ReadOnly] public NativeArray<int> colliderIDs;

    [WriteOnly] public NativeList<int>.ParallelWriter filtered;

    public void Execute(int index)
    {
        if (results[index].colliderInstanceID == colliderIDs[index])
        {
            filtered.AddNoResize(index);
        }
    }
}