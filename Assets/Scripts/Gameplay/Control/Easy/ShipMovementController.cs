using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public interface IMovementController
{
    public void MoveTo(Vector3 targetPos);
}

public class ShipMovementController : MonoBehaviour, IMovementController
{
    [SerializeField] private NavMeshAgent _navMesh;

    public void MoveTo(Vector3 targetPos)
    {
        _navMesh.SetDestination(targetPos); 
    }
}
