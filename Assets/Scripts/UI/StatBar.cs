using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatBar : MonoBehaviour, IInitilizable<ShipInitilizationData>
{
    [SerializeField] private ShipEntity _shipEntity;
    [SerializeField] private Image _bar;
    [SerializeField] private StatData _statData;

    private int _maxValue;

    private void Start()
    {
        _shipEntity.StatsController.OnStatChange += (data, value) =>
        {
            if (data == _statData)
                SetValue(value);
        };
    }

    private void Init(StatStruct statData)
    {
        _maxValue = statData.StartValue;
        _bar.color = statData.StatData.Color;
        SetValue(_maxValue);
    }

    public void Init(ShipInitilizationData data)
    {
        StatStruct statData = data.ShipData.StatDatas.Find(f => f.StatData == _statData);
        Init(statData);
    }

    public void SetValue(int curValue)
    {
        _bar.fillAmount = Mathf.Clamp01((float)curValue / _maxValue);
    }
}
