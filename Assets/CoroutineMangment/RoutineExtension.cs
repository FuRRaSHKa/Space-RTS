using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoutineExtension
{
    public static Routine Wait(this Routine routine, float time, Action callback = null)
    {
        WaitForSeconds waitForFixedUpdate = new WaitForSeconds(time);

        IEnumerator WaitCoroutine()
        {
            yield return waitForFixedUpdate;
            callback?.Invoke();
        }

        return routine.SetEnumerator(WaitCoroutine());
    }

    public static Routine RepeatWaiting(this Routine routine, float time, Action callback = null)
    {
        WaitForSeconds waitForFixedUpdate = new WaitForSeconds(time);

        IEnumerator WaitCoroutine()
        {
            while (true)
            {
                yield return waitForFixedUpdate;
                callback?.Invoke();
            }
        }

        return routine.SetEnumerator(WaitCoroutine());
    }

    public static Routine RepeatWaiting(this Routine routine, float time, Func<bool> condition, Action callback = null)
    {
        WaitForSeconds waitForFixedUpdate = new WaitForSeconds(time);

        IEnumerator WaitCoroutine()
        {
            yield return waitForFixedUpdate;

            while (condition())
            {
                callback?.Invoke();
                yield return waitForFixedUpdate;
            }
        }

        return routine.SetEnumerator(WaitCoroutine());
    }

    public static Routine NextFrame(this Routine routine, Action callback = null)
    {
        IEnumerator NextFrame()
        {
            yield return null;
            callback?.Invoke();
        }

        return routine.SetEnumerator(NextFrame());
    }

    public static Routine SkipFrames(this Routine routine, int frameCount, Action callback = null)
    {
        IEnumerator NextFrame()
        {
            while (frameCount > 0)
            {
                yield return null;
                callback?.Invoke();
            }
        }

        return routine.SetEnumerator(NextFrame());
    }

    public static Routine EveryAfterUpdate(this Routine routine, Action onUpdate = null, Func<bool> condition = null)
    {
        IEnumerator AfterUpdate()
        {
            while (condition())
            {
                yield return true;
                onUpdate?.Invoke();
            }
        }

        return routine.SetEnumerator(AfterUpdate());
    }

    public static Routine EveryAfterFixedUpdate(this Routine routine, Action onUpdate = null)
    {
        WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

        IEnumerator FixedCoroutine()
        {
            while (true)
            {
                yield return waitForFixedUpdate;
                onUpdate?.Invoke();
            }
        }

        return routine.SetEnumerator(FixedCoroutine());
    }


}
