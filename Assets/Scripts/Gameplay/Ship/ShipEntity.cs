using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipEntity : MonoBehaviour, IInitilizable<ShipInitilizationData>
{
    private IMovementController _shipMovementController;
    private ITargetController _shipTargetController;
    private IStatsController _statsController;

    private SideData _side;
    private ShipData _shipData;

    public SideData Side => _side;
    public IMovementController ShipMovement => _shipMovementController;
    public ITargetController ShipTarget => _shipTargetController;
    public IStatsController StatsController => _statsController;
    public ShipData ShipData => _shipData;

    public void Init(ShipInitilizationData data)
    {
        _side = data.SideData;
        _shipData = data.ShipData;
    }

    private void Awake()
    {
        _shipMovementController = GetComponent<IMovementController>();
        _shipTargetController = GetComponent<ITargetController>();
        _statsController = GetComponent<IStatsController>();
    }
}