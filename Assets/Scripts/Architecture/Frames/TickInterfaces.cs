using HalloGames.Architecture.Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.Architecture.Frames
{
    public interface ITickable
    {
    }

    public interface ILogicTickable : ITickable
    {
        public void Tick(float deltaTime);

    }

    public interface IUpdatable : ITickable
    {
        public void UpdateTick(float deltaTime);

    }

    public interface IFixedUpdatable : ITickable
    {
        public void FixedUpdateTick(float deltaTime);

    }

    public interface ILogicTicksDispatcher : IService
    {
        public void Register(ILogicTickable logicTickable);
        public void Unregister(ILogicTickable logicTickable);
    }

    public interface IUpdateTicksDispatcher : IService
    {
        public void Register(IUpdatable logicTickable);
        public void Unregister(IUpdatable logicTickable);
    }

    public interface IFixedUpdateTicksDispatcher : IService
    {
        public void Register(IFixedUpdatable logicTickable);
        public void Unregister(IFixedUpdatable logicTickable);
    }
}