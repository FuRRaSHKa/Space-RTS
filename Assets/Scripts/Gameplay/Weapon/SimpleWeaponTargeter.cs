using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponTargeter
{
    public float AngleDelta
    {
        get;
    }

    public void StartFolowing(ITargetable targetable);
    public void StopFolowing();
}

public class SimpleWeaponTargeter : MonoBehaviour, IWeaponTargeter, IInitilizable<WeaponData>
{
    [SerializeField] private Transform _rotationPart;
    [SerializeField] private float _rotationSpeed;
    [SerializeField] private Transform _basement; 

    private Vector3 _defautlForward;
    private Vector3 _defaultUpward;
    private Vector3 _defoultRight;

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
        _currentRotation = Quaternion.LookRotation(_basement.forward, _defaultUpward);
        _rotationPart.rotation = Quaternion.RotateTowards(_rotationPart.rotation, _currentRotation, _rotationSpeed * Time.deltaTime);
    }

    private void Rotate()
    {
        Vector3 direction = (_targetable.TargetTransform.position - _rotationPart.transform.position).normalized;
        _targetRotation = Quaternion.LookRotation(direction, _defaultUpward);     

        //Clamp turret rotation
        Vector3 localDirection = _basement.InverseTransformDirection(direction).normalized;
        localDirection.y = Mathf.Clamp(localDirection.y, -.3f, .7f);

        direction = _basement.TransformDirection(localDirection).normalized;

        _currentRotation = Quaternion.LookRotation(direction, _defaultUpward);
        _rotationPart.rotation = Quaternion.RotateTowards(_rotationPart.rotation, _currentRotation, _rotationSpeed * Time.deltaTime);
    }

    public void Init(WeaponData data)
    {
        _defaultUpward =  _basement.up;
        _defautlForward = _basement.forward;
    }
}
