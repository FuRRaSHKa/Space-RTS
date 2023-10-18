using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HalloGames.Utilities
{
    public class ConstantCameraAngle : MonoBehaviour
    {
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            transform.LookAt(_camera.transform, Vector3.up);
        }
    }
}