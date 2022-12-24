using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvanceWeaponTargeter : MonoBehaviour, IWeaponTargeter, IInitilizable<WeaponData>
{
    [SerializeField] private BulletData _bulletData;
    [SerializeField] private Transform _rotationPart;
    [SerializeField] private Transform _basement;

    private float _rotationSpeed;

    private Quaternion _currentRotation;
    private Quaternion _targetRotation;

    private ITargetable _targetable;

    public float AngleDelta => Quaternion.Angle(_rotationPart.rotation, _targetRotation);

    public void StartFolowing(ITargetable targetable)
    {
        _targetable = targetable;
    }

    public void StopFolowing()
    {
        _targetable = null;
    }

    private void Update()
    {
        if (_targetable == null)
            RotateToDefault();
        else
            Rotate();
    }

    private void RotateToDefault()
    {
        _currentRotation = Quaternion.LookRotation(_basement.forward, _basement.up);
        _rotationPart.rotation = Quaternion.RotateTowards(_rotationPart.rotation, _currentRotation, _rotationSpeed * Time.deltaTime);
    }

    private void Rotate()
    {

        Vector3 direction = (CalculatePos() - _rotationPart.transform.position).normalized;
        _targetRotation = Quaternion.LookRotation(direction, _basement.up);

        //Clamp turret rotation
        Vector3 localDirection = _basement.InverseTransformDirection(direction).normalized;
        localDirection.y = Mathf.Clamp(localDirection.y, -.1f, .7f);

        direction = _basement.TransformDirection(localDirection).normalized;

        _currentRotation = Quaternion.LookRotation(direction, _basement.up);
        _rotationPart.rotation = Quaternion.RotateTowards(_rotationPart.rotation, _currentRotation, _rotationSpeed * Time.deltaTime);
    }

    private Vector3 CalculatePos()
    {
        float distance = (_targetable.TargetTransform.position - _rotationPart.transform.position).magnitude;

        Vector3 targetPosition = _targetable.TargetTransform.position + _targetable.TargetDataObservable.CurrentVelocity * distance / _bulletData.Speed;
        return targetPosition;
    }

    public void Init(WeaponData data)
    {
        _rotationSpeed = data.RotationSpeed;
    }
}
