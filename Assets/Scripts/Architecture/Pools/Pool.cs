using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Pool
{
    private PoolObject _prefab;
    private Transform _spawnPoint;

    private List<PoolObject> _objects = new List<PoolObject>();

    public Pool(PoolObject prefab, int prebakedCount ,Transform spawnPoint)
    {
        _prefab = prefab;
        _spawnPoint = spawnPoint;

        for (int i = 0; i < prebakedCount; i++)
        {
            PoolObject output = Object.Instantiate(_prefab, _spawnPoint);
            output.DisableObject();

            _objects.Add(output);
        }
    }

    public PoolObject GetObject()
    {
        PoolObject output = _objects
            .DefaultIfEmpty(null)
            .FirstOrDefault(f => !f.gameObject.activeSelf);

        if (output == null)
            output = SpawnObject();

        output.gameObject.SetActive(true);
        return output;
    }

    private PoolObject SpawnObject()
    {
        PoolObject output = Object.Instantiate(_prefab, _spawnPoint);
        _objects.Add(output);

        return output; 
    }

    private void ReturnObject(PoolObject poolObject)
    {
        poolObject.transform.SetParent(_spawnPoint, false);
    }

    public void ReturnAllObjects()
    {
        foreach (var obj in _objects)
        {
            obj.DisableObject();
        }
    }
}
