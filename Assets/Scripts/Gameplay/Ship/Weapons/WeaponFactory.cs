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
        throw new System.NotImplementedException();
    }
}
