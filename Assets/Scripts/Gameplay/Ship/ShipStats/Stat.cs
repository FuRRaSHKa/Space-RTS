using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stat : MonoBehaviour
{
    private int _maxStat;
    private int _currentStat;

    public void Initilize(int maxStat)
    {
        _maxStat = maxStat;
        _currentStat = maxStat;
    }

    public void ChangeStat(int delta)
    {
        _currentStat += delta;
        _currentStat = Mathf.Clamp(_currentStat, 0, _maxStat);
    }

    public int GetValue()
    {
        return _currentStat;
    }
}
