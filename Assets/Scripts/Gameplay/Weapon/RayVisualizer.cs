using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponeVisualizer
{
    public void ShowShootEffect(ITargetable targetable);
}

public class RayVisualizer : MonoBehaviour, IWeaponeVisualizer
{
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private float duration;

    private Transform _target;

    private float _curTime;
    private WeaponController _weaponController;
    private bool _visualize = false;

    private void Awake()
    {
        _weaponController= GetComponent<WeaponController>();
        _weaponController.OnShooting += ShowShootEffect;
    }

    public void ShowShootEffect(ITargetable targetable)
    {
        _visualize = true;
        _lineRenderer.enabled = true;
        _target = targetable.TargetTransform;
        _curTime = 0;
    }

    private void EndVisualize()
    {
        _lineRenderer.enabled = false;
        _visualize = false;
    }

    private void Update()
    {
        if (!_visualize)
            return;

        _curTime += Time.deltaTime;
        if (_curTime > duration)
        {
            EndVisualize();
            return;
        }

        _lineRenderer.SetPosition(0, _shootPoint.position);
        _lineRenderer.SetPosition(1, _target.position);

        LerpAlpha();
    }

    private void LerpAlpha()
    {
        Material material = _lineRenderer.material;
        Color color = material.color;
        color.a = Mathf.Lerp(1, 0, _curTime / duration);
        material.color = color;

        _lineRenderer.material = material;
    }
}
