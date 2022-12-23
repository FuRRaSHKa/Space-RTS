using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponeType
{
    Ray,
    Projectile
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    [SerializeField] private float _rotaionSpeed;
    [SerializeField] private float _shootTime;
    [SerializeField] private float _maxAngleDeviation;
    [SerializeField] private int _damage;
    [SerializeField] private float _distance;
    [SerializeField] private WeaponeType _weaponeType;
    
    [SerializeField] private GameObject _weaponPrefab;

    public WeaponeType WeaponeType => _weaponeType;
    public float Distance => _distance;
    public float RotationSpeed => _rotaionSpeed;
    public float ShootTime => _shootTime;
    public float MaxAngleDeviation => _maxAngleDeviation;
    public int Damage => _damage;
    public GameObject Prefab => _weaponPrefab;
}