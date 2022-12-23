using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolObject : MonoBehaviour
{
    public event Action OnDisableObject;

    public void DisableObject()
    {
        OnDisableObject?.Invoke();
        gameObject.SetActive(false);
    }
}
