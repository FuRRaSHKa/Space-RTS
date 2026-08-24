using UnityEngine;
using HalloGames.Architecture.Frames;
using HalloGames.Architecture.Services;

namespace HalloGames.SpaceRTS.Management.Input
{
    public class ObjectClicker : MonoBehaviour, IUpdatable, IService
    {
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private LayerMask _backgroundLayer;

        private Camera _camera;
        private GameObject _currentObject;
        private Vector3 _pos;
        private IInput _input;

        private void Awake()
        {
            _camera = Camera.main;
        }

        public void Initialize(IInput input)
        {
            _input = input;
        }

        private void OnEnable()
        {
            TickManager.RegisterUpdate(this);
        }

        private void OnDisable()
        {
            TickManager.UnregisterUpdate(this);
        }

        public void UpdateTick(float deltaTime)
        {
            if (_input == null || !_input.IsMouseOnScreen)
            {
                _currentObject = null;
                return;
            }

            Vector3 rawPos = _input.MouseScreenPosition;
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
}