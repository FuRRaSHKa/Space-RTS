using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipGunController : MonoBehaviour, IInitilizable<ShipInitilizationData>
{
    [SerializeField] private List<Transform> _gunPositions;

    private List<IWeapon> _weaponList = new List<IWeapon>();
    private IWeaponFactory _weaponFactory;

    public void InstallService(IWeaponFactory weaponFactory)
    {
        _weaponFactory = weaponFactory;
    }

    public void Init(ShipInitilizationData data)
    {
        List<WeaponData> weaponDatas = new(data.ShipData.WeaponDatas);

        for (int i = 0; i < weaponDatas.Count; i++)
        {
            IWeapon weapon = _weaponFactory.CreateWeapon(weaponDatas[i], _gunPositions[i]);
            _weaponList.Add(weapon);
        }
    }

    public void Shoot()
    {

    }

    public void StopShooting()
    {

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