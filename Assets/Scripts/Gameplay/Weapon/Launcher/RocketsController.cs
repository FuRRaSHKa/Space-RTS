using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Gameplay.Projectile;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.ProjectileManagement
{
    public class RocketsController : ProjectileController<RocketWrapper>, IService
    {
        protected override void ScheduleMoving()
        {
            deltaTime = Time.deltaTime;

            for (int i = 0; i < shotProjectile.Count; i++)
            {

                CalculateRocketMove(shotProjectile[i], out Vector3 pos, out Vector3 moveDelta);

                RaycastCommand raycastCommand = new RaycastCommand(pos, moveDelta, moveDelta.magnitude, layerMask.value);
                raycastCommands.AddNoResize(raycastCommand);

                shotProjectile[i].Transform.position += moveDelta;
            }
        }

        private void CalculateRocketMove(RocketWrapper rocket, out Vector3 pos, out Vector3 moveDelta)
        {
            RocketMovementStruct rocketMovementStruct = rocket.RocketMovement;
            pos = rocket.Transform.position;

            float rotationSpeed = Mathf.MoveTowards(rocketMovementStruct.currentRotationSpeed, rocketMovementStruct.maxRotationSpeed, rocketMovementStruct.rotationAcceleration * deltaTime);
            rocket.Transform.rotation = Quaternion.RotateTowards(rocket.Transform.rotation, Quaternion.LookRotation((rocket.TargetPos - rocket.Transform.position).normalized), rocketMovementStruct.currentRotationSpeed * deltaTime);

            float speed = Mathf.MoveTowards(rocketMovementStruct.currentSpeed, rocketMovementStruct.maxSpeed, rocketMovementStruct.acceleration * deltaTime);
            Vector3 currentDirection = rocket.Transform.forward * speed;

            moveDelta = currentDirection * deltaTime;
            rocket.MoveRocket(currentDirection, speed, rotationSpeed);
        }
    }
}