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

public readonly struct MoveData
{
    public readonly float velocity;
    public readonly Vector3 direction;

    public MoveData(float velocity, Vector3 direction)
    {
        this.velocity = velocity;
        this.direction = direction;
    }
}

public readonly struct DeltaMoveData
{
    public readonly Vector3 position;
    public readonly Vector3 delta;

    public DeltaMoveData(Vector3 position, Vector3 delta)
    {
        this.position = position;
        this.delta = delta;
    }
}

public class BulletWrapper
{
    private MoveData _moveData;
    private PoolObject _bullet;
    private ITargetable _target;

    private float _lifeTime;
    private float _currentTime;
    private int _damage;
    private int _colliderInstanceId;
    private bool _toReturn;

    public bool ToReturn => _toReturn;
    public MoveData MoveData => _moveData;
    public Transform Transform => _bullet.transform;
    public int ColliderInstanceId => _colliderInstanceId;

    public event Action<Vector3, Vector3> OnHit;

    public BulletWrapper(Vector3 direction, PoolObject bullet, ITargetable target, float lifeTime, float velocity, int damage)
    {
        _bullet = bullet;
        _target = target;
        _lifeTime = lifeTime;
        _damage = damage;

        _moveData = new MoveData(velocity, direction);
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
    private TransformAccessArray _transformAccess;
    private NativeArray<MoveData> _moveData;
    private NativeArray<DeltaMoveData> _deltas;
    private NativeArray<int> _colliderIDs;

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
        _transformAccess = new TransformAccessArray(_shootedBullets.Count);
        _moveData = new NativeArray<MoveData>(_shootedBullets.Count, Allocator.TempJob);
        _deltas = new NativeArray<DeltaMoveData>(_shootedBullets.Count, Allocator.TempJob);
        _colliderIDs = new NativeArray<int>(_shootedBullets.Count, Allocator.TempJob);
        _deltaTime = Time.deltaTime;

        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            _colliderIDs[i] = _shootedBullets[i].ColliderInstanceId;
            _transformAccess.Add(_shootedBullets[i].Transform);
            _moveData[i] = _shootedBullets[i].MoveData;
        }
    }

    private void DisposeData()
    {
        _colliderIDs.Dispose();
        _moveData.Dispose();
        _transformAccess.Dispose();
        _results.Dispose();
        _deltas.Dispose();
    }

    private void SheludeMoving()
    {
        MoveJob job = new MoveJob()
        {
            moveDatas = _moveData,
            deltaTime = _deltaTime,
            deltas = _deltas
        };

        JobHandle jobHandle = job.Schedule(_transformAccess);
        jobHandle.Complete();
    }

    private void Raycasts()
    {
        NativeArray<RaycastCommand> raycastCommands = new NativeArray<RaycastCommand>(_shootedBullets.Count, Allocator.TempJob);
        NativeList<int> filtered = new NativeList<int>(_shootedBullets.Count, Allocator.TempJob);

        _results = new NativeArray<RaycastHit>(raycastCommands.Length, Allocator.TempJob);

        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            raycastCommands[i] = new RaycastCommand(_deltas[i].position, _deltas[i].delta, _layerMask);
        }

        RaycastResultJob raycastResultJob = new RaycastResultJob()
        {
            results = _results,
            colliderIDs = _colliderIDs,

            filtered = filtered.AsParallelWriter()
        };

        var raycasts = RaycastCommand.ScheduleBatch(raycastCommands, _results, 1);
        JobHandle jobHandle = raycastResultJob.Schedule(_shootedBullets.Count, 32, raycasts);

        raycasts.Complete();
        jobHandle.Complete();

        int count = filtered.Length;
        for (int i = 0; i < count; i++)
        {
            int id = filtered[i];
            _shootedBullets[id].ExecuteHit(_results[id].point, _results[id].normal);
        }

        filtered.Dispose();
        raycastCommands.Dispose();
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
public struct MoveJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<MoveData> moveDatas;
    [ReadOnly] public float deltaTime;

    [WriteOnly] public NativeArray<DeltaMoveData> deltas;

    public void Execute(int index, TransformAccess transform)
    {
        Vector3 pos = transform.position;
        Vector3 delta = moveDatas[index].direction * moveDatas[index].velocity * deltaTime;

        deltas[index] = new DeltaMoveData(pos, delta);
        pos += delta;

        transform.position = pos;
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