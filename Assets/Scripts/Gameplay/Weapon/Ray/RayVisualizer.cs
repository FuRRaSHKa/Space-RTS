using HalloGames.SpaceRTS.Gameplay.Guns.Targeter;
using HalloGames.SpaceRTS.Gameplay.Targets;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Guns.Graphic
{
    public class RayVisualizer : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particleSystem;
        [SerializeField] private Transform _shootPoint;
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private float _duration;
        [SerializeField] private float _rayFlyDuration;

        private Transform _target;

        private float _curTime;
        private IShooter _shooter;
        private bool _visualize = false;

        private void Awake()
        {
            _shooter = GetComponent<IShooter>();
            _shooter.OnShooting += ShowShootEffect;
        }

        private void ShowShootEffect(ITargetable targetable)
        {
            _particleSystem.Play();
            _visualize = true;
            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(1, _shootPoint.position);
            _target = targetable.TargetTransform;
            _curTime = 0;
        }

        private void EndVisualize()
        {
            _lineRenderer.enabled = false;
            _visualize = false;
        }

        private void Update()
        {
            if (!_visualize)
                return;

            _curTime += Time.deltaTime;
            if (_curTime > _duration)
            {
                EndVisualize();
                return;
            }

            _lineRenderer.SetPosition(0, _shootPoint.position);

            Vector3 pos = Vector3.Lerp(_shootPoint.position, _target.position, _curTime / _rayFlyDuration);
            _lineRenderer.SetPosition(1, pos);

            LerpAlpha();
        }

        private void LerpAlpha()
        {
            Material material = _lineRenderer.material;
            Color color = material.color;
            color.a = Mathf.Lerp(1, 0, _curTime / _duration);
            material.color = color;

            _lineRenderer.material = material;
        }
    }

}

