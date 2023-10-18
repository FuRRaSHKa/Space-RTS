using HalloGames.Architecture.CoroutineManagement;
using System;
using UnityEngine;

namespace HalloGames.Architecture.PoolSystem
{

    public class PoolObject : MonoBehaviour
    {
        private bool _enableToSpawn;
        private Routine _disableObjectRoutine;

        public event Action<PoolObject> OnPoolReturn;

        public bool EnableToSpawn => _enableToSpawn;

        public void DisableObject()
        {
            if (gameObject.activeSelf && _disableObjectRoutine == null)
            {
                _disableObjectRoutine = RoutineManager.CreateRoutine().NextFrame(() =>
                {
                    gameObject.SetActive(false);
                    _enableToSpawn = true;

                    _disableObjectRoutine = null;
                });

                _disableObjectRoutine.Start();
            }
            else
                _enableToSpawn = true;

            OnPoolReturn?.Invoke(this);
        }

        public void ForceDisable()
        {
            _disableObjectRoutine?.Stop();
            _disableObjectRoutine = null;

            gameObject.SetActive(false);
            _enableToSpawn = true;
            OnPoolReturn?.Invoke(this);
        }

        public void Spawn()
        {
            _disableObjectRoutine?.Stop();
            _disableObjectRoutine = null;

            _enableToSpawn = false;
            gameObject.SetActive(true);
        }
    }
}