using HalloGames.Architecture.Initilizer;
using HalloGames.SpaceRTS.Data.Projectel;
using HalloGames.SpaceRTS.Data.Weapon;
using HalloGames.SpaceRTS.Gameplay.Guns.Targeter;
using HalloGames.SpaceRTS.Gameplay.Targets;
using HalloGames.SpaceRTS.Management.Factories;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Guns
{
    public class BurstProjectelShooter : MonoBehaviour, IShooter, IInitilizable<WeaponData>
    {
        [SerializeField] private List<Transform> _spawnPoints;
        [SerializeField] private ProjectelData _bulletData;

        private int _damage;

        private IProjecterCreator _projecterCreator;

        public event Action<Vector3, Vector3> OnDealDamage;
        public event Action<ITargetable> OnShooting;

        public void Init(WeaponData data)
        {
            _damage = data.Damage;
        }

        public void InitProjectileCreator(IProjecterCreator creator)
        {
            _projecterCreator = creator;
        }

        public void BulletHit(Vector3 point, Vector3 normal)
        {
            OnDealDamage?.Invoke(point, normal);
        }

        public void Shoot(ITargetable targetable)
        {
            for (int spawnPointId = 0; spawnPointId < _spawnPoints.Count; spawnPointId++)
            {
                _projecterCreator.InstantiateProjectile(_bulletData, targetable, _spawnPoints[spawnPointId], _damage, _spawnPoints[spawnPointId].forward)
                    .OnHit += BulletHit;

                OnShooting?.Invoke(targetable);
            }
        }
    }
}