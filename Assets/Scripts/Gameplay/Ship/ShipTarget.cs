using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipTarget : MonoBehaviour, ITargetable
{
    private ShipEntity _shipEntity;
    private ITargetDataObserver _shipDataObserver;

    public Transform TargetTransform => transform;

    public ITargetDataObserver TargetDataObservable => _shipDataObserver;

    public SideData Side => _shipEntity.Side;

    private void Awake()
    {
        _shipEntity = GetComponent<ShipEntity>();
    }

    private void Start()
    {
        _shipDataObserver = new ShipDataObserver(_shipEntity.StatsController, _shipEntity.ShipMovement);
    }

    public void DealDamage(int damage)
    {
        _shipEntity.StatsController.DealDamage(damage);
    }
}

public class ShipDataObserver : ITargetDataObserver
{
    private readonly IStatsController _statsController;
    private readonly IMovementController _movementController;

    public ShipDataObserver(IStatsController statsController, IMovementController movementController)
    {
        _statsController = statsController;
        _movementController = movementController;
    }

    public Vector3 CurrentVelocity => throw new System.NotImplementedException();
    public bool IsDead => _statsController.IsDeath;
}

public interface ITargetDataObserver
{
    public Vector3 CurrentVelocity
    {
        get;
    }
    public bool IsDead
    {
        get;
    }
}

public interface ITargetable
{
    public Transform TargetTransform
    {
        get;
    }
    public ITargetDataObserver TargetDataObservable
    {
        get;
    }
    public SideData Side
    {
        get;
    }

    public void DealDamage(int damage);
}