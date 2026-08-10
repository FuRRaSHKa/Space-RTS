using HalloGames.Architecture.CoroutineManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.Architecture.Frames
{
    public class CentralTicker : MonoBehaviour, ILogicTicksDispatcher, IUpdateTicksDispatcher, IFixedUpdateTicksDispatcher
    {
        [SerializeField] private int _ticksPerSecond = 30;
        [SerializeField] private int _maxTicksPerFrame = 2;

        private float _logicDeltaTime = 0;
        private float _accumulator = 0;

        private TickDispatcher<ILogicTickable> _logicTicker;
        private TickDispatcher<IUpdatable> _updateTicker;
        private TickDispatcher<IFixedUpdatable> _fixedUpdateTicker;

        private void Awake()
        {
            _logicDeltaTime = 1f / _ticksPerSecond;

            _logicTicker = new TickDispatcher<ILogicTickable>(static (t, dt) => t.Tick(dt));
            _updateTicker = new TickDispatcher<IUpdatable>(static (t, dt) => t.Tick(dt));
            _fixedUpdateTicker = new TickDispatcher<IFixedUpdatable>(static (t, dt) => t.Tick(dt));
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            _updateTicker.Run(deltaTime);

            AccumulateLogic();
        }

        private void AccumulateLogic()
        {
            _accumulator += Time.deltaTime;

            int steps = 0;
            while (_accumulator >= _logicDeltaTime)
            {
                if (steps >= _maxTicksPerFrame)
                {
                    _accumulator = 0f;
                    break;
                }

                _logicTicker.Run(_logicDeltaTime);
                _accumulator -= _logicDeltaTime;
                steps++;
            }
        }

        private void FixedUpdate()
        {
            var fixedUpdate = Time.fixedDeltaTime;
            _fixedUpdateTicker.Run(fixedUpdate);
        }

        public void Register(ILogicTickable logicTickable)
        {
            _logicTicker.Register(logicTickable);
        }

        public void Unregister(ILogicTickable logicTickable)
        {
            _logicTicker.Unregister(logicTickable);
        }

        public void Register(IFixedUpdatable logicTickable)
        {
            _fixedUpdateTicker.Register(logicTickable);
        }

        public void Unregister(IFixedUpdatable logicTickable)
        {
            _fixedUpdateTicker.Unregister(logicTickable);
        }

        public void Register(IUpdatable logicTickable)
        {
            _updateTicker.Register(logicTickable);
        }

        public void Unregister(IUpdatable logicTickable)
        {
            _updateTicker.Unregister(logicTickable);
        }
    }
}


