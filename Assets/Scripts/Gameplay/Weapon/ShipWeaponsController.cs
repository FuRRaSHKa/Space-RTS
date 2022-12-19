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

    private void Awake()
    {
        _weaponFactory = new WeaponFactory();
    }

    public void Init(ShipInitilizationData data)
    {
        List<WeaponData> weaponDatas = new(data.ShipData.WeaponDatas);

        for (int i = 0; i < weaponDatas.Count && i < _gunPositions.Count; i++)
        {
            IWeapon weapon = _weaponFactory.CreateWeapon(weaponDatas[i], _gunPositions[i]);
            _weaponList.Add(weapon);
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