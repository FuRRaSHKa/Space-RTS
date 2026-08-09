using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Data.Weapon;
using HalloGames.SpaceRTS.Gameplay.Guns;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Factories
{
    public interface IWeaponFactory : IService
    {
        public IWeapon CreateWeapon(WeaponData weaponData, Transform parent);
    }

    public class WeaponFactory : IWeaponFactory
    {
        private IServiceProvider _serviceProvider;

        public WeaponFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWeapon CreateWeapon(WeaponData weaponData, Transform parent)
        {
            WeaponInitilizer weapon = Object.Instantiate(weaponData.Prefab).GetComponent<WeaponInitilizer>();
            weapon.transform.SetParent(parent);
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.position = parent.position;

            weapon.Initialize(weaponData);
            if (weaponData.WeaponType == WeaponType.Projectile)
            {
                IProjectileCreator projectileCreator = _serviceProvider.GetService<BulletSpawner>();
                weapon.GetComponent<SequenceProjectileShooter>().InitProjectileCreator(projectileCreator);
            }
            else if (weaponData.WeaponType == WeaponType.Rocket)
            {
                IProjectileCreator projectileCreator = _serviceProvider.GetService<RocketSpawner>();
                weapon.GetComponent<SequenceProjectileShooter>().InitProjectileCreator(projectileCreator);
            }

            return weapon.GetComponent<IWeapon>();
        }
    }
}