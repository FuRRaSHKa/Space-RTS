using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileVisual : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _particleSystem;
    [SerializeField] protected ParticleSystem _hitSystem;

    private WeaponController _weaponController;
    private ProjectileShooter _shooter;
    private int _curId;

    private void Awake()
    {
        _weaponController = GetComponent<WeaponController>();
        _shooter= GetComponent<ProjectileShooter>();

        _shooter.OnDealDamage += ShowHit;
        _weaponController.OnShooting += ShowShootEffect;
    }

    private void ShowHit(Vector3 point, Vector3 normal)
    {
        _hitSystem.transform.position = point;
        _hitSystem.transform.rotation = Quaternion.LookRotation(normal);
        _hitSystem.Play();
    }

    private void ShowShootEffect(ITargetable targetable)
    {
        if (_particleSystem.Length <= 0)
            return;

        _particleSystem[_curId].Play();
        _curId++;
        _curId %= _particleSystem.Length;
    }
}
