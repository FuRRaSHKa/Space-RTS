using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Gameplay.Ship;
using System.Collections.Generic;

namespace HalloGames.SpaceRTS.Management.ShipManagement
{
    public interface IShipRegistry : IService
    {
        public IReadOnlyList<ShipEntity> Ships
        {
            get;
        }

        public void AddShip(ShipEntity shipEntity);
    }

    public class ShipRegistry : IShipRegistry
    {
        private List<ShipEntity> _ships = new List<ShipEntity>();

        public IReadOnlyList<ShipEntity> Ships => _ships;

        public void AddShip(ShipEntity shipEntity)
        {
            _ships.Add(shipEntity);
        }
    }
}
