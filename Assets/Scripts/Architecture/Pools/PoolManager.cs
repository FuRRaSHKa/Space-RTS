using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoSingletone<PoolManager>
{
    [SerializeField] private List<PoolPair> _poolPairs;
    [SerializeField] private Transform _spawnPoint;

    private Dictionary<PoolObject, Pool> _pairs = new Dictionary<PoolObject, Pool>();

    public Pool this[PoolObject key] 
    {
        get
        {
            return _pairs[key];
        }
    }

    protected override void Awake()
    {
        base.Awake();

        foreach (var pair in _poolPairs)
        {
            Pool pool = new Pool(pair.Prefab, pair.PrespawnedCount, _spawnPoint);
            _pairs.Add(pair.Prefab, pool);
        }
    }
}

[System.Serializable]
public struct PoolPair
{
    public PoolObject Prefab;
    public int PrespawnedCount;
}
