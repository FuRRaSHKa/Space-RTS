using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITargetController
{
    public void LockTarget(ITargetable target);
}

public class ShipTargetController : MonoBehaviour, ITargetController
{
    private List<ITargetable> _targets = new List<ITargetable>();

    public void LockTarget(ITargetable target)
    {
        _targets.Add(target);
    }
}

public struct TargetData
{
    public int CurrentVelocity;
    public int CurrentHealth;
}

public interface ITargetable
{
    public Transform TargetTransform { get; }
    public TargetData TargetData { get; }
}