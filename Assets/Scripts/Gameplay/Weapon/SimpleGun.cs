using HalloGames.SpaceRTS.Gameplay.Targets;
using System;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Guns
{
    public class RayWeapon : MonoBehaviour, IWeapon
    {
        public event Action<ITargetable> OnStartShooting;
        public event Action<ITargetable> OnTargetDeath;

        public void StopShooting()
        {

        }

        public void StartShooting(ITargetable targetable)
        {

        }
    }

    public interface IWeapon
    {
        public event Action<ITargetable> OnStartShooting;

        public void StartShooting(ITargetable targetable);

        public void StopShooting();
    }
}
