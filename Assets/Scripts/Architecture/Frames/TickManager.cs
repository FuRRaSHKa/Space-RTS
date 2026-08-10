using UnityEngine.Assertions;

namespace HalloGames.Architecture.Frames
{
    public static class TickManager
    {
        public static void RegisterUpdate(IUpdatable tickable)
        {
            Assert.IsNotNull(CentralTicker.Instance, $"{nameof(TickManager)}: no {nameof(CentralTicker)} in the scene");

            CentralTicker.Instance.Register(tickable);
        }

        public static void UnregisterUpdate(IUpdatable tickable)
        {
            if (CentralTicker.Instance == null)
                return;

            CentralTicker.Instance.Unregister(tickable);
        }

        public static void RegisterLogic(ILogicTickable tickable)
        {
            Assert.IsNotNull(CentralTicker.Instance, $"{nameof(TickManager)}: no {nameof(CentralTicker)} in the scene");

            CentralTicker.Instance.Register(tickable);
        }

        public static void UnregisterLogic(ILogicTickable tickable)
        {
            if (CentralTicker.Instance == null)
                return;

            CentralTicker.Instance.Unregister(tickable);
        }

        public static void RegisterFixed(IFixedUpdatable tickable)
        {
            Assert.IsNotNull(CentralTicker.Instance, $"{nameof(TickManager)}: no {nameof(CentralTicker)} in the scene");

            CentralTicker.Instance.Register(tickable);
        }

        public static void UnregisterFixed(IFixedUpdatable tickable)
        {
            if (CentralTicker.Instance == null)
                return;

            CentralTicker.Instance.Unregister(tickable);
        }
    }
}
