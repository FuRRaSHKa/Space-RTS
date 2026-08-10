using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace HalloGames.Architecture.Frames
{
    public class TickDispatcher<TTickable> where TTickable : class, ITickable
    {
        private readonly Action<TTickable, float> _tickDelegate;

        private List<TTickable> _tickers = new List<TTickable>(64);
        public TickDispatcher(Action<TTickable, float> tickDelegate)
        {
            _tickDelegate = tickDelegate;

            Assert.IsNotNull(_tickDelegate);
        }

        public void Register(TTickable tickable)
        {
            _tickers.Add(tickable);
        }

        public void Unregister(TTickable tickable)
        {
            var id = _tickers.IndexOf(tickable);
            if (id < 0)
                return;

            _tickers[id] = null;
        }

        public void Run(float deltaTime)
        {
            var markToRemove = false;
            var count = _tickers.Count;
            for (int i = 0; i < count; i++)
            {
                var tickable = _tickers[i];
                if (tickable == null)
                {
                    markToRemove = true;
                    continue;
                }

                _tickDelegate(tickable, deltaTime);
            }

            if (markToRemove)
                Compact();
        }

        private void Compact()
        {
            int write = 0;
            int count = _tickers.Count;

            for (int read = 0; read < count; read++)
            {
                var item = _tickers[read];
                if (item == null)
                    continue;

                if (write != read)
                    _tickers[write] = item;
                
                write++;
            }

            _tickers.RemoveRange(write, count - write);
        }
    }
}