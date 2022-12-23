using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public interface IBulletsController : IService
{
    public void AddBullet(BulletWrapper shootedBulletStruct);
}

public class BulletWrapper
{
    private Vector3 _direction;
    private PoolObject _bullet;
    private ITargetable _target;

    private float _lifeTime;
    private float _currentTime;
    private float _velocity;
    private int _damage;
    private int _colliderInstanceId;
    public event Action<Vector3, Vector3> OnHit;

    public Vector3 Direction => _direction;
    public Transform Transform => _bullet.transform;
    public float Velocity => _velocity;
    public int ColliderInstanceId => _colliderInstanceId;

    public BulletWrapper(Vector3 direction, PoolObject bullet, ITargetable target, float lifeTime, float velocity, int damage)
    {
        _direction = direction;
        _bullet = bullet;
        _target = target;
        _lifeTime = lifeTime;
        _velocity = velocity;
        _damage = damage;

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
        _bullet.DisableObject();
    }
}

public class BulletsController : MonoBehaviour, IBulletsController
{
    [SerializeField] private LayerMask _layerMask;

    private List<BulletWrapper> _shootedBullets = new List<BulletWrapper>();

    private NativeArray<RaycastHit> _results;
    private TransformAccessArray _transformAccess;
    private NativeArray<Vector3> _directions;
    private NativeArray<float> _velocities;
    private float _deltaTime;

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
        _transformAccess = new TransformAccessArray(_shootedBullets.Count);
        _directions = new NativeArray<Vector3>(_shootedBullets.Count, Allocator.TempJob);
        _velocities = new NativeArray<float>(_shootedBullets.Count, Allocator.TempJob);
    }

    private void DisposeData()
    {
        _velocities.Dispose();
        _directions.Dispose();
        _transformAccess.Dispose();
        _results.Dispose();
    }

    private void SheludeMoving()
    {
        _deltaTime = Time.deltaTime;

        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            _transformAccess.Add(_shootedBullets[i].Transform);
            _directions[i] = _shootedBullets[i].Direction;
            _velocities[i] = _shootedBullets[i].Velocity;
        }

        MoveJob job = new MoveJob()
        {
            directions = _directions,
            velocities = _velocities,
            deltaTime = _deltaTime
        };

        JobHandle jobHandle = job.Schedule(_transformAccess);
        jobHandle.Complete();
    }

    private void Raycasts()
    {
        NativeArray<RaycastCommand> raycastCommands = new NativeArray<RaycastCommand>(_shootedBullets.Count, Allocator.TempJob);
        _results = new NativeArray<RaycastHit>(raycastCommands.Length, Allocator.TempJob);

        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            raycastCommands[i] = new RaycastCommand(_transformAccess[i].position, _directions[i] * _velocities.Length * _deltaTime, _layerMask);
        }

        var raycasts = RaycastCommand.ScheduleBatch(raycastCommands, _results, 1);
        raycasts.Complete();

        List<BulletWrapper> toDelete = new List<BulletWrapper>();
        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            if (_results[i].colliderInstanceID == _shootedBullets[i].ColliderInstanceId)
            {
                _shootedBullets[i].ExecuteHit(_results[i].point, _results[i].normal);
                toDelete.Add(_shootedBullets[i]);
            }
        }

        int step = 0;
        for (int i = 0; i < toDelete.Count; i++)
        {
            _shootedBullets.Remove(toDelete[i]);
            step++;
        }

        raycastCommands.Dispose();
    }

    private void UpdateLifeTime()
    {
        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            if (_shootedBullets[i].UpdateLifeTime())
            {
                _shootedBullets.RemoveAt(i);
                i--;
            }
        }
    }
}

[BurstCompile]
public struct MoveJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<Vector3> directions;
    [ReadOnly] public NativeArray<float> velocities;
    [ReadOnly] public float deltaTime;

    public void Execute(int index, TransformAccess transform)
    {
        Vector3 pos = transform.position;
        pos += directions[index] * velocities[index] * deltaTime;
        transform.position = pos;
    }
}
