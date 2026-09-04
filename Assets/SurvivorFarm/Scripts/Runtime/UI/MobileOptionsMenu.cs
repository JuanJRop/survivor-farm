using UnityEngine;
using UnityEngine.UI;
using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class MobileOptionsMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Image brightnessOverlay;
        [SerializeField] private PlayerMovementController movement;
        [SerializeField] private Text volumeText;
        [SerializeField] private Text brightnessText;
        [SerializeField] private Text languageText;
        [SerializeField] private Text mobilityText;

        private float brightness = 1f;
        private bool spanish = true;

        public void Configure(
            GameObject optionsPanel,
            Image overlay,
            PlayerMovementController playerMovement,
            Text volumeValue,
            Text brightnessValue,
            Text languageValue,
            Text mobilityValue)
        {
            panel = optionsPanel;
            brightnessOverlay = overlay;
            movement = playerMovement;
            volumeText = volumeValue;
            brightnessText = brightnessValue;
            languageText = languageValue;
            mobilityText = mobilityValue;
            movement?.SetMovementMode(PlayerMovementController.MobileMovementMode.TapToMove);
            Refresh();
        }

        public void TogglePanel()
        {
            if (panel != null)
            {
                panel.SetActive(!panel.activeSelf);
            }
        }

        public void IncreaseVolume()
        {
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume + 0.1f);
            Refresh();
        }

        public void DecreaseVolume()
        {
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume - 0.1f);
            Refresh();
        }

        public void IncreaseBrightness()
        {
            brightness = Mathf.Clamp01(brightness + 0.1f);
            Refresh();
        }

        public void DecreaseBrightness()
        {
            brightness = Mathf.Clamp01(brightness - 0.1f);
            Refresh();
        }

        public void ToggleLanguage()
        {
            spanish = !spanish;
            Refresh();
        }

        public void UseTapMovement()
        {
            SetMovementMode(PlayerMovementController.MobileMovementMode.TapToMove);
        }

        private void SetMovementMode(PlayerMovementController.MobileMovementMode mode)
        {
            if (movement != null)
            {
                movement.SetMovementMode(mode);
            }

            Refresh();
        }

        private void Refresh()
        {
            if (brightnessOverlay != null)
            {
                float overlayAlpha = Mathf.Lerp(0.42f, 0f, brightness);
                brightnessOverlay.color = new Color(0f, 0f, 0f, overlayAlpha);
                brightnessOverlay.raycastTarget = false;
            }

            if (volumeText != null)
            {
                volumeText.text = $"{Mathf.RoundToInt(AudioListener.volume * 100f)}%";
            }

            if (brightnessText != null)
            {
                brightnessText.text = $"{Mathf.RoundToInt(brightness * 100f)}%";
            }

            if (languageText != null)
            {
                languageText.text = spanish ? "Español" : "English";
            }

            if (mobilityText != null && movement != null)
            {
                mobilityText.text = "Tocar destino";
            }
        }
    }
}
