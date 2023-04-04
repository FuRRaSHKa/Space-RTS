using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletData", menuName = "Data/Bullet/BulletData")]
public class BulletData : ProjectelData
{

}

public abstract class ProjectelData : ScriptableObject
{
    [SerializeField] private PoolObject _projectelPrefab;
    [SerializeField] private float _speed;
    [SerializeField] private float _lifetime;

    public float Speed => _speed;
    public float Lifetime => _lifetime;
    public PoolObject ProjectelPrefab => _projectelPrefab;
} 