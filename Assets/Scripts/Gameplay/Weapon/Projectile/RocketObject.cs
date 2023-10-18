using HalloGames.Architecture.CoroutineManagement;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Projectel
{
    public class RocketObject : ProjectelObject
    {
        [SerializeField] private GameObject _gfx;
        [SerializeField] private ParticleSystem[] _trails;

        private IStopable _stopable;

        public override void DisableObject()
        {
            foreach (var trail in _trails)
            {
                trail.Stop();
            }

            _gfx.SetActive(false);

            _stopable?.Stop();
            _stopable = RoutineManager.CreateRoutine(this)
                .Wait(1f, base.DisableObject)
                .Start();
        }

        public override void EnableObject()
        {
            foreach (var trail in _trails)
            {
                trail.Play();
            }

            _gfx.SetActive(true);
            base.EnableObject();
        }
    }
}
