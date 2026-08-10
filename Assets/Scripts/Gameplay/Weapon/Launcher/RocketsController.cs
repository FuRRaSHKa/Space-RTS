using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Gameplay.Projectile;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.ProjectileManagement
{
    public class RocketsController : ProjectileController<RocketWrapper>, IService
    {
        protected override void ScheduleMoving()
        {
            for (int i = 0; i < ShotProjectile.Count; i++)
            {

                CalculateRocketMove(ShotProjectile[i], out Vector3 pos, out Vector3 moveDelta);

                RaycastCommand raycastCommand = new RaycastCommand(pos, moveDelta, moveDelta.magnitude, LayerMaskValue);
                AddRaycastCommand(raycastCommand);

                ShotProjectile[i].Transform.position += moveDelta;
            }
        }

        private void CalculateRocketMove(RocketWrapper rocket, out Vector3 pos, out Vector3 moveDelta)
        {
            RocketMovementStruct rocketMovementStruct = rocket.RocketMovement;
            pos = rocket.Transform.position;

            float rotationSpeed = Mathf.MoveTowards(rocketMovementStruct.currentRotationSpeed, rocketMovementStruct.maxRotationSpeed, rocketMovementStruct.rotationAcceleration * DeltaTime);
            rocket.Transform.rotation = Quaternion.RotateTowards(rocket.Transform.rotation, Quaternion.LookRotation((rocket.TargetPos - rocket.Transform.position).normalized), rocketMovementStruct.currentRotationSpeed * DeltaTime);

            float speed = Mathf.MoveTowards(rocketMovementStruct.currentSpeed, rocketMovementStruct.maxSpeed, rocketMovementStruct.acceleration * DeltaTime);
            Vector3 currentDirection = rocket.Transform.forward * speed;

            moveDelta = currentDirection * DeltaTime;
            rocket.MoveRocket(currentDirection, speed, rotationSpeed);
        }
    }
}