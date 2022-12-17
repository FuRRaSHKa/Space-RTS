using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleWaepon : MonoBehaviour, IWeapon
{
    private int _localId = 0;
    private float _fireCycleTime;

    public void StopShooting()
    {
        
    }

    public void StartShooting()
    {
        
    }
}

public interface IWeapon
{
    public void StartShooting();

    public void StopShooting();
}