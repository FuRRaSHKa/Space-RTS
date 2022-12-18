using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IShooter
{
    public void Shoot(ITargetable targetable);
}

public class RayShooter : MonoBehaviour, IShooter
{
    public void Shoot(ITargetable targetable)
    {
        Debug.Log("Pew");
    }
}
