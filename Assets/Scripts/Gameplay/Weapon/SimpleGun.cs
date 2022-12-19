using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayWeapon : MonoBehaviour, IWeapon
{
    public event Action<ITargetable> OnShooting;

    public void StopShooting()
    {
        
    }

    public void StartShooting(ITargetable targetable)
    {
        
    }
}

public interface IWeapon
{
    public event Action<ITargetable> OnShooting;

    public void StartShooting(ITargetable targetable);

    public void StopShooting();
}