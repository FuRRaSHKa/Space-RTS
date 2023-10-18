using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.Architecture.Initilizer
{
    public abstract class AbstractInitilizer<TData> : MonoBehaviour
    {
        protected IInitilizable<TData>[] _initilizables;
        protected IDeInitilizable[] _deInitilizables;

        protected virtual void Awake()
        {
            _deInitilizables = GetComponentsInChildren<IDeInitilizable>();
            _initilizables = GetComponentsInChildren<IInitilizable<TData>>();
        }

        public virtual void Initilize(TData data)
        {
            foreach (var item in _initilizables)
            {
                item.Init(data);
            }
        }

        public virtual void DeInitilize()
        {
            foreach (var item in _deInitilizables)
            {
                item.DeInit();
            }
        }
    }

    public interface IInitilizable<TData>
    {
        public void Init(TData data);
    }

    public interface IDeInitilizable
    {
        public void DeInit();
    }
}