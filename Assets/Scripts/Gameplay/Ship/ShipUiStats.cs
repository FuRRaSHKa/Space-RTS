using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipUiStats : MonoBehaviour
{
    [SerializeField] private List<StatBar> _statBars;
    [SerializeField] private ShipEntity _shipEntity;
   
    private void Start()
    {
      
    }

    private void StatChanged(StatData statData, int value)
    {
        
    }
}