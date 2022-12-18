using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShipHullData", menuName = "Data/Ships/ShipHullData")]
public class ShipHullData : ScriptableObject
{
    [SerializeField] private ShipSize _shipSize;
    [SerializeField] private PoolObject _hullPrefab;
    [SerializeField] private float _defaultShipSpeed;
    [SerializeField] private float _defaultShipAcceleration;
    [SerializeField] private float _defaultShipRotationSpeed;

    public PoolObject HullPrefab => _hullPrefab;
    public ShipSize ShipSize => _shipSize;
    public float DefaultShipSpeed => _defaultShipSpeed;
    public float DefaultShipAcceleration => _defaultShipAcceleration;
    public float DefaultShipRotationSpeed => _defaultShipRotationSpeed;
}
