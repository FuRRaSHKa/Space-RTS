using System;
using System.Collections;
using UnityEngine;

namespace HalloGames.Architecture.CoroutineManagement
{
    public class Routine : IStopable
    {
        private readonly MonoBehaviour _coroutineHost;

        private IEnumerator _enumerator;
        private Action _onEndAction;
        private Coroutine _coroutine;

        public Routine(MonoBehaviour coroutineHost, IEnumerator enumerator) : this(coroutineHost)
        {
            _coroutineHost = coroutineHost;
            _enumerator = enumerator;
        }

        public Routine(MonoBehaviour coroutineHost)
        {
            _coroutineHost = coroutineHost;
        }

        public IStopable Start()
        {
            if (_enumerator != null && _coroutine == null && _coroutineHost != null)
            {
                _coroutine = _coroutineHost.StartCoroutine(RunRoutine());
            }

            return this;
        }

        public void Stop()
        {
            if (_coroutine != null && _coroutineHost != null)
            {
                _coroutineHost.StopCoroutine(_coroutine);
                _coroutine = null;
            }

            _onEndAction = null;
            _coroutine = null;
        }

        private IEnumerator RunRoutine()
        {
            yield return _enumerator;

            EndRoutine();
        }

        public Routine Finally(Action completeCallback)
        {
            _onEndAction = completeCallback;
            return this;
        }

        private void EndRoutine()
        {
            _onEndAction?.Invoke();
            _onEndAction = null;
            _coroutine = null;
        }

        public Routine SetEnumerator(IEnumerator enumerator)
        {
            _enumerator = enumerator;
            return this;
        }
    }

}

