using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipInitilizer : AbstractInitilizer<ShipInitilizationData>
{
    private ShipEntity _entity;
}

public readonly struct ShipInitilizationData
{
    public readonly SideData SideData;
    public readonly ShipData ShipData;

    public ShipInitilizationData(ShipData shipData, SideData sideData)
    {
        SideData = sideData;
        ShipData = shipData;
    }
}