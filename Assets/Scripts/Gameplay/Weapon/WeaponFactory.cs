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

            weapon.Initilize(weaponData);
            if (weaponData.WeaponType == WeaponType.Projectile)
            {
                IProjectileCreator projecterCreator = _serviceProvider.GetService<BulletSpawner>();
                weapon.GetComponent<SequenceProjectileShooter>().InitProjectileCreator(projecterCreator);
            }
            else if (weaponData.WeaponType == WeaponType.Rocket)
            {
                IProjectileCreator projecterCreator = _serviceProvider.GetService<RocketSpawner>();
                weapon.GetComponent<SequenceProjectileShooter>().InitProjectileCreator(projecterCreator);
            }

            return weapon.GetComponent<IWeapon>();
        }
    }
}