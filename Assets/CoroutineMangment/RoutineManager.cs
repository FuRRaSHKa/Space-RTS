using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoutineManager : MonoSingletone<RoutineManager>
{
    public static Routine CreateRoutine(IEnumerator enumerator, MonoBehaviour coroutineHost = null)
    {
        return new Routine(coroutineHost == null ? Instance : coroutineHost, enumerator);
    }

    public static Routine CreateRoutine(MonoBehaviour coroutineHost = null)
    {
        return new Routine(coroutineHost == null ? Instance : coroutineHost);
    }

    public void StopAllRoutines()
    {
        StopAllCoroutines();
    }
}
