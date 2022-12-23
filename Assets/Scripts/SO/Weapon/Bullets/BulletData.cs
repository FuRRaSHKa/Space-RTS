using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletData", menuName = "Data/Bullet/BulletData")]
public class BulletData : ScriptableObject
{
    [SerializeField] private PoolObject _bulletPrefab;
    [SerializeField] private float _speed;
    [SerializeField] private float _lifetime;

    public float Speed => _speed;
    public float Lifetime => _lifetime;
    public PoolObject BulletPrefab => _bulletPrefab;
}
