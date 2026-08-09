using HalloGames.Architecture.Initializer;
using HalloGames.SpaceRTS.Data.Weapon;
using HalloGames.SpaceRTS.Gameplay.Targets;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Guns.Targeter
{
    public interface IWeaponTargeter
    {
        public float AngleDelta
        {
            get;
        }

        public void StartFollowing(ITargetable targetable);
        public void StopFollowing();
    }

    public class SimpleWeaponTargeter : MonoBehaviour, IWeaponTargeter, IInitializable<WeaponData>
    {
        [SerializeField] private Transform _rotationPart;
        [SerializeField] private Transform _basement;

        private float _rotationSpeed;

        private Quaternion _currentRotation;
        private Quaternion _targetRotation;

        private ITargetable _targetable;

        public float AngleDelta => Quaternion.Angle(_rotationPart.rotation, _targetRotation);

        public void StartFollowing(ITargetable targetable)
        {
            _targetable = targetable;
        }

        public void StopFollowing()
        {
            _targetable = null;
        }

        private void Update()
        {
            if (_targetable == null)
                RotateToDefault();
            else
                Rotate();
        }

        private void RotateToDefault()
        {
            _currentRotation = Quaternion.LookRotation(_basement.forward, _basement.up);
            _rotationPart.rotation = Quaternion.RotateTowards(_rotationPart.rotation, _currentRotation, _rotationSpeed * Time.deltaTime);
        }

        private void Rotate()
        {
            Vector3 direction = (_targetable.TargetTransform.position - _rotationPart.transform.position).normalized;
            _targetRotation = Quaternion.LookRotation(direction, _basement.up);

            //Clamp turret rotation
            Vector3 localDirection = _basement.InverseTransformDirection(direction).normalized;
            localDirection.y = Mathf.Clamp(localDirection.y, -.1f, .7f);

            direction = _basement.TransformDirection(localDirection).normalized;

            _currentRotation = Quaternion.LookRotation(direction, _basement.up);
            _rotationPart.rotation = Quaternion.RotateTowards(_rotationPart.rotation, _currentRotation, _rotationSpeed * Time.deltaTime);
        }

        public void Init(WeaponData data)
        {
            _rotationSpeed = data.RotationSpeed;
        }
    }
}