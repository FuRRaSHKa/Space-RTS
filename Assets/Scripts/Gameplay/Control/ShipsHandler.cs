using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HalloGames.SpaceRTS.Gameplay.Ship;
using HalloGames.SpaceRTS.Data.Enums;

namespace HalloGames.SpaceRTS.Management.ShipManagement
{
    public class ShipsHandler : MonoBehaviour
    {
        private Dictionary<SideData, List<ShipEntity>> _ships = new Dictionary<SideData, List<ShipEntity>>();

        public void AddShip(ShipEntity shipEntity, SideData side)
        {
            if (!_ships.ContainsKey(side))
                _ships.Add(side, new List<ShipEntity>(1));

            _ships[side].Add(shipEntity);
        }

        public List<ShipEntity> GetChosedShips(Vector3 firstPos, Vector3 secondPos, SideData side)
        {
            Vector3 size = (secondPos - firstPos) / 2;
            Bounds bounds = new Bounds(firstPos + size, size);
            return _ships[side].Where(w =>
            {
                Vector3 pos = w.transform.position;
                pos.y = bounds.center.y;
                return bounds.Contains(pos);
            }).ToList();
        }
    }
}