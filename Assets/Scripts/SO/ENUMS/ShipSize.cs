using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipSize : ScriptableObject
{
    [SerializeField] private string _sizeName; 
    public string SizeName => _sizeName;
}
