using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour, IWeapon, IInitilizable<WeaponData>
{
    private float _shootTime;
    private float _maxAngleDeviation;
    private float _distance;

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

    public void Init(WeaponData data)
    {
        _shootTime = data.ShootTime;
        _maxAngleDeviation = data.MaxAngleDeviation;
        _distance = data.Distance;
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
        if (_currentTime > _shootTime)
        {
            if (_target == null)
                return;

            if (_weaponTargeter.AngleDelta <= _maxAngleDeviation && _distance > (_target.TargetTransform.position - transform.position).magnitude)
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
