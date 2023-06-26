using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketTargeter : MonoBehaviour, IWeaponTargeter, IInitilizable<WeaponData>
{
    private ITargetable _targetable;

    public float AngleDelta => 0;

    public void Init(WeaponData data)
    {
       
    }

    public void StartFolowing(ITargetable targetable)
    {
        _targetable = targetable;
    }

    public void StopFolowing()
    {
        _targetable = null;
    }
}
