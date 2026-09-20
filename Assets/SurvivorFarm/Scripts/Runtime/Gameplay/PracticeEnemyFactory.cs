using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Adapts authored creature templates and raid roles without duplicating their AI.</summary>
    public sealed class PracticeEnemyFactory
    {
        private readonly PracticeSession practice;
        private readonly Transform parent;
        private readonly BasicEnemyAI[] templates;
        private readonly EnemyProjectilePool projectiles;
        public PracticeEnemyFactory(PracticeSession owner,Transform root,BasicEnemyAI[] legacy)
        {practice=owner;parent=root;templates=legacy;projectiles=EnemyProjectilePool.Ensure(root.gameObject,24);}
        public EnemyAIBase Create(int index,Vector3 position)
        {
            EnemyAIBase enemy;
            if(index<3)
            {
                if(templates==null||templates.Length!=3||templates[index]==null)
                    throw new System.InvalidOperationException("Arena is missing an authored creature template.");
                enemy=Object.Instantiate(templates[index],parent);enemy.Configure(practice.Session.Player.transform,null);
                if(index!=1)
                {
                    foreach(var originalArt in enemy.GetComponentsInChildren<SpriteRenderer>(true))originalArt.enabled=false;
                    var art=new GameObject("Criatura animada del pack");art.transform.SetParent(enemy.transform,false);
                    art.transform.localScale=Vector3.one*(index==2?1.25f:.8f)/enemy.transform.lossyScale.x;
                    var visual=art.AddComponent<SpriteRenderer>();art.AddComponent<WorldSpriteDepth>().Visual=visual;
                    enemy.ConfigureVisuals(visual,null,Color.white,index==2?"Golem":"Limo");
                    enemy.ConfigureAnimation(Resources.Load<PlayerAnimationLibrary>("SproutSlimeAnimations"));
                }
                else
                {
                    foreach(var sr in enemy.GetComponentsInChildren<SpriteRenderer>(true))
                        if(sr.sprite!=null){var depth=sr.GetComponent<WorldSpriteDepth>();if(depth==null)depth=sr.gameObject.AddComponent<WorldSpriteDepth>();depth.Visual=sr;}
                }
            }
            else
            {
                var go=new GameObject(PracticeWaveDirector.Roster[index]);go.SetActive(false);go.transform.SetParent(parent,false);
                var art=new GameObject("Enemigo del pack");art.transform.SetParent(go.transform,false);art.AddComponent<SpriteRenderer>();
                go.AddComponent<CircleCollider2D>().radius=.25f;
                var raid=go.AddComponent<RaidEnemy>();
                var style=index>=7?(EnemyCombatStyle)((int)EnemyCombatStyle.Soldier+index-7):EnemyCombatStyle.Legacy;
                var role=index==5||index==8||index==10?RaidRole.Brute:index==4?RaidRole.Archer:RaidRole.Chaser;
                raid.ConfigureRaid(practice.Session,role,projectiles,1,style);
                enemy=raid;
            }
            if(enemy.GetComponent<WorldHealthReadout>()==null)enemy.gameObject.AddComponent<WorldHealthReadout>();
            enemy.name="Arena · "+PracticeWaveDirector.Roster[index];enemy.ActivateFromPool(position);return enemy;
        }
        public void ClearProjectiles()=>projectiles.ReturnAll();
    }
}
