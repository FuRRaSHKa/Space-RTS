using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Gameplay.Ship.Control;
using HalloGames.SpaceRTS.Gameplay.Ship.Stats;
using HalloGames.SpaceRTS.Gameplay.Targets;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Ship
{
    public class ShipTarget : MonoBehaviour, ITargetable
    {
        [SerializeField] private Collider _hullCollider;

        private ShipEntity _shipEntity;
        private ITargetDataObserver _shipDataObserver;

        public Transform TargetTransform => transform;

        public ITargetDataObserver TargetDataObservable => _shipDataObserver;

        public SideData Side => _shipEntity.Side;

        public int ColliderID => _hullCollider.GetInstanceID();

        private void Awake()
        {
            _shipEntity = GetComponent<ShipEntity>();
        }

        private void Start()
        {
            _shipDataObserver = new ShipDataObserver(_shipEntity.DeathHandler, _shipEntity.ShipMovement);
        }

        public void DealDamage(int damage)
        {
            _shipEntity.StatsController.DealDamage(damage);
        }
    }

    public class ShipDataObserver : ITargetDataObserver
    {
        private readonly IDeathHandler _deathHandler;
        private readonly IMovementController _movementController;

        public Vector3 CurrentVelocity => _movementController.CurrentVelocity;
        public IDeathHandler DeathHandler => _deathHandler;

        public ShipDataObserver(IDeathHandler deathHandler, IMovementController movementController)
        {
            _deathHandler = deathHandler;
            _movementController = movementController;
        }
    }
}

namespace HalloGames.SpaceRTS.Gameplay.Targets
{
    public interface ITargetDataObserver
    {
        public Vector3 CurrentVelocity
        {
            get;
        }

        public IDeathHandler DeathHandler
        {
            get;
        }
    }

    public interface ITargetable
    {
        public Transform TargetTransform
        {
            get;
        }

        public int ColliderID
        {
            get;
        }

        public ITargetDataObserver TargetDataObservable
        {
            get;
        }

        public SideData Side
        {
            get;
        }

        public void DealDamage(int damage);
    }
}
