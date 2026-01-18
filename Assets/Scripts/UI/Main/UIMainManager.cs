using Ships;
using TMPro;
using UnityEngine;

namespace UI.Main
{
    public class UIMainManager : MonoBehaviour
    {
        public Ship playerShip;
        public TextMeshProUGUI speedText;

        private void Update()
        {
            speedText.SetText(
                playerShip?.CommandModule?.PixelatedRigidbody?.Rigidbody?.linearVelocity.magnitude.ToString("F1"));
        }
    }
}