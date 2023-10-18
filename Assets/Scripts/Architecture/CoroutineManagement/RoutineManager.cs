using HalloGames.Architecture.Singletones;
using System.Collections;
using UnityEngine;

namespace HalloGames.Architecture.CoroutineManagement
{
    public class RoutineManager : MonoSingleton<RoutineManager>
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
}