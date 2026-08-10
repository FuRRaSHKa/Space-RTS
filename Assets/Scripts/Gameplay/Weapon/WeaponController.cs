using HalloGames.Architecture.Frames;
using HalloGames.Architecture.Initializer;
using HalloGames.SpaceRTS.Data.Weapon;
using HalloGames.SpaceRTS.Gameplay.Guns.Targeter;
using HalloGames.SpaceRTS.Gameplay.Targets;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace HalloGames.SpaceRTS.Gameplay.Guns
{
    public class WeaponController : MonoBehaviour, IWeapon, ILogicTickable, IInitializable<WeaponData>
    {
        private float _shootTime;
        private float _maxAngleDeviation;
        private float _distance;

        private ITargetable _target;
        private IShooter _shooter;
        private IWeaponTargeter _weaponTargeter;

        private float _currentTime;

        public event Action<ITargetable> OnStartShooting;

        private void Awake()
        {
            _shooter = GetComponent<IShooter>();
            _weaponTargeter = GetComponent<IWeaponTargeter>();

            Assert.IsNotNull(_shooter, $"{nameof(WeaponController)}: no {nameof(IShooter)} on this object");
            Assert.IsNotNull(_weaponTargeter, $"{nameof(WeaponController)}: no {nameof(IWeaponTargeter)} on this object");
        }

        public void Init(WeaponData data)
        {
            _shootTime = data.ShootTime;
            _maxAngleDeviation = data.MaxAngleDeviation;
            _distance = data.Distance;
        }

        private void OnEnable()
        {
            TickManager.RegisterLogic(this);
        }

        private void OnDisable()
        {
            TickManager.UnregisterLogic(this);
        }

        public void StartShooting(ITargetable targetable)
        {
            _target = targetable;
            _weaponTargeter.StartFollowing(targetable);
        }

        public void StopShooting()
        {
            _weaponTargeter.StopFollowing();
            _target = null;
        }

        public void Tick(float deltaTime)
        {
            if (_currentTime > _shootTime)
            {
                if (_target == null)
                    return;

                if (_weaponTargeter.AngleDelta <= _maxAngleDeviation && _distance > (_target.TargetTransform.position - transform.position).magnitude)
                    Shoot();

                return;
            }

            _currentTime += deltaTime;
        }

        private void Shoot()
        {
            OnStartShooting?.Invoke(_target);

            _currentTime = 0;
            _shooter.Shoot(_target);
        }
    }

}

