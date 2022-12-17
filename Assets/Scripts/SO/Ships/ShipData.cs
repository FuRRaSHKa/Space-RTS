using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShipData", menuName = "Ships/ShipData")]
public class ShipData : ScriptableObject
{
    [SerializeField] private ShipHullData _shipHullData;
    [SerializeField] private List<WeaponData> _weaponDatas;

    public ShipHullData ShipHullData => _shipHullData;
    public List<WeaponData> WeaponDatas => _weaponDatas;
}