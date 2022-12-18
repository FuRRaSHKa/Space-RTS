using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TeamData", menuName = "Data/Team/TeamData")]
public class TeamData : ScriptableObject
{
    [SerializeField] private SideData _sideData;
    [SerializeField] private List<ShipData> _shipDatas;

    public SideData SideData => _sideData;
    public List<ShipData> ShipDatas => _shipDatas;
}
