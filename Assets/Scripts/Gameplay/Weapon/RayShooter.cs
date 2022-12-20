using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IShooter
{
    public void Shoot(ITargetable targetable);
}

public class RayShooter : MonoBehaviour, IShooter, IInitilizable<WeaponData>
{
    private int _damage;

    public void Init(WeaponData data)
    {
        _damage = data.Damage;
    }

    public void Shoot(ITargetable targetable)
    {
        targetable.DealDamage(_damage);
    }
}
