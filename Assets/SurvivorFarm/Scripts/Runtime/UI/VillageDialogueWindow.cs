using System;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class VillageDialogueWindow : MonoBehaviour
    {
        enum Page { Conversation, Quest, Services, Camps, CampQuest }
        static VillageDialogueWindow active;
        public static bool IsOpen => active != null && active.root != null && active.root.gameObject.activeInHierarchy;
        public static void CloseActive() { if (active != null) active.Close(); }
        public string ResidentId { get; private set; }
        public RectTransform Root => root;
        RectTransform root, speechContent, costsRoot;
        Text nameText, activity, speech, title, status, reward;
        Image portrait;
        readonly Button[] actions = new Button[4];
        readonly Text[] actionLabels = new Text[4];
        MaterialCostBadge[] costs = Array.Empty<MaterialCostBadge>();
        ValleyCampaign campaign;
        ValleyData openedData;
        Transform speaker;
        Page page;
        string reply;
        string selectedCamp;
        float refreshAt;

        public void Configure(Transform canvas)
        {
            if (root != null) return;
            root = AdventureWindow.Rect(canvas, "Conversacion con aldeano", 0, 0, 800, 480);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, 0);
            root.anchoredPosition = new Vector2(0, 20);
            FarmUiStyle.Frame(root.gameObject.AddComponent<Image>());
            portrait = AdventureWindow.Rect(root, "Aldeano", 24, 22, 128, 164).gameObject.AddComponent<Image>();
            portrait.preserveAspect = true; portrait.raycastTarget = false;
            nameText = Label(root, 176, 18, 544, 34, 26);
            activity = Label(root, 176, 58, 558, 28, 16); activity.color = FarmUiStyle.Muted;
            var scroll = FarmUiStyle.Scroll(root, "Dialogo", 176, 96, 596, 98);
            speechContent = scroll.content;
            speech = FarmUiStyle.Paragraph(speechContent, "", 0, 576, 18);
            title = Label(root, 24, 210, 752, 30, 21);
            status = Label(root, 24, 248, 752, 32, 16);
            costsRoot = AdventureWindow.Rect(root, "Materiales del encargo", 24, 294, 752, 32);
            reward = Label(root, 24, 342, 752, 60, 17); reward.color = FarmUiStyle.Accent;
            for (int i = 0; i < actions.Length; i++)
            {
                var rect = AdventureWindow.Rect(root, "Respuesta " + i, 24 + i * 190, 420, 174, 42);
                rect.gameObject.AddComponent<Image>();
                actions[i] = rect.gameObject.AddComponent<Button>(); FarmUiStyle.Button(actions[i]);
                actionLabels[i] = Label(rect, 6, 4, 162, 34, 16); actionLabels[i].alignment = TextAnchor.MiddleCenter;
            }
            var closeRect = AdventureWindow.Rect(root, "Cerrar conversacion", 732, 14, 44, 40);
            closeRect.gameObject.AddComponent<Image>(); var close = closeRect.gameObject.AddComponent<Button>();
            Label(closeRect, 4, 2, 36, 36, 24);
            FarmUiStyle.CloseButton(close); close.onClick.AddListener(Close);
            root.gameObject.SetActive(false);
        }

        public bool Open(ValleyCampaign owner, string residentId, Transform anchor)
        {
            if (owner == null || !VillageQuests.IsVillager(residentId) || anchor == null || root == null ||
                !anchor.gameObject.activeInHierarchy || Vector2.Distance(owner.transform.position, anchor.position) > 2.2f ||
                owner.GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0) return false;
            CloseActive();
            SimpleShopSystem.CloseActive();
            GetComponent<AdventureWindow>()?.Close(); GetComponent<InventoryPanelSystem>()?.Close();
            GetComponent<CraftingWindow>()?.Close(); GetComponent<PlayerEquipmentWindow>()?.Close();
            campaign = owner; openedData = owner.Data; speaker = anchor; ResidentId = residentId;
            owner.GetComponent<ConstructionSystem>()?.Cancel(); owner.GetComponent<PlayerMovementController>()?.StopMovement();
            owner.GetComponent<PlayerCharacterAnimator>()?.CancelAction();
            active = this; page = Page.Conversation; reply = null;
            portrait.sprite = Resources.Load<VillageNpcArtCatalog>("VillageNpcArt")?.Frame(residentId, false, false, 0, 0);
            root.gameObject.SetActive(true); root.SetAsLastSibling();
            RebuildCosts(); Draw(); return true;
        }

        bool CanRespond => active == this && IsOpen && campaign != null && campaign.Data == openedData && speaker != null &&
            speaker.gameObject.activeInHierarchy && Vector2.Distance(campaign.transform.position, speaker.position) <= 2.2f &&
            !(campaign.GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0);

        public void AskForQuest() { if (!CanRespond) return; page = Page.Quest; reply = null; RebuildCosts(); Draw(); }
        public bool AcceptQuest()
        {
            if (!CanRespond || page != Page.Quest || !campaign.AcceptVillageQuest(ResidentId)) return false;
            page = Page.Conversation; reply = "Gracias. Cuando tengas los materiales, vuelve a hablar conmigo y revisaremos la entrega."; Draw(); return true;
        }
        public void DeclineQuest()
        {
            if (!CanRespond) return;
            page = Page.Conversation; reply = "No pasa nada. Volvemos a hablar cuando te venga bien."; Draw();
        }
        public bool DeliverQuest()
        {
            if (!CanRespond || page != Page.Quest || !campaign.CompleteVillageQuest(ResidentId)) { if (CanRespond) Draw(); return false; }
            page = Page.Conversation; reply = "Lo tenemos todo. Gracias por cumplir tu palabra; el pueblo ya tiene algo mas que ayer."; Draw(); return true;
        }
        public void AskAboutVillage() { if (!CanRespond) return; page = Page.Conversation; reply = VillageQuests.SmallTalk(campaign.Data, ResidentId); Draw(); }

        public void AskForCampQuests()
        {
            if (!CanRespond || ResidentId != "village:guard") return;
            page = Page.Camps; selectedCamp = null; Draw();
        }
        public bool SelectCampQuest(string id)
        {
            if (!CanRespond || ResidentId != "village:guard" || CampCombatQuests.Find(id) == null) return false;
            selectedCamp = id; page = Page.CampQuest; RebuildCosts(); Draw(); return true;
        }
        public bool AcceptCampQuest()
        {
            if (!CanRespond || page != Page.CampQuest || !campaign.AcceptCampQuest(selectedCamp)) return false;
            AskForCampQuests(); return true;
        }
        public bool DeliverCampQuest()
        {
            if (!CanRespond || page != Page.CampQuest || !campaign.CompleteCampQuest(selectedCamp)) return false;
            AskForCampQuests(); return true;
        }
        public void DeclineCampQuest() { if (CanRespond && page == Page.CampQuest) AskForCampQuests(); }

        void RebuildCosts()
        {
            foreach (Transform child in costsRoot) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            if (page == Page.CampQuest)
            {
                var combat = CampCombatQuests.Find(selectedCamp);
                MaterialCostBadge.Create(costsRoot, "Coins", 0, 0, 200).Set(combat.Coins, 0, true);
                if (combat.Rubies > 0) MaterialCostBadge.Create(costsRoot, "Ruby", 242, 0, 200).Set(combat.Rubies, 0, true);
                costs = Array.Empty<MaterialCostBadge>();
                return;
            }
            var quest = VillageQuests.Find(ResidentId);
            var entries = page == Page.Services ? new[] { new VillageQuests.Cost("Wood", ResidentId == "village:blacksmith" ? 4 : 5), new VillageQuests.Cost("Stone", 6) } : quest.Costs;
            int count = page == Page.Services && ResidentId != "village:blacksmith" ? 1 : entries.Length;
            costs = new MaterialCostBadge[count];
            for (int i = 0; i < count; i++) costs[i] = MaterialCostBadge.Create(costsRoot, entries[i].ItemId, i * 242, 0, 226);
        }

        void SetSpeech(string text)
        {
            bool changed = speech.text != text;
            speech.text = text;
            float height = Mathf.Max(98, speech.preferredHeight + 6);
            speech.rectTransform.sizeDelta = new Vector2(576, height);
            speechContent.sizeDelta = new Vector2(584, height);
            if (changed) speechContent.anchoredPosition = Vector2.zero;
        }

        void Action(int index, string caption, Action callback, bool enabled = true)
        {
            actions[index].gameObject.SetActive(true); actions[index].interactable = enabled;
            actionLabels[index].text = caption;
            actions[index].onClick.RemoveAllListeners(); actions[index].onClick.AddListener(() => { if (CanRespond) callback(); else Close(); });
        }

        void Draw()
        {
            float height = page == Page.Conversation || page == Page.Camps ? 370 : 480;
            root.sizeDelta = new Vector2(800, height);
            foreach (var button in actions)
            {
                var rect = (RectTransform)button.transform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -(height - 60));
            }
            nameText.text = VillageResidents.Name(ResidentId);
            activity.text = campaign.GetVillagerActivity(ResidentId);
            foreach (var button in actions) button.gameObject.SetActive(false);
            costsRoot.gameObject.SetActive(page == Page.Quest || page == Page.Services || page == Page.CampQuest); reward.text = "";
            var quest = VillageQuests.Find(ResidentId);
            if (page == Page.Conversation)
            {
                SetSpeech(reply ?? VillageResidents.Dialogue(campaign.Data, ResidentId));
                title.text = quest.Title; status.text = VillageQuests.Status(campaign.Data, ResidentId);
                Action(0, VillageQuests.IsComplete(campaign.Data, ResidentId) ? "Encargo completado" : VillageQuests.IsAccepted(campaign.Data, ResidentId) ? "Ver mi encargo" : "\u00bfNecesitas ayuda?", AskForQuest);
                if (ResidentId == "village:guard") Action(1, "Campamentos", AskForCampQuests);
                else Action(1, "Sobre el pueblo", AskAboutVillage);
                Action(2, "Servicios", ShowServices); Action(3, "Hasta luego", Close);
            }
            else if (page == Page.Quest)
            {
                bool done = VillageQuests.IsComplete(campaign.Data, ResidentId), accepted = VillageQuests.IsAccepted(campaign.Data, ResidentId);
                string requirement = VillageQuests.Requirement(campaign.Data, ResidentId);
                SetSpeech(done ? VillageResidents.Dialogue(campaign.Data, ResidentId) : quest.Request);
                title.text = quest.Title;
                status.text = done ? "Misi\u00f3n completada" : requirement != "" ? requirement : accepted ? "En curso. Entrega los materiales cuando est\u00e9s listo." : "\u00bfAceptas este encargo?";
                reward.text = "Recompensa: " + quest.Reward;
                for (int i = 0; i < costs.Length; i++) costs[i].Set(VillageQuests.Owned(campaign.Inventory, quest.Costs[i].ItemId), quest.Costs[i].Amount);
                if (!done)
                {
                    if (accepted) Action(0, "Entregar materiales", () => DeliverQuest(), requirement == "" && VillageQuests.HasMaterials(campaign.Inventory, quest));
                    else Action(0, "Aceptar misi\u00f3n", () => AcceptQuest(), requirement == "");
                    Action(1, accepted ? "Seguir reuniendo" : "Ahora no", DeclineQuest);
                }
                Action(3, "Volver", () => { page = Page.Conversation; reply = null; Draw(); });
            }
            else if (page == Page.Camps || page == Page.CampQuest) DrawCampQuests();
            else DrawServices();
            FarmUiStyle.FitWindow(root);
        }

        void DrawCampQuests()
        {
            if (page == Page.Camps)
            {
                SetSpeech("Cuatro grupos amenazan los caminos de Raizclara. Puedes ocuparte de ellos en el orden que prefieras. Sus suministros seran tuyos al vencer; el pueblo te recompensara aparte cuando regreses.");
                title.text = "Defensa de Raizclara";
                int done = 0; foreach (var q in CampCombatQuests.All) if (CampCombatQuests.IsClaimed(campaign.Data, q.CampId)) done++;
                status.text = "Encargos completados: " + done + "/4";
                reward.text = "";
                string[] names = { "Sur", "Oeste", "Este", "Norte" };
                for (int i = 0; i < CampCombatQuests.All.Length; i++)
                {
                    string id = CampCombatQuests.All[i].CampId;
                    string state = CampCombatQuests.IsClaimed(campaign.Data, id) ? "completo" : CampCombatQuests.Progress(campaign.Data, id).Replace(" enemigos", "");
                    Action(i, names[i] + " - " + state, () => SelectCampQuest(id));
                }
                return;
            }
            var quest = CampCombatQuests.Find(selectedCamp);
            bool claimed = CampCombatQuests.IsClaimed(campaign.Data, selectedCamp);
            bool accepted = CampCombatQuests.IsAccepted(campaign.Data, selectedCamp);
            SetSpeech(claimed ? "Ese camino vuelve a estar a salvo. Ya has recibido la recompensa; gracias por proteger al pueblo." : quest.Request + "\n" + quest.Location + ".");
            title.text = quest.Title;
            status.text = CampCombatQuests.Progress(campaign.Data, selectedCamp) + " - " + CampCombatQuests.Status(campaign.Data, selectedCamp);
            reward.text = (claimed ? "Recompensa entregada: " : "Recompensa de Iria: ") + quest.Reward;
            if (!claimed)
            {
                if (accepted) Action(0, "Informar victoria", () => DeliverCampQuest(), CampCombatQuests.CanClaim(campaign.Data, selectedCamp));
                else Action(0, "Aceptar mision", () => AcceptCampQuest());
                Action(1, accepted ? "Volver" : "Ahora no", DeclineCampQuest);
            }
            Action(2, "Campamentos", AskForCampQuests);
            Action(3, "Hasta luego", Close);
        }

        void ShowServices()
        {
            if (ResidentId == "village:blacksmith" || ResidentId == "village:merchant") { page = Page.Services; RebuildCosts(); Draw(); return; }
            Close();
            if (ResidentId == "village:farmer") FindFirstObjectByType<SimpleShopSystem>()?.OpenService("Food");
            else if (ResidentId == "village:guard") GetComponent<PlayerEquipmentWindow>()?.Open();
            else { GetComponent<AdventureWindow>()?.Open("Journal"); GetComponent<AdventureWindow>()?.SelectJournalTab("Projects"); }
        }

        void DrawServices()
        {
            bool smith = ResidentId == "village:blacksmith";
            title.text = smith ? "Herrajes de Nico" : "Trueque de Rolo";
            status.text = smith ? campaign.WorkshopService.UnavailableReason : campaign.Data.roloMarketOpened ? "5 madera por una racion" : "Primero recupera el mercado.";
            SetSpeech(smith ? campaign.WorkshopService.Summary : "Si te sobra madera, puedo cambiarla por comida para el camino. Tambien puedes consultar el puesto de provisiones.");
            costs[0].Set(campaign.Inventory.Wood, smith ? 4 : 5);
            if (smith) costs[1].Set(campaign.Inventory.Stone, 6);
            reward.text = smith ? campaign.WorkshopService.Effect : "+1 racion";
            Action(0, smith ? "Encargar hierro" : "Confirmar trueque", () => { if (smith) campaign.TryUseWorkshopService(); else campaign.TradeWithRolo(); Draw(); },
                smith ? campaign.WorkshopService.CanUse : campaign.Data.roloMarketOpened && campaign.Inventory.Wood >= 5);
            Action(1, smith ? "Armeria" : "Provisiones", () => { Close(); if (smith) { GetComponent<CraftingWindow>()?.Open(); GetComponent<CraftingWindow>()?.SelectCategory("Equipment"); } else FindFirstObjectByType<SimpleShopSystem>()?.OpenService("Food"); }, !smith || campaign.WorkshopRestored);
            Action(3, "Volver", () => { page = Page.Conversation; reply = null; Draw(); });
        }

        static Text Label(Transform parent, float x, float y, float w, float h, int size)
        {
            var text = AdventureWindow.Rect(parent, "Texto", x, y, w, h).gameObject.AddComponent<Text>(); FarmUiStyle.Text(text, size, true); return text;
        }
        public void Close() { if (active == this) active = null; if (root != null) root.gameObject.SetActive(false); }
        void Update()
        {
            if (active != this) return;
            if (!CanRespond || PlayerRespawnController.MenuOpen || Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (Time.unscaledTime >= refreshAt) { refreshAt = Time.unscaledTime + .5f; Draw(); }
        }
        void OnDisable() => Close();
        void OnDestroy() { Close(); if (root != null) Destroy(root.gameObject); }
    }
}
