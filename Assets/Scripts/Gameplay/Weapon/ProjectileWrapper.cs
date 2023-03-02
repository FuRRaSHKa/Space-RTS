using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class ProjectileWrapper : MonoBehaviour
{
    private PoolObject _projectel;
    private Transform _projectelTransform;

    private float _lifeTime;
    private float _currentTime;

    private int _damage;
    private int _colliderInstanceId;
    private bool _toReturn;

    protected ITargetable target;

    public int ColliderInstanceId => _colliderInstanceId;
    public bool ToReturn => _toReturn;
    public Transform Transform => _projectelTransform;

    public event Action<Vector3, Vector3> OnHit;

    public ProjectileWrapper(PoolObject projectel, ITargetable target, float lifeTime, int damage)
    {
        _projectel = projectel;
        this.target = target;
        _lifeTime = lifeTime;
        _damage = damage;
        _projectelTransform = _projectel.transform;

        _colliderInstanceId = target.ColliderID;
    }

    public void ExecuteHit(Vector3 point, Vector3 normal)
    {
        OnHit?.Invoke(point, normal);
        target.DealDamage(_damage);
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
        _projectel.DisableObject();
    }
}

public class BulletWrapper : ProjectileWrapper
{
    private float _velocity;
    private Vector3 _direction;

    public Vector3 Direction => _direction;
    public float Velocity => _velocity;

    public BulletWrapper(PoolObject projectel, ITargetable target, float lifeTime, int damage, Vector3 direction, float velocity) : base(projectel, target, lifeTime, damage)
    {
        _direction = direction;
        _velocity = velocity;
    }
}

public class RocketWeapper : ProjectileWrapper
{
    private float _inertion;
    private float _maxVelocity;
    private Vector3 _direction;

    public Vector3 TargetPos => target.TargetTransform.position;
    public RocketMovementStruct RocketMovement => new RocketMovementStruct(_inertion, _maxVelocity, _direction);

    public RocketWeapper(PoolObject projectel, ITargetable target, float lifeTime, int damage, float inertion, float maxVeloctity, Vector3 direction) : base(projectel, target, lifeTime, damage)
    {
        _inertion = inertion;
        _maxVelocity = maxVeloctity;
        _direction = direction;
    }
}

public readonly struct RocketMovementStruct
{
    public readonly float inertion;
    public readonly float maxVelocity;
    public readonly Vector3 currentVelocity;

    public RocketMovementStruct(float inertion, float maxVelocity, Vector3 currentVelocity)
    {
        this.inertion = inertion;
        this.maxVelocity = maxVelocity;
        this.currentVelocity = currentVelocity;
    }
}