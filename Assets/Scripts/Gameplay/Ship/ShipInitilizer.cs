using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipInitilizer : AbstractInitilizer<ShipInitilizationData>
{
    private ShipEntity _entity;
    private ShipGunController _shipGun;

    protected override void Awake()
    {
        _shipGun = GetComponent<ShipGunController>();
    }

    public void InitilizeServices(IWeaponFactory weaponFactory)
    {
        _shipGun.InstallService(weaponFactory);
    }
}

public readonly struct ShipInitilizationData
{
    public readonly SideData SideData;
    public readonly ShipData ShipData;

    public ShipInitilizationData(ShipData shipData, SideData sideData)
    {
        SideData = sideData;
        ShipData = shipData;
    }
}