using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Data/Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    [SerializeField] private PoolObject _weaponePrefab;

    public PoolObject PoolObject => _weaponePrefab;
}
