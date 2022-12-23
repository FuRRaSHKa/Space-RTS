using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponController
{
    public void Shoot(ITargetable targetable);
    public void StopShooting();
}

public class ShipWeaponsController : MonoBehaviour, IInitilizable<ShipInitilizationData>, IWeaponController
{
    [SerializeField] private List<Transform> _gunPositions;

    private List<IWeapon> _weaponList = new List<IWeapon>();
    private IWeaponFactory _weaponFactory;

    private void OnDrawGizmos()
    {
        foreach (var postion in _gunPositions)
        {
            Ray r = new Ray(postion.position, postion.up);
            Gizmos.DrawRay(r);

            r = new Ray(postion.position, postion.forward);
            Gizmos.DrawRay(r);
        }
    }

    public void InitWeaponeFactory(IWeaponFactory weaponFactory)
    {
        _weaponFactory = weaponFactory;
    }

    public void Init(ShipInitilizationData data)
    {
        WeaponData weaponData = data.ShipData.WeaponData;
        for (int i = 0; i < _gunPositions.Count; i++)
        {
            IWeapon weapon = _weaponFactory.CreateWeapon(weaponData, _gunPositions[i]);
            _weaponList.Add(weapon);
            weapon.OnShooting += CheckTargetDeath;
        }
    }

    public void Shoot(ITargetable targetable)
    {
        foreach (var weapon in _weaponList)
        {
            weapon.StartShooting(targetable);
        }
    }

    public void StopShooting()
    {
        foreach (var weapon in _weaponList)
        {
            weapon.StopShooting();
        }
    }

    private void CheckTargetDeath(ITargetable targetable)
    {
        if (targetable.TargetDataObservable.IsDead)
        {
            StopShooting();
        }
    }
}

public readonly struct WeaponSpawnData
{
    public readonly WeaponData WeaponData;
    public readonly Transform Parent;

    public WeaponSpawnData(WeaponData weaponData, Transform parent)
    {
        WeaponData = weaponData;
        Parent = parent;
    }
}