using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponFactory
{
    public IWeapon CreateWeapon(WeaponData weaponData, Transform parent);
}

public class WeaponFactory : IWeaponFactory
{
    public IWeapon CreateWeapon(WeaponData weaponData, Transform parent)
    {
        WeaponInitilizer weapon = PoolManager.Instance[weaponData.PoolObject].GetObject().GetComponent<WeaponInitilizer>();
        weapon.transform.SetParent(parent);
        weapon.transform.localRotation = Quaternion.identity;
        weapon.transform.position = parent.position;

        weapon.Initilize(weaponData);
        return weapon.GetComponent<IWeapon>();
    }
}
