using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.Core
{
    public sealed class DayNightCycle : MonoBehaviour
    {
        [SerializeField, Min(10f)] private float cycleDurationSeconds = 900f;
        [SerializeField, Range(0f, 24f)] private float hour = 8f;
        [SerializeField, Min(1)] private int day = 1;
        [SerializeField] private Color nightTint = new Color(0.04f, 0.07f, 0.2f, 0.48f);
        [SerializeField] private Color sunsetTint = new Color(0.5f, 0.2f, 0.08f, 0.16f);
        [SerializeField] private Image worldTint;
        [SerializeField] private ShopEntrance shop;
        [SerializeField] private DungeonEntrance dungeon;

        public float Hour => hour;
        public int Day => day;
        public bool IsNight => hour >= 21f || hour < 5f;

        public bool TrySleepUntilMorning()
        {
            if (PortfolioSession.Active) return false;
            if (!IsNight) return false;
            Restore(hour >= 21f ? day + 1 : day, 8f);
            FarmGameEvents.RaiseSleptUntilMorning();
            return true;
        }
        public float CycleDurationSeconds => cycleDurationSeconds;

        public void Configure(Image overlay, ShopEntrance shopEntrance, DungeonEntrance dungeonEntrance)
        {
            worldTint = overlay;
            shop = shopEntrance;
            dungeon = dungeonEntrance;
            RefreshVisuals();
        }

        private void Awake() { if(Mathf.Approximately(cycleDurationSeconds,600f))cycleDurationSeconds=900f; }

        private void Update()
        {
            if (PortfolioSession.Active) return;
            bool wasNight=IsNight; float previous=hour;
            Advance(Time.deltaTime);
            if(previous<19f && hour>=19f)SurvivorFarm.Runtime.UI.FarmNotificationCenter.Show("Anochece pronto. Cocina, equipa tu arma y prepara la cama.");
            if(wasNight!=IsNight)SurvivorFarm.Runtime.UI.FarmNotificationCenter.Show(IsNight?"Noche: los enemigos son más numerosos y rápidos.":"Amaneció. Puedes volver a explorar y reconstruir.");
        }

        private void LateUpdate() => RefreshVisuals();

        public void Advance(float seconds)
        {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            double totalHours = hour + (double)seconds * 24d / Mathf.Max(10f, cycleDurationSeconds);
            day += (int)(totalHours / 24d);
            hour = (float)(totalHours % 24d);
        }

        public void Restore(int savedDay, float savedHour)
        {
            day = Mathf.Max(1, savedDay);
            hour = float.IsNaN(savedHour) || float.IsInfinity(savedHour) ? 8f : Mathf.Repeat(savedHour, 24f);
            RefreshVisuals();
        }

        public Color EvaluateTint(float timeOfDay)
        {
            float time = Mathf.Repeat(timeOfDay, 24f);
            if (time < 5f || time >= 21f) return nightTint;
            if (time < 8f) return Color.Lerp(nightTint, Color.clear, Mathf.SmoothStep(0f, 1f, (time - 5f) / 3f));
            if (time < 17f) return Color.clear;
            if (time < 19f) return Color.Lerp(Color.clear, sunsetTint, Mathf.SmoothStep(0f, 1f, (time - 17f) / 2f));
            return Color.Lerp(sunsetTint, nightTint, Mathf.SmoothStep(0f, 1f, (time - 19f) / 2f));
        }

        private void RefreshVisuals()
        {
            if (worldTint == null) return;
            worldTint.raycastTarget = false;
            bool indoors = (shop != null && shop.IsInsideShop) || (dungeon != null && dungeon.IsInsideDungeon);
            Color tint=indoors?Color.clear:EvaluateTint(hour);
            worldTint.color=PortfolioSession.Active?Color.Lerp(worldTint.color,tint,1-Mathf.Exp(-Time.unscaledDeltaTime*1.8f)):tint;
        }

        private void OnDisable()
        {
            if (worldTint != null) worldTint.color = Color.clear;
        }
    }
}
