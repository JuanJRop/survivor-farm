using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class DungeonExpeditionHud : MonoBehaviour
    {
        private DungeonExpedition expedition;
        private Transform player;
        private GameObject hud;
        private Text title;
        private Image fill;
        public void Configure(DungeonExpedition owner, Transform target)
        {
            expedition = owner; player = target;
            hud = new GameObject("Dungeon room and boss HUD", typeof(Canvas), typeof(CanvasScaler));
            hud.transform.SetParent(transform, false);
            var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 35;
            var scaler = hud.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var panel = new GameObject("Dungeon status", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(hud.transform, false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -12); rect.sizeDelta = new Vector2(350, 66);
            FarmUiStyle.Frame(panel.GetComponent<Image>()); panel.GetComponent<Image>().raycastTarget = false;
            var text = new GameObject("Room", typeof(RectTransform), typeof(Text)); text.transform.SetParent(panel.transform, false);
            title = text.GetComponent<Text>(); FarmUiStyle.Text(title, 17); title.alignment = TextAnchor.MiddleCenter;
            title.rectTransform.anchorMin = Vector2.zero; title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = new Vector2(10, 15); title.rectTransform.offsetMax = new Vector2(-10, -5);
            var bar = new GameObject("Boss health", typeof(RectTransform), typeof(Image)); bar.transform.SetParent(panel.transform, false);
            fill = bar.GetComponent<Image>(); fill.rectTransform.anchorMin = new Vector2(0, 0); fill.rectTransform.anchorMax = new Vector2(1, 0);
            fill.rectTransform.pivot = new Vector2(0, 0); fill.rectTransform.offsetMin = new Vector2(14, 9); fill.rectTransform.offsetMax = new Vector2(-14, 15);
            fill.sprite = Resources.Load<Sprite>("BackpackIcons/Panel"); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = new Color(.85f, .26f, .28f); fill.raycastTarget = false;
        }
        private void LateUpdate()
        {
            if (hud == null) return;
            hud.SetActive(expedition.IsPresent);
            if (!expedition.IsPresent) return;
            bool combat = expedition.BossFightActive;
            int room = DungeonLayout.RoomAt(player.position);
            title.text = combat ? "EL CUSTODIO  " + expedition.Boss.CurrentHealth + " / " + expedition.Boss.MaximumHealth + "\n" + expedition.Boss.PhaseLabel :
                "RUINAS BAJO RAIZCLARA\n" + (room >= 0 ? DungeonLayout.Names[room] : "Galerias de enlace");
            fill.enabled = combat; fill.fillAmount = (float)expedition.Boss.CurrentHealth / expedition.Boss.MaximumHealth;
        }
    }
}
