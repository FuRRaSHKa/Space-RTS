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
            IProjecterCreator projecterCreator = new BulletSpawner(_serviceProvider.GetService<BulletsController>());
            weapon.GetComponent<ProjectileShooter>().InitProjectileCreator(projecterCreator);
        }

        return weapon.GetComponent<IWeapon>();
    }
}
