using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RocketData", menuName = "Data/Rocket/RocketData")]
public class RocketData : ProjectelData
{
    [SerializeField] private float _inertia;

    public float Inertia => _inertia;
}
