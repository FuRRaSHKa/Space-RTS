using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSingletone<TMono> : MonoBehaviour where TMono : MonoSingletone<TMono>
{
    private static TMono _instance; 

    public static TMono Instance 
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError($"Singletone {typeof(TMono)} not initilized");
                return null;
            }

            return _instance;
        }

        private set
        {
            _instance = value;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null)
        {
            Destroy(this); 
        }

        _instance = (TMono)this;
    }

}
