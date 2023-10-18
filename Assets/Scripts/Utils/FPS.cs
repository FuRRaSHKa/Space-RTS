using System;
using TMPro;
using UnityEngine;

namespace HalloGames.Utilities
{
    public class FPS : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private void Update()
        {
            _text.text = String.Format("{0:0}", (1f / Time.deltaTime));
        }
    }
}

