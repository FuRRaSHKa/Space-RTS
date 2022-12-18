using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponTargeter
{
    public float AngleDelta { get; }

    public void StartFolowing(ITargetable targetable);
    public void StopFolowing();
}

public class WeaponTargeter : MonoBehaviour, IWeaponTargeter
{
    [SerializeField] private Transform _rotationPart;
    [SerializeField] private float _rotationSpeed;

    private Vector3 _defautlForward;
    private Vector3 _defaultUpward;

    private Quaternion _currentRotation;

    private ITargetable _targetable;

    public float AngleDelta => Quaternion.Angle(_rotationPart.rotation, _currentRotation);

    public void Init()
    {
        _defaultUpward = _rotationPart.up;
        _defautlForward = _rotationPart.forward;
    }

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
            return;

        Rotate();
    }

    private void Rotate()
    {
        Vector3 direction = (_targetable.TargetTransform.position - _rotationPart.transform.position);
        _currentRotation = Quaternion.LookRotation(direction, _defaultUpward);
        _rotationPart.rotation = Quaternion.RotateTowards(_rotationPart.rotation, _currentRotation, _rotationSpeed * Time.deltaTime );
    }
}
