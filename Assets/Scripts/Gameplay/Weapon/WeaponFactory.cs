using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        if (weaponData.WeaponeType == WeaponeType.Projectile)
        {
            IProjecterCreator projecterCreator = _serviceProvider.GetService<BulletSpawner>();
            weapon.GetComponent<ProjectileShooter>().InitProjectileCreator(projecterCreator);
        }
        else if (weaponData.WeaponeType == WeaponeType.Rocket)
        {
            IProjecterCreator projecterCreator = _serviceProvider.GetService<RocketSpawner>();
            weapon.GetComponent<ProjectileShooter>().InitProjectileCreator(projecterCreator);
        }

        return weapon.GetComponent<IWeapon>();
    }
}
