using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Gameplay.Projectel;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.ProjectelManagement
{
    public class RocketsController : ProjectelController<RocketWrapper>, IService
    {
        protected override void SheludeMoving()
        {
            deltaTime = Time.deltaTime;

            for (int i = 0; i < shootedProjectel.Count; i++)
            {

                CalculateRocketMove(shootedProjectel[i], out Vector3 pos, out Vector3 moveDelta);

                RaycastCommand raycastCommand = new RaycastCommand(pos, moveDelta, moveDelta.magnitude, layerMask.value);
                raycastCommands.AddNoResize(raycastCommand);

                shootedProjectel[i].Transform.position += moveDelta;
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