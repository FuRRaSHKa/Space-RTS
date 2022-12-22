using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public interface IBulletsController
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

    public Vector3 Direction => _direction;
    public Transform Transform => _bullet.transform;
    public float Velocity => _velocity; 

    public BulletWrapper(Vector3 direction, PoolObject bullet, ITargetable target, float lifeTime, float velocity, int damage)
    {
        _direction = direction;
        _bullet = bullet;
        _target = target;
        _lifeTime = lifeTime;
        _velocity = velocity;
        _damage = damage;
    }

    public void ExecuteHit()
    {
        _target.DealDamage(_damage);
    }

    public bool UpdateLifeTime()
    {
        _currentTime += Time.deltaTime;
        if(_currentTime > _lifeTime)
        {
            return true;
        }

        Death();
        return false;
    }

    private void Death()
    {
        _bullet.DisableObject();
    }
}

public class BulletsController : MonoBehaviour, IBulletsController
{
    private List<BulletWrapper> _shootedBulletStructs;

    public void AddBullet(BulletWrapper bulletWrapper)
    {
        _shootedBulletStructs.Add(bulletWrapper);
    }

    private void Update()
    {
        SheludeMoving();
        UpdateLifeTime();
    }

    private void SheludeMoving()
    {
        TransformAccessArray transformAccess = new TransformAccessArray(_shootedBulletStructs.Count);
        NativeArray<Vector3> directions = new NativeArray<Vector3>(_shootedBulletStructs.Count, Allocator.Temp);
        NativeArray<float> velocities = new NativeArray<float>(_shootedBulletStructs.Count, Allocator.Temp);
        float deltaTime = Time.deltaTime;

        for (int i = 0; i < _shootedBulletStructs.Count; i++)
        {
            transformAccess[i] = _shootedBulletStructs[i].Transform;
            directions[i] = _shootedBulletStructs[i].Direction;
            velocities[i] = _shootedBulletStructs[i].Velocity;
        }

        MoveJob job = new MoveJob()
        {
            directions = directions,
            velocities = velocities,
            deltaTime = deltaTime
        };

        JobHandle jobHandle = job.Schedule(transformAccess);
        jobHandle.Complete();
    }

    private void UpdateLifeTime()
    {
        for (int i = 0; i < _shootedBulletStructs.Count; i++)
        {
            if (_shootedBulletStructs[i].UpdateLifeTime())
            {
                _shootedBulletStructs.RemoveAt(i);
                i--;
            }
        }
    }
}

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
