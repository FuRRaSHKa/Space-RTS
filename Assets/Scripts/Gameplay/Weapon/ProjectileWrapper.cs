using HalloGames.SpaceRTS.Gameplay.Targets;
using System;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Projectile
{
    public class ProjectileWrapper
    {
        private ProjectileObject _projectile;
        private Transform _projectileTransform;

        private float _lifeTime;
        private float _currentTime;

        private int _damage;
        private int _colliderInstanceId;
        private bool _toReturn;

        protected ITargetable target;

        public int ColliderInstanceId => _colliderInstanceId;
        public bool ToReturn => _toReturn;
        public Transform Transform => _projectileTransform;

        public event Action<Vector3, Vector3> OnHit;

        public ProjectileWrapper(ProjectileObject projectile, ITargetable target, float lifeTime, int damage)
        {
            _projectile = projectile;
            this.target = target;
            _lifeTime = lifeTime;
            _damage = damage;
            _projectileTransform = _projectile.transform;

            _colliderInstanceId = target.ColliderID;

            _projectile.EnableObject();
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
            _projectile.DisableObject();
        }
    }

    public class BulletWrapper : ProjectileWrapper
    {
        private float _velocity;
        private Vector3 _direction;

        public Vector3 Direction => _direction;
        public float Velocity => _velocity;

        public BulletWrapper(ProjectileObject projectile, ITargetable target, float lifeTime, int damage, Vector3 direction, float velocity) : base(projectile, target, lifeTime, damage)
        {
            _direction = direction;
            _velocity = velocity;
        }
    }

    public class RocketWrapper : ProjectileWrapper
    {
        private readonly float _maxRotationSpeed;
        private readonly float _maxSpeed;
        private readonly float _acceleration;
        private readonly float _rotationAcceleration;

        private float _currentSpeed;
        private float _currentRotationSpeed;
        private Vector3 _direction;

        public Vector3 TargetPos => target.TargetTransform.position;
        public RocketMovementStruct RocketMovement => new RocketMovementStruct(_maxRotationSpeed, _maxSpeed, _acceleration, _currentSpeed, _rotationAcceleration, _currentRotationSpeed, _direction);

        public RocketWrapper(ProjectileObject projectile, ITargetable target, float lifeTime, int damage, float rotationSpeed, float maxSpeed, float acceleration, float rotationAcceleration, float startSpeed, Vector3 direction) : base(projectile, target, lifeTime, damage)
        {
            _rotationAcceleration = rotationAcceleration;
            _acceleration = acceleration;
            _maxRotationSpeed = rotationSpeed;
            _maxSpeed = maxSpeed;
            _direction = direction * startSpeed;
            _currentSpeed = startSpeed;
        }

        public void MoveRocket(Vector3 direction, float currentSpeed, float currentRotationSpeed)
        {
            _currentRotationSpeed = currentRotationSpeed;
            _currentSpeed = currentSpeed;
            _direction = direction;
        }
    }

    public readonly struct RocketMovementStruct
    {
        public readonly float maxRotationSpeed;
        public readonly float maxSpeed;
        public readonly float acceleration;
        public readonly float currentSpeed;
        public readonly float currentRotationSpeed;
        public readonly float rotationAcceleration;
        public readonly Vector3 direction;

        public RocketMovementStruct(float maxRotationSpeed, float maxSpeed, float acceleration, float currentSpeed, float rotationAcceleration, float currentRotationSpeed, Vector3 direction)
        {
            this.currentRotationSpeed = currentRotationSpeed;
            this.rotationAcceleration = rotationAcceleration;
            this.currentSpeed = currentSpeed;
            this.acceleration = acceleration;
            this.maxRotationSpeed = maxRotationSpeed;
            this.maxSpeed = maxSpeed;
            this.direction = direction;
        }
    }
}