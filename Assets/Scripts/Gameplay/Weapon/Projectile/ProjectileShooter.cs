using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileShooter : MonoBehaviour, IShooter, IInitilizable<WeaponData>
{
    [SerializeField] private List<Transform> _spawnPoints;
    [SerializeField] private ProjectelData _bulletData;

    private int _damage;
    private int _spawnPointId = 0;

    private IProjecterCreator _projecterCreator;

    public event Action<Vector3, Vector3> OnDealDamage;

    public void Init(WeaponData data)
    {
        _damage = data.Damage;
    }

    public void InitProjectileCreator(IProjecterCreator creator)
    {
        _projecterCreator = creator;
    }

    public void BulletHit(Vector3 point, Vector3 normal)
    {
        OnDealDamage?.Invoke(point, normal); 
    }

    public void Shoot(ITargetable targetable)
    {
        _projecterCreator.InstantiateProjectile(_bulletData, targetable, _spawnPoints[_spawnPointId], _damage, _spawnPoints[_spawnPointId].forward).OnHit += BulletHit;
        _spawnPointId++;
        _spawnPointId %= _spawnPoints.Count;
    }
}
