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

    private float _deltaTime;

    public void AddBullet(BulletWrapper bulletWrapper)
    {
        _shootedBullets.Add(bulletWrapper);
    }


    private void Update()
    {
        SheludeMoving();
        Raycasts();
        UpdateLifeTime();
    }


    private void SheludeMoving()
    {
        _deltaTime = Time.deltaTime;

        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            _shootedBullets[i].Transform.position += _shootedBullets[i].Direction * _shootedBullets[i].Velocity * _deltaTime; 
        }
    }

    private void Raycasts()
    {
        for (int i = 0; i < _shootedBullets.Count; i++)
        {
            if (Physics.Raycast(_shootedBullets[i].Transform.position, -_shootedBullets[i].Direction, out RaycastHit result, _shootedBullets[i].Velocity * _deltaTime, _layerMask.value))
            {
                if (result.colliderInstanceID == _shootedBullets[i].ColliderInstanceId)
                {
                    _shootedBullets[i].ExecuteHit(result.point, result.normal);

                    _shootedBullets.RemoveAt(i);
                    i--;
                }
            }         
        }
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
