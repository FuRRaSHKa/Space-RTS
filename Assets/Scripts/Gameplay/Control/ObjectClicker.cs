using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectClicker : MonoSingletone<ObjectClicker>
{
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _backgroundLayer;

    private Camera _camera;
    private GameObject _currentObject;
    private Vector3 _pos;

    protected override void Awake()
    {
        _camera = Camera.main;
        base.Awake();
    }

    private void Update()
    {
        Vector3 rawPos = Mouse.current.position.ReadValue();
        rawPos.z = 5;
        Ray ray = _camera.ScreenPointToRay(rawPos);
        
        RaycastHit raycastHit;
        if (Physics.Raycast(ray, out raycastHit, 30, _targetLayer.value))
            _currentObject = raycastHit.collider.gameObject;
        else
            _currentObject = null;

        if (Physics.Raycast(ray, out raycastHit, 30, _backgroundLayer))
            _pos = raycastHit.point;
    }

    public GameObject GetCurrentObject()
    {
        return _currentObject;
    }

    public Vector3 GetWorldMousePos()
    {
        return _pos;
    }
}
