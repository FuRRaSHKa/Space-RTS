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
    public class SequenceProjectileShooter : MonoBehaviour, IShooter, IInitilizable<WeaponData>
    {
        [SerializeField] private List<Transform> _spawnPoints;
        [SerializeField] private ProjectelData _bulletData;

        private int _damage;
        private int _spawnPointId = 0;

        private IProjectileCreator _projecterCreator;

        public event Action<Vector3, Vector3> OnDealDamage;
        public event Action<ITargetable> OnShooting;

        public void Init(WeaponData data)
        {
            _damage = data.Damage;
        }

        public void InitProjectileCreator(IProjectileCreator creator)
        {
            _projecterCreator = creator;
        }

        public void BulletHit(Vector3 point, Vector3 normal)
        {
            OnDealDamage?.Invoke(point, normal);
        }

        public void Shoot(ITargetable targetable)
        {
            _projecterCreator.InstantiateProjectile(_bulletData, targetable, _spawnPoints[_spawnPointId], _damage, _spawnPoints[_spawnPointId].forward)
                .OnHit += BulletHit;

            OnShooting?.Invoke(targetable);
            _spawnPointId++;
            _spawnPointId %= _spawnPoints.Count;
        }
    }
}
