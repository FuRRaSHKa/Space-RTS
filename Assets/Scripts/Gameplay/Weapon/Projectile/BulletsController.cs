using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

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

public class BulletWrapper
{
    private MoveData _moveData;
    private PoolObject _bullet;
    private ITargetable _target;

    private float _lifeTime;
    private float _currentTime;
    private int _damage;
    private int _colliderInstanceId;
    public event Action<Vector3, Vector3> OnHit;

    public MoveData MoveData => _moveData;
    public Transform Transform => _bullet.transform;
    public int ColliderInstanceId => _colliderInstanceId;

    public BulletWrapper(Vector3 direction, PoolObject bullet, ITargetable target, float lifeTime, float velocity, int damage)
    {
        _bullet = bullet;
        _target = target;
        _lifeTime = lifeTime;
        _damage = damage;

        _moveData= new MoveData(velocity, direction);
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
    private NativeArray<MoveData> _moveData;
    private NativeArray<Vector3> _positions;
    private NativeArray<Vector3> _deltas;

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
        _moveData = new NativeArray<MoveData>(_shootedBullets.Count, Allocator.TempJob);
        _positions = new NativeArray<Vector3>(_shootedBullets.Count, Allocator.TempJob);
        _deltas = new NativeArray<Vector3>(_shootedBullets.Count, Allocator.TempJob);
        _deltaTime = Time.deltaTime;

        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            _transformAccess.Add(_shootedBullets[i].Transform);
            _positions[i] = _shootedBullets[i].Transform.position;
            _moveData[i] = _shootedBullets[i].MoveData;
        }
    }

    private void DisposeData()
    {
        _moveData.Dispose();
        _transformAccess.Dispose();
        _results.Dispose();
        _deltas.Dispose();
        _positions.Dispose();
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
        _results = new NativeArray<RaycastHit>(raycastCommands.Length, Allocator.TempJob);

        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            raycastCommands[i] = new RaycastCommand(_positions[i], _deltas[i], _layerMask);
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
    [ReadOnly] public NativeArray<MoveData> moveDatas;
    [ReadOnly] public float deltaTime;

    [WriteOnly]
    public NativeArray<Vector3> deltas;

    public void Execute(int index, TransformAccess transform)
    {
        Vector3 pos = transform.position;
        Vector3 delta = moveDatas[index].direction * moveDatas[index].velocity * deltaTime;
       
        pos += delta;
        
        deltas[index] = delta;
        transform.position = pos;
    }
}
