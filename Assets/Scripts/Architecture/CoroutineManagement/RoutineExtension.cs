using System;
using System.Collections;
using UnityEngine;

namespace HalloGames.Architecture.CoroutineManagement
{
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

        public static Routine WaitIfCondition(this Routine routine, float time, Func<bool> condition, Action callback = null)
        {
            WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

            IEnumerator WaitCoroutine()
            {
                float remainingTime = time;
                while (remainingTime > 0f)
                {
                    yield return waitForEndOfFrame;

                    if (!condition())
                    {
                        remainingTime = time;
                        continue;
                    }

                    remainingTime -= Time.deltaTime;
                }

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

        public static Routine EndOfFrame(this Routine routine, Action callback = null)
        {
            IEnumerator NextFrame()
            {
                yield return new WaitForEndOfFrame();
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

        public static Routine AnimateColor(this Routine routine, Color startValue, Color endValue, Action<Color> setter, float duration)
        {
            return AnimateValue(routine, startValue, endValue, setter, Color.Lerp, duration);
        }

        public static Routine AnimateFloat(this Routine routine, float startValue, float endValue, Action<float> setter, float duration)
        {
            return AnimateValue(routine, startValue, endValue, setter, Mathf.Lerp, duration);
        }

        public static Routine AnimateFloat(this Routine routine, float startValue, float endValue, Action<float> setter, AnimationCurve ease, float duration)
        {
            return AnimateValue(routine, startValue, endValue, setter, LerpWithEase, duration);

            float LerpWithEase(float start, float end, float value)
            {
                return Mathf.Lerp(start, end, ease.Evaluate(value));
            }
        }

        public static Routine AnimateValue<T>(this Routine routine, T startValue, T endValue, Action<T> setter, Func<T, T, float, T> lerp, float duration)
        {
            if (duration <= 0f)
            {
                setter.Invoke(endValue);
                return routine;
            }

            setter.Invoke(startValue);
            float animTime = 0f;

            return EveryAfterUpdate(routine, OnUpdate, Condition);

            void OnUpdate()
            {
                animTime = Mathf.Clamp(animTime + Time.deltaTime, 0f, duration);
                T curValue = lerp.Invoke(startValue, endValue, animTime / duration);
                setter.Invoke(curValue);
            }

            bool Condition() => animTime < duration;
        }

    }
}