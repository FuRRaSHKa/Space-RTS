using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponeVisualizer
{
    public void ShowShootEffect(ITargetable targetable);
}

public class RayVisualizer : MonoBehaviour, IWeaponeVisualizer
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private float duration;

    private WeaponController _weaponController;
    private bool _visualize = false;

    private void Awake()
    {
        _weaponController.OnShooting += ShowShootEffect;
    }

    public void ShowShootEffect(ITargetable targetable)
    {
        _visualize = true;
        _lineRenderer.enabled = true;
        
    }

    private void EndVisualize()
    {

    }
}
