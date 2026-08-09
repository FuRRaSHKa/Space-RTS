using HalloGames.Architecture.Initializer;
using HalloGames.SpaceRTS.Data.Weapon;
using HalloGames.SpaceRTS.Gameplay.Guns.Targeter;
using HalloGames.SpaceRTS.Gameplay.Targets;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Guns
{
    public class RocketTargeter : MonoBehaviour, IWeaponTargeter, IInitializable<WeaponData>
    {
        private ITargetable _targetable;

        public float AngleDelta => 0;

        public void Init(WeaponData data)
        {

        }

        public void StartFollowing(ITargetable targetable)
        {
            _targetable = targetable;
        }

        public void StopFollowing()
        {
            _targetable = null;
        }
    }
}


