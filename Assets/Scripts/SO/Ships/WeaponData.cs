using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    [SerializeField] private float _rotaionSpeed;
    [SerializeField] private float _shootTime;
    [SerializeField] private float _maxAngleDeviation;
    [SerializeField] private int _damage;

    [SerializeField] private PoolObject _weaponePrefab;

    public float RotationSpeed => _rotaionSpeed;
    public float ShootTime => _shootTime;
    public float MaxAngleDeviation => _maxAngleDeviation;
    public int Damage => _damage;
    public PoolObject PoolObject => _weaponePrefab;
}
