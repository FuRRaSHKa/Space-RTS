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
        StartCoroutine(DisableRoutine());
    }

    private IEnumerator DisableRoutine()
    {
        yield return null;
        gameObject.SetActive(false);
    }
}
