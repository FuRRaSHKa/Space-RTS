using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileVisual : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] _particleSystem;

    private WeaponController _weaponController;
    private int _curId;

    private void Awake()
    {
        _weaponController = GetComponent<WeaponController>();
        _weaponController.OnShooting += ShowShootEffect;
    }

    private void ShowShootEffect(ITargetable targetable)
    {
        _particleSystem[_curId].Play();
        _curId++;
        _curId %= _particleSystem.Length;
    }
}
