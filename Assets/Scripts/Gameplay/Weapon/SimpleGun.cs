using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RayWeapon : MonoBehaviour, IWeapon
{
    public event Action<ITargetable> OnShooting;
    public event Action<ITargetable> OnTargetDeath;

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