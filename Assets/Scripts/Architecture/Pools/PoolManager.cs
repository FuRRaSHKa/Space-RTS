using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoSingletone<PoolManager>
{
    [SerializeField] private List<PoolPair> _poolPairs;
    [SerializeField] private Transform _spawnPoint;

    private Dictionary<string, Pool> _pairs = new Dictionary<string, Pool>();

    public Pool this[string key] 
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
            _pairs.Add(pair.Name, pool);
        }
    }
}

[System.Serializable]
public struct PoolPair
{
    public PoolObject Prefab;
    public string Name;
    public int PrespawnedCount;
}
