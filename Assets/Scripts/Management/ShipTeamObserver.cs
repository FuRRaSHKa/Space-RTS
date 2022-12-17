using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipTeamObserver
{
    private SideData _sideData;
    private List<ShipEntity> _ships;

    public event Action OnAllDies;

    public ShipTeamObserver(SideData sideData, List<ShipEntity> ships)
    {
        _sideData = sideData;
        _ships = ships;
    }

    public void StartObserving()
    {
        foreach (var ship in _ships)
        {
            
        }
    }

    private void ShipDeath()
    {

    }
}
