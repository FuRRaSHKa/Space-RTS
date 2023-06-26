using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RocketData", menuName = "Data/Rocket/RocketData")]
public class RocketData : ProjectelData
{
    [SerializeField] private float _rotationSpeed;
    [SerializeField] private float _rotationAcceleration;

    [SerializeField] private float _startSpeed;
    [SerializeField] private float _acceleration;

    public float RotationSpeed => _rotationSpeed;
    public float Acceleration => _acceleration;
    public float StartSpeed => _startSpeed;
    public float RotationAcceleration => _rotationAcceleration;
}
