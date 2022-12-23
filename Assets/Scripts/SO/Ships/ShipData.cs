using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShipData", menuName = "Data/Ships/ShipData")]
public class ShipData : ScriptableObject
{
    [SerializeField] private ShipHullData _shipHullData;
    [SerializeField] private WeaponData _weaponData;
    [SerializeField] private List<StatStruct> _statsDatas;

    public ShipHullData ShipHullData => _shipHullData;
    public WeaponData WeaponData => _weaponData;
    public List<StatStruct> StatDatas => _statsDatas;
}

[System.Serializable]
public struct StatStruct
{
    public StatData StatData;
    public int StartValue;
    public int DamageOrder;
}