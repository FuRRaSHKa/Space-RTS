using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour, IWeapon
{
    private ITargetable _target;
    private IShooter _shooter;

    public Action<ITargetable> OnShooting;

    private void Awake()
    {
        
    }

    public void StartShooting(ITargetable targetable)
    {
        
    }

    public void StopShooting()
    {
        
    }
}
