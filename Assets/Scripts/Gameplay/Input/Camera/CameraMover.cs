using HalloGames.SpaceRTS.Management.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HalloGames.SpaceRTS.Management.CameraManagement
{
    public class CameraMover : MonoBehaviour
    {
        [SerializeField] private Transform _center;

        [SerializeField] private Vector3 _cameraBorders;
        [SerializeField] private float _mouseMoveTriggerBorders;
        [SerializeField] private float _cameraMoveSpeed;

        [Header("Rotation Settings")]
        [SerializeField] private float _xRotationMaxBorder;
        [SerializeField] private float _xRotationMinBorder;
        [SerializeField] private float _xRotationSensivity;
        [SerializeField] private float _yRotationSensivity;

        [Header("Zoom Settings")]
        [SerializeField] private float _zoomSpeed;
        [SerializeField] private float _zoomSensivity;
        [SerializeField] private float _minZoomDistance;
        [SerializeField] private float _maxZoomDistance;

        private Transform _camera;
        private Vector3 _targetLocalPos;
        private float _currentZoom;

        private bool _isCameraRotation = false;

        private IInput _input;

        private void Awake()
        {
            _camera = UnityEngine.Camera.main.transform;
            _targetLocalPos = _camera.localPosition;
            _currentZoom = _camera.localPosition.magnitude;
        }

        public void Initilize(IInput input)
        {
            _input = input;

            _input.OnScrollPressed += RotateChange;
            _input.OnScrollChange += ZoomCamera;
        }

        private void Update()
        {
            if (!_isCameraRotation)
                MoveCenter();
            else
                RotateCamera();

            MoveCamera();
            LookAtCenter();
        }

        private void MoveCamera()
        {
            _camera.localPosition = Vector3.Lerp(_camera.localPosition, _targetLocalPos, _zoomSpeed * Time.deltaTime);
        }

        private void RotateCamera()
        {
            Vector2 camDelta = _input.MouseDelta;
            Vector3 rotation = _center.transform.eulerAngles;

            rotation.x = Mathf.Clamp(rotation.x + -camDelta.y * _xRotationSensivity * Time.deltaTime, _xRotationMinBorder, _xRotationMaxBorder);
            rotation.y += camDelta.x * _yRotationSensivity * Time.deltaTime;

            _center.rotation = Quaternion.Euler(rotation);
        }

        private void RotateChange(bool value)
        {
            _isCameraRotation = value;
        }

        private void ZoomCamera(float zoomDelta)
        {
            zoomDelta = -Mathf.Clamp(zoomDelta, -1, 1);
            Vector3 currentZoom = _camera.localPosition;
            Vector3 zoomVectorDelta = currentZoom.normalized * zoomDelta * _zoomSensivity;

            Vector3 result = currentZoom + zoomVectorDelta;
            if (result.magnitude < _minZoomDistance)
            {
                result = currentZoom.normalized * _minZoomDistance;
            }
            else if (result.magnitude > _maxZoomDistance)
            {
                result = currentZoom.normalized * _maxZoomDistance;
            }

            _currentZoom = result.magnitude;
            _targetLocalPos = result;
        }

        private void LookAtCenter()
        {
            _camera.LookAt(_center);
        }

        private void MoveCenter()
        {
            Vector2 mousePOS = Mouse.current.position.ReadValue();
            Vector2 res = new Vector2(Screen.width, Screen.height);

            Vector3 direction = Vector3.zero;
            Quaternion rotation = Quaternion.Euler(new Vector3(0, _camera.eulerAngles.y, 0));

            if (mousePOS.x < _mouseMoveTriggerBorders)
                direction += Vector3.left * _cameraMoveSpeed * Time.deltaTime;
            else if (mousePOS.x > res.x - _mouseMoveTriggerBorders)
                direction += Vector3.right * _cameraMoveSpeed * Time.deltaTime;

            if (mousePOS.y < _mouseMoveTriggerBorders)
                direction += Vector3.back * _cameraMoveSpeed * Time.deltaTime;
            else if (mousePOS.y > res.y - _mouseMoveTriggerBorders)
                direction += Vector3.forward * _cameraMoveSpeed * Time.deltaTime;


            _center.position += rotation * direction * (Mathf.Clamp01(_currentZoom / (_maxZoomDistance - _minZoomDistance)));
            Vector3 pos = _center.position;
            pos.x = Mathf.Clamp(pos.x, -_cameraBorders.x, _cameraBorders.x);
            pos.z = Mathf.Clamp(pos.z, -_cameraBorders.z, _cameraBorders.z);
            _center.position = pos;
        }
    }
}

