using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HalloGames.Architecture.PoolSystem
{
    public class ObjectPool
    {
        private PoolObject _prefab;
        private Transform _spawnPoint;
        private List<PoolObject> _pool = new List<PoolObject>();

        public bool KeepWhenSceneChanged
        {
            get; private set;
        }

        public ObjectPool(PoolObject prefab, Transform spawnPoint, int prebakedCount, bool keepWhenSceneChanged = false)
        {
            _prefab = prefab;
            _spawnPoint = spawnPoint;
            KeepWhenSceneChanged = keepWhenSceneChanged;

            _pool = new List<PoolObject>();
            for (int i = 0; i < prebakedCount; i++)
            {
                PoolObject obj = SpawnNewObject();
                obj.ForceDisable();
            }
        }

        public PoolObject SpawnObject()
        {
            PoolObject output = _pool
                 .DefaultIfEmpty(null)
                 .FirstOrDefault(f => f.EnableToSpawn);

            if (output == null)
                output = SpawnNewObject();

            output.Spawn();
            return output;
        }

        private PoolObject SpawnNewObject()
        {
            PoolObject output = Object.Instantiate(_prefab, _spawnPoint);
            output.OnPoolReturn += ReturnObjectToPool;
            _pool.Add(output);
            output.name += _pool.Count.ToString();
            return output;
        }

        public void ReturnAllToPools()
        {
            foreach (var obj in _pool)
            {
                obj.DisableObject();
            }
        }

        private void ReturnObjectToPool(PoolObject poolObject)
        {
            poolObject.transform.SetParent(_spawnPoint, false);
        }
    }
}