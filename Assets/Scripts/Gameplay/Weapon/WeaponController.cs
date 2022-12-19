using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour, IWeapon
{
    [SerializeField] private float _shootTime;
    [SerializeField] private float _maxAngleDeviation;

    private ITargetable _target;
    private IShooter _shooter;
    private IWeaponTargeter _weaponTargeter;

    private float _currentTime;

    public event Action<ITargetable> OnShooting;

    private void Awake()
    {
        _shooter = GetComponent<IShooter>();
        _weaponTargeter = GetComponent<IWeaponTargeter>();
    }

    public void StartShooting(ITargetable targetable)
    {
        _target = targetable;
        _weaponTargeter.StartFolowing(targetable);
    }

    public void StopShooting()
    {
        _weaponTargeter.StopFolowing();
        _target = null;
    }

    private void Update()
    {
        if (_target == null)
            return;

        if (_currentTime > _shootTime)
        {
            if (_weaponTargeter.AngleDelta <= _maxAngleDeviation)
                Shoot();

            return;
        }

        _currentTime += Time.deltaTime;
    }

    private void Shoot()
    {
        _shooter.Shoot(_target);
        OnShooting(_target);
        _currentTime = 0;
    }
}
