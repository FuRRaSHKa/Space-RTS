using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.Architecture.Initializer
{
    public abstract class AbstractInitilizer<TData> : MonoBehaviour
    {
        protected IInitializable<TData>[] _initializables;
        protected IDeInitializable[] _deInitializables;

        protected virtual void Awake()
        {
            _deInitializables = GetComponentsInChildren<IDeInitializable>();
            _initializables = GetComponentsInChildren<IInitializable<TData>>();
        }

        public virtual void Initialize(TData data)
        {
            foreach (var item in _initializables)
            {
                item.Init(data);
            }
        }

        public virtual void DeInitialize()
        {
            foreach (var item in _deInitializables)
            {
                item.DeInit();
            }
        }
    }

    public interface IInitializable<TData>
    {
        public void Init(TData data);
    }

    public interface IDeInitializable
    {
        public void DeInit();
    }
}