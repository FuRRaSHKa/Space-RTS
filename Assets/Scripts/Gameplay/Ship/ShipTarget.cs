using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipTarget : MonoBehaviour, ITargetable
{
    public Transform TargetTransform => transform;

    public TargetData TargetData => throw new System.NotImplementedException();
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