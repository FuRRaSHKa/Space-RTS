using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ShipModelRotator : MonoBehaviour
{
    [SerializeField] private GameObject _model;
    [SerializeField] private float _speed;
    [SerializeField] private float _maxAngle;
    [SerializeField] private NavMeshAgent _navMesh;

    private Quaternion _rightRotation;
    private Quaternion _leftRotation;

    private Vector3 _prevPosition;

    private void Awake()
    {
        _leftRotation = Quaternion.Euler(0, 0, _maxAngle);
        _rightRotation = Quaternion.Inverse(_leftRotation);
    }

    private void Update()
    {
        Vector3 delta = transform.InverseTransformDirection(transform.position - _prevPosition).normalized;

        float turn = delta.x;
        if (delta.z < 0)
            turn = 1 * Mathf.Sign(turn);

        Quaternion rotation;
        if (turn < 0)
            rotation = Quaternion.Lerp(Quaternion.identity, _leftRotation, Mathf.Abs(turn));
        else
            rotation = Quaternion.Lerp(Quaternion.identity, _rightRotation, turn);

        _model.transform.localRotation = Quaternion.Lerp(_model.transform.localRotation, rotation, _speed * Time.deltaTime);
        _prevPosition = transform.position;
    }
}
