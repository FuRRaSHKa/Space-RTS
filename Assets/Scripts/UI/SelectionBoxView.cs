using UnityEngine;

namespace HalloGames.SpaceRTS.UI
{
    public class SelectionBoxView : MonoBehaviour
    {
        [SerializeField] private RectTransform _boxRect;

        public void Show(Vector2 startScreenPos)
        {
            if (_boxRect == null)
                return;

            _boxRect.gameObject.SetActive(true);
            UpdateBox(startScreenPos, startScreenPos);
        }

        public void UpdateBox(Vector2 startScreenPos, Vector2 currentScreenPos)
        {
            if (_boxRect == null)
                return;

            var min = Vector2.Min(startScreenPos, currentScreenPos);
            var max = Vector2.Max(startScreenPos, currentScreenPos);

            _boxRect.anchoredPosition = min;
            _boxRect.sizeDelta = max - min;
        }

        public void Hide()
        {
            if (_boxRect == null)
                return;

            _boxRect.gameObject.SetActive(false);
        }
    }
}
