using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public static class PcControlsLayout
    {
        public static void Apply(Canvas canvas)
        {
            if (canvas == null) return;
            Input.simulateMouseWithTouches = false;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.matchWidthOrHeight = 1f;
            }
            foreach (var t in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name == "HUD Attack" || t.name == "HUD Interact" || t.name == "Attack Button" || t.name == "Interact Button")
                    t.gameObject.SetActive(false);
                if (t.name == "Tools")
                {
                    t.anchorMin = t.anchorMax = new Vector2(.5f, 0);
                    t.pivot = new Vector2(.5f, 0);
                    t.anchoredPosition = new Vector2(0, 12);
                }
                if (t.name == "Mobility Value") t.GetComponent<Text>().text = "WASD + ratón";
                if (t.name == "Mobility Label") t.GetComponent<Text>().text = "Controles PC";
                if (t.name == "Context Text") t.GetComponent<Text>().text = "WASD: mover · Clic izq.: atacar · Clic der.: usar";
            }
        }
    }
}
