using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Data.Weapon;
using HalloGames.SpaceRTS.Gameplay.Guns;
using System;
using System.Collections.Generic;
using UnityEngine;
using IServiceProvider = HalloGames.Architecture.Services.IServiceProvider;

namespace HalloGames.SpaceRTS.Management.Factories
{
    public interface IWeaponFactory : IService
    {
        public IWeapon CreateWeapon(WeaponData weaponData, Transform parent);
    }

    public class WeaponFactory : IWeaponFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IReadOnlyDictionary<WeaponType, Action<WeaponInitilizer>> _factoryWeaponPairs;

        public WeaponFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _factoryWeaponPairs = new Dictionary<WeaponType, Action<WeaponInitilizer>>
            {
                [WeaponType.Projectile] = InitProjectileWeapon,
                [WeaponType.Rocket] = InitRocketWeapon
            };
        }

        public IWeapon CreateWeapon(WeaponData weaponData, Transform parent)
        {
            WeaponInitilizer weapon = UnityEngine.Object.Instantiate(weaponData.Prefab).GetComponent<WeaponInitilizer>();
            weapon.transform.SetParent(parent);
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.position = parent.position;

            weapon.Initialize(weaponData);
            if (_factoryWeaponPairs.TryGetValue(weaponData.WeaponType, out var action))
                action?.Invoke(weapon);

            return weapon.GetComponent<IWeapon>();
        }

        private void InitProjectileWeapon(WeaponInitilizer weapon)
        {
            IProjectileCreator projectileCreator = _serviceProvider.GetService<BulletSpawner>();
            weapon.GetComponent<SequenceProjectileShooter>().InitProjectileCreator(projectileCreator);
        }

        private void InitRocketWeapon(WeaponInitilizer weapon)
        {
            IProjectileCreator projectileCreator = _serviceProvider.GetService<RocketSpawner>();
            weapon.GetComponent<SequenceProjectileShooter>().InitProjectileCreator(projectileCreator);
        }
    }
}