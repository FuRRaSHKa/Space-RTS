using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayWeapon : MonoBehaviour, IWeapon
{
    private int _localId = 0;
    private float _fireCycleTime;

    public void StopShooting()
    {
        
    }

    public void StartShooting(ITargetable targetable)
    {
        
    }
}

public interface IWeapon
{
    public void StartShooting(ITargetable targetable);

    public void StopShooting();
}