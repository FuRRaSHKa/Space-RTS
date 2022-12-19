using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipEntity : MonoBehaviour, IInitilizable<ShipInitilizationData>
{
    private IMovementController _shipMovementController;
    private ITargetable _shipTarget;
    private IStatsController _statsController;
    private IWeaponController _weaponController;

    private SideData _side;
    private ShipData _shipData;

    public SideData Side => _side;
    public IMovementController ShipMovement => _shipMovementController;
    public ITargetable ShipTarget => _shipTarget;
    public IStatsController StatsController => _statsController;
    public ShipData ShipData => _shipData;
    public IWeaponController WeaponController => _weaponController;

    public void Init(ShipInitilizationData data)
    {
        _side = data.SideData;
        _shipData = data.ShipData;
    }

    private void Awake()
    {
        _weaponController = GetComponent<IWeaponController>();
        _shipMovementController = GetComponent<IMovementController>();
        _shipTarget = GetComponent<ITargetable>();
        _statsController = GetComponent<IStatsController>();
    }
}