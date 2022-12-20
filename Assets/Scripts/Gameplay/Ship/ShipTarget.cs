using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipTarget : MonoBehaviour, ITargetable
{
    private ShipEntity _shipEntity;

    public Transform TargetTransform => transform;

    public TargetData TargetData => throw new System.NotImplementedException();

    private void Awake()
    {
        _shipEntity = GetComponent<ShipEntity>();
    }

    public void DealDamage(int damage)
    {
        _shipEntity.StatsController.DealDamage(damage);
    }
}

public struct TargetData
{
    public int CurrentVelocity;
    public int CurrentHealth;
}

public interface ITargetable
{
    public Transform TargetTransform
    {
        get;
    }
    public TargetData TargetData
    {
        get;
    }

    public void DealDamage(int damage);
}