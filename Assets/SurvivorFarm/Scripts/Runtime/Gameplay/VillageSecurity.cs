using System;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [Serializable]
    public sealed class VillageSecuritySnapshot
    {
        public bool occupied;
        public int garrisonRemaining;
    }

    /// <summary>Village integrity comes from its homes and people, including fallen residents.</summary>
    public sealed class VillageSecurity : MonoBehaviour
    {
        private PortfolioSession session;
        private VillageHouseHealth[] houses=Array.Empty<VillageHouseHealth>();
        private VillageResidentHealth[] residents=Array.Empty<VillageResidentHealth>();
        private GameObject occupationArt;
        private VillageRecoveryPost recoveryPost;
        private float integrity=1;
        private int lastWarning=100;
        public event Action OccupationStarted;
        public event Action VillageLiberated;
        public bool IsOccupied {get;private set;}
        public float Normalized=>IsOccupied?0:integrity;
        public int Percent=>Mathf.CeilToInt(Normalized*100);
        public int DamagedHouses=>houses.Count(h=>h!=null&&h.Health>0&&h.Health<h.Maximum);
        public int DestroyedHouses=>houses.Count(h=>h!=null&&h.Health==0);
        public int IntactHouses=>houses.Count(h=>h!=null&&h.Health>0);
        public int HouseCount=>houses.Count(h=>h!=null);
        public int LivingResidents=>residents.Count(r=>r!=null&&r.Health>0);
        public int FallenResidents=>residents.Count(r=>r!=null&&r.Health==0);
        public int ResidentCount=>residents.Count(r=>r!=null);
        public int GarrisonRemaining=>IsOccupied&&session!=null&&session.Raids!=null?session.Raids.Alive:0;
        public bool CanLiberate=>IsOccupied&&GarrisonRemaining==0&&IntactHouses>0;
        public Vector3 RecoveryPoint=>recoveryPost!=null?recoveryPost.transform.position:VillageLayout.Well+Vector3.right*1.6f;

        public void Configure(PortfolioSession owner)
        {
            session=owner;
            houses=FindObjectsByType<VillageHouseHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            residents=FindObjectsByType<VillageResidentHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(r=>r.GetComponent<VillageGuard>()==null).ToArray();
            BuildOccupationArt();
            RefreshState();
        }

        public void RefreshState()
        {
            float score=0;int count=0;
            foreach(var home in houses)if(home!=null){score+=home.Health/(float)home.Maximum;count++;}
            foreach(var person in residents)if(person!=null){score+=person.Health/(float)person.Maximum;count++;}
            integrity=count>0?Mathf.Clamp01(score/count):1;
            if(session==null||session.IsPractice||!session.HasBegun||session.Phase==SlicePhase.Defeat||session.Phase==SlicePhase.Victory)return;
            if(IsOccupied)return;
            int warning=Percent<=25?25:Percent<=50?50:100;
            if(warning<lastWarning)
                FarmNotificationCenter.Show(warning==25?"¡Raízclara está al borde de caer! Protege las casas y auxilia a los aldeanos.":"La seguridad del pueblo ha bajado al 50 %. Repara las casas y auxilia a los heridos.");
            lastWarning=warning;
            if(count>0&&integrity<=0)BeginOccupation(4);
        }

        private void BeginOccupation(int remaining)
        {
            IsOccupied=true;
            occupationArt?.SetActive(true);
            // The session suspends its clock and cancels the old encounter before the garrison spawns.
            OccupationStarted?.Invoke();
            session.Raids?.BeginOccupationGarrison(remaining);
            FarmNotificationCenter.Show("RAÍZCLARA OCUPADA · Elimina a los invasores, reconstruye una casa y recupera la plaza [E].");
        }

        public bool Liberate(PlayerInventory inventory)
        {
            if(!CanLiberate||inventory==null||inventory!=session.Player||session.IsPaused||
                inventory.GetComponent<PlayerSurvivalStats>()?.CurrentHealth<=0||
                Vector2.Distance(inventory.transform.position,RecoveryPoint)>1.65f)return false;
            IsOccupied=false;
            occupationArt?.SetActive(false);
            RefreshState();
            VillageLiberated?.Invoke();
            FarmNotificationCenter.Show("Raízclara vuelve a ser tu hogar. Repara sus casas y prepara el próximo viaje.");
            return true;
        }

        public VillageSecuritySnapshot Capture()=>new VillageSecuritySnapshot{occupied=IsOccupied,garrisonRemaining=GarrisonRemaining};

        public void Restore(VillageSecuritySnapshot data)
        {
            IsOccupied=false;occupationArt?.SetActive(false);lastWarning=100;
            // Restore must be invoked after all house and resident snapshots have been applied.
            if(data!=null&&data.occupied)BeginOccupation(Mathf.Clamp(data.garrisonRemaining,0,4));
            RefreshState();
        }

        private void BuildOccupationArt()
        {
            if(session.IsPractice||occupationArt!=null)return;
            occupationArt=new GameObject("Raízclara · ocupación de monstruos");
            occupationArt.transform.SetParent(transform,false);
            var world=FindFirstObjectByType<ValleyWorld>();
            if(world!=null)
            {
                foreach(var point in new[]{new Vector3(-3,-1.2f),new Vector3(3,-1.2f)})
                {
                    var portal=world.Prop("Portal",point,1.4f);
                    portal.name="Marca de los invasores";portal.transform.SetParent(occupationArt.transform,true);
                    var art=portal.GetComponentInChildren<SpriteRenderer>();
                    if(art!=null)art.color=new Color(.95f,.55f,1f,.9f);
                }
                var banner=world.Label("RAÍZCLARA OCUPADA",new Vector3(0,2.9f),.075f);
                banner.color=new Color(1,.55f,.7f);banner.transform.SetParent(occupationArt.transform,true);
            }
            var post=new GameObject("Recuperar la plaza [E]");
            post.transform.SetParent(transform,false);post.transform.position=VillageLayout.Well+Vector3.right*1.6f;
            recoveryPost=post.AddComponent<VillageRecoveryPost>();recoveryPost.Configure(this);
            occupationArt.SetActive(false);
        }
    }

    public sealed class VillageRecoveryPost : WorldInteractable
    {
        private VillageSecurity security;
        public void Configure(VillageSecurity owner)=>security=owner;
        public override bool IsAvailable=>security!=null&&security.IsOccupied;
        protected override float HighlightScale=>1;
        public override string GetInteractionLabel(FarmTool tool)=>security.GarrisonRemaining>0?
            $"Pueblo ocupado · derrota a {security.GarrisonRemaining} invasores":security.IntactHouses==0?
            "Reconstruye una casa · 2 madera junto a sus ruinas":"Recuperar Raízclara [E]";
        public override void Interact(FarmTool tool,PlayerInventory inventory)
        {
            if(!security.Liberate(inventory))FarmNotificationCenter.Show(GetInteractionLabel(tool));
        }
    }
}
