using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace HalloGames.Architecture.Singletones
{
    public class NoneLazySingletone<TIntance> where TIntance : class
    {
        private static TIntance _instance;

        public static TIntance Instance => _instance;

        public NoneLazySingletone()
        {
            if (_instance == null)
                _instance = this as TIntance;
        }
    }
}