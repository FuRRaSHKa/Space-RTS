using HalloGames.Architecture.Frames;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HalloGames.Utilities
{
    public class ConstantCameraAngle : MonoBehaviour, IUpdatable
    {
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
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
            transform.LookAt(_camera.transform, Vector3.up);
        }
    }
}