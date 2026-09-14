using System.Linq;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
namespace SurvivorFarm.Runtime.Gameplay
{
    [DefaultExecutionOrder(500)]
    public sealed class TutorialHomestead : MonoBehaviour
    {
        Transform root;ValleyCampaign campaign;
        public void Build(ValleyCampaign owner)
        {
            campaign=owner;
            var outside=FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Outdoor World");
            root=new GameObject("Tutorial · rincones de la granja").transform;root.SetParent(outside,false);
            var tree=FindObjectsByType<ResourceSpawnPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>p.Prefab).OfType<TreeResource>().FirstOrDefault();
            var rock=FindObjectsByType<ResourceSpawnPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>p.Prefab).OfType<RockResource>().FirstOrDefault();
            if(tree==null)tree=FindFirstObjectByType<TreeResource>();if(rock==null)rock=FindFirstObjectByType<RockResource>();
            Vector2[] trees={new(-11,2.6f),new(-9.3f,4.7f),new(-11,5.8f),new(-9.3f,-5.8f),new(-11,-3.7f),new(-9.3f,-1.8f)};
            Vector2[] rocks={new(9.7f,3),new(11.2f,4.3f),new(9.7f,5.6f),new(11.2f,2)};
            for(int i=0;i<trees.Length;i++)Resource(tree,trees[i],"arbol:"+i);
            for(int i=0;i<rocks.Length;i++)Resource(rock,rocks[i],"roca:"+i);
            Sign(new Vector3(-8.7f,1.1f),"Bosquecillo", "Oeste: madera y suministros. Acércate a un árbol para talarlo.");
            Sign(new Vector3(7.25f,-5.75f),"Provisiones de Dalia", "Este: provisiones de Dalia. Consigue ingredientes para cocinar y comida para recuperar vida.");
            var practice=owner.World.Prop("Boss",new Vector3(10.5f,6),.75f);practice.transform.SetParent(root,true);practice.name="Limo de práctica";practice.AddComponent<CircleCollider2D>().radius=.35f;
            practice.AddComponent<TutorialPracticeTarget>().Campaign=owner;
            Cache(new Vector3(-10.6f,1.3f),0);Cache(new Vector3(10.9f,-6.2f),1);
            VillageLayout.ArrangeResources();
        }
        void Resource(HarvestableResource prefab,Vector2 position,string id)
        {
            if(prefab==null)return;
            var instance=Instantiate(prefab,position,Quaternion.identity,root);instance.name="Tutorial "+id;instance.gameObject.SetActive(true);instance.Spawn(position);
            var spawn=ResourceSpawnPoint.Attach(instance,prefab);spawn.Configure(prefab,instance,"tutorial:"+id);
        }
        void Sign(Vector3 position,string title,string help)
        {
            var go=campaign.World.Prop("Sign",position,.45f);go.transform.SetParent(root,true);go.AddComponent<CircleCollider2D>().isTrigger=true;
            var sign=go.AddComponent<TutorialSupply>();sign.Campaign=campaign;sign.Help=help;sign.Title=title;sign.Index=-1;
        }
        void Cache(Vector3 position,int index)
        {
            var go=campaign.World.Prop("Chest",position,.65f);go.transform.SetParent(root,true);go.AddComponent<CircleCollider2D>().isTrigger=true;
            var item=go.AddComponent<TutorialSupply>();item.Campaign=campaign;item.Index=index;item.Title="Suministros del pueblo";
        }
        void Start()
        {
            // Added scenery must never overlap a building from an existing save.
            var buildings=campaign.GetComponent<ConstructionSystem>();
            foreach(var spawn in root.GetComponentsInChildren<ResourceSpawnPoint>())
                if(buildings.Buildings.Any(b=>Vector2.Distance(new Vector2(b.x,b.y),spawn.transform.position)<1.3f))
                {
                    var origin=spawn.transform.position;
                    for(int i=1;i<=12;i++){var p=origin+new Vector3(Mathf.Cos(i*2.4f),Mathf.Sin(i*2.4f))*((i+1)*.3f);if(CultivationGrid.IsGreen(p)&&!Physics2D.OverlapCircleAll(p,.6f).Any(c=>!c.isTrigger&&!c.transform.IsChildOf(spawn.transform))){spawn.transform.position=p;break;}}
                }
        }
        void OnDestroy(){if(root!=null)Destroy(root.gameObject);}
    }
    public sealed class TutorialSupply : WorldInteractable
    {
        public ValleyCampaign Campaign;public int Index;public string Help,Title;
        string Key=>"tutorial:cache:"+Index;
        public override bool IsAvailable=>Index<0||!Campaign.Data.discoveries.Contains(Key);
        public override string GetInteractionLabel(FarmTool tool)=>Index<0?Title+" · leer consejo":"Abrir suministros";
        public override void Interact(FarmTool tool,PlayerInventory inventory)
        {
            if(inventory!=Campaign.Inventory||!IsAvailable||Vector2.Distance(inventory.transform.position,transform.position)>1.5f)return;
            if(Index<0){FarmNotificationCenter.Show(Help);return;}
            Campaign.Data.discoveries.Add(Key);
            if(Index==0){inventory.AddFruit(4);inventory.AddFood(1);Campaign.Changed("Encontraste 4 frutas y una ración. Puedes cocinar con las frutas o comer para recuperar vida.");}
            else {inventory.AddPacked("Fence",3);inventory.AddCoins(10);Campaign.Changed("Encontraste 3 cercas y 10 oro. Desde I puedes elegir dónde colocar cada cerca.");}
        }
    }
    public sealed class TutorialPracticeTarget : MonoBehaviour,IDamageable
    {
        public ValleyCampaign Campaign;int health=3;float resetAt;int generation;
        public bool IsAlive=>health>0;public Transform Transform=>transform;public int SpawnGeneration=>generation;
        public void TakeDamage(int amount,PlayerInventory source)
        {
            if(!IsAlive||amount<=0||source!=Campaign.Inventory)return;
            VisibleHitFeedback.Play(gameObject);health--;
            if(health>0){FarmNotificationCenter.Show("Buen golpe · "+(3-health)+" / 3 impactos");return;}
            resetAt=Time.time+2;GetComponentInChildren<SpriteRenderer>().enabled=false;
            const string key="tutorial:practice";
            if(!Campaign.Data.discoveries.Contains(key)){Campaign.Data.discoveries.Add(key);source.AddCoins(15);Campaign.Changed("¡Práctica completada! +15 oro. Puedes seguir entrenando o explorar los cofres del pueblo.");}
        }
        void Update(){if(health==0&&Time.time>=resetAt){health=3;generation++;GetComponentInChildren<SpriteRenderer>().enabled=true;}}
    }
}
