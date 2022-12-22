using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PojectileShooter : MonoBehaviour, IShooter, IInitilizable<WeaponData>
{
    [SerializeField] private List<Transform> _spawnPoints;
    [SerializeField] private BulletData _bulletData;

    private int _damage;
    private int _spawnPointId = 0;

    private IProjecterCreator _projecterCreator;

    public void Init(WeaponData data)
    {
        _damage = data.Damage;
    }

    public void InitProjectileCreator(IProjecterCreator creator)
    {
        _projecterCreator = creator;
    }

    public void Shoot(ITargetable targetable)
    {
        _projecterCreator.InstantiateBullet(_bulletData, targetable, _spawnPoints[_spawnPointId], _damage, _spawnPoints[_spawnPointId].forward);
        _spawnPointId++;
        _spawnPointId %= _spawnPoints.Count;
    }
}
