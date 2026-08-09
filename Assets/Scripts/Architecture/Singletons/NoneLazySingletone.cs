using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace HalloGames.Architecture.Singletones
{
    public class NoneLazySingletone<TInstance> where TInstance : class
    {
        private static TInstance _instance;

        public static TInstance Instance => _instance;

        public NoneLazySingletone()
        {
            if (_instance == null)
                _instance = this as TInstance;
        }
    }
}