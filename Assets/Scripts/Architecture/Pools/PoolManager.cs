using HalloGames.Architecture.Singletones;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.Architecture.PoolSystem
{
    public class PoolManager : MonoSingleton<PoolManager>
    {
        [SerializeField] private Transform _defaultSpawnPoint;
        [SerializeField] private PoolPair[] _poolPairs;

        private Dictionary<PoolObject, ObjectPool> _pools;

        protected override void OverridedAwake()
        {
            _pools = new Dictionary<PoolObject, ObjectPool>(_poolPairs.Length);
            foreach (var pair in _poolPairs)
            {
                ObjectPool pool = new ObjectPool(pair.prefab, _defaultSpawnPoint, pair.PrebakedCount, pair.KeepWhenSceneChanged);
                _pools.Add(pair.prefab, pool);
            }
        }

        public ObjectPool this[PoolObject key]
        {
            get
            {
                if (!_pools.ContainsKey(key))
                    throw new NullReferenceException("There is no pool with that name");

                return _pools[key];
            }
        }

        public void ReturnPools()
        {
            foreach (var pool in _pools.Values)
            {
                if (!pool.KeepWhenSceneChanged)
                    pool.ReturnAllToPools();
            }
        }
    }

    [Serializable]
    public struct PoolPair
    {
        public string Name;
        public int PrebakedCount;
        public bool KeepWhenSceneChanged;
        public PoolObject prefab;
    }
}