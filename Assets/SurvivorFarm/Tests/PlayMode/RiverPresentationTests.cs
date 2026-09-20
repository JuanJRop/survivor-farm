using System.Collections;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class RiverPresentationTests
    {
        [UnityTearDown] public IEnumerator Teardown()
        {
            Time.timeScale=1;
            var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("AfterRiverPresentation"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest] public IEnumerator MainHasSynchronizedWaterAndGrassOnBothBanks()
        {
            SceneManager.LoadScene("Main");
            yield return Ready();
            PortfolioSession.Instance.BeginNewGame();
            yield return VerifyRiver();
        }

        [UnityTest] public IEnumerator FarmPracticeUsesTheSameWaterAndKeepsTheBridgeCrossing()
        {
            PracticeSession.Open(PracticeMode.Farm);
            yield return Ready();
            Assert.That(PortfolioSession.Instance.Practice.Ready,Is.True);
            yield return VerifyRiver();
        }

        private static IEnumerator Ready()
        {
            for(int i=0;i<180;i++)
            {
                yield return null;
                if(PortfolioSession.Instance!=null&&PortfolioSession.Instance.IsReady)yield break;
            }
            Assert.Fail("River scene did not initialize.");
        }

        private static IEnumerator VerifyRiver()
        {
            yield return null;
            var renderers=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            var north=renderers.Where(s=>s.name=="Orilla norte de pasto").ToArray();
            var south=renderers.Where(s=>s.name=="Orilla sur de pasto").ToArray();
            var water=Object.FindObjectsByType<EnvironmentSpriteAnimation>(FindObjectsSortMode.None)
                .Where(a=>a.Visual!=null&&a.Visual.sprite!=null&&a.Visual.sprite.texture.name=="LivingWater").ToArray();
            Assert.That(north.Length,Is.GreaterThan(0));
            Assert.That(water.Length,Is.EqualTo(north.Length),"Every upper-bank tile must animate, including both river ends.");
            CollectionAssert.AreEquivalent(north.Select(s=>s.transform.position.x),water.Select(a=>a.transform.position.x));
            Assert.That(water.All(a=>a.transform.position.y>7.5f),Is.True,"Water animation must stay on the upper bank.");
            Assert.That(water.Select(a=>a.transform.position.y).Distinct().Count(),Is.EqualTo(1));
            Assert.That(water.Select(a=>a.Visual.sprite).Distinct().Count(),Is.EqualTo(1),"Upper water tiles must show the same frame.");
            Assert.That(south.Length,Is.EqualTo(north.Length));
            Assert.That(north.Concat(south).All(s=>s.sprite!=null&&s.sprite.texture.name=="TerrainAtlas"&&Mathf.Abs(s.bounds.size.x-.5f)<.001f),Is.True,"Both banks must use grass at the same 32 px/unit as the terrain.");
            Assert.That(renderers.Where(s=>s.name=="North Shore"||s.name=="South Shore").All(s=>!s.enabled),Is.True,"The old dirt banks must not remain visible below the grass.");
            Assert.That(south.All(s=>s.GetComponent<EnvironmentSpriteAnimation>()==null),Is.True);
            Assert.That(Object.FindObjectsByType<WaterRipple>(FindObjectsSortMode.None),Is.Empty);
            var first=water[0].Visual.sprite;
            var grass=south.Select(s=>s.sprite).ToArray();
            yield return new WaitForSeconds(.3f);
            Assert.That(water[0].Visual.sprite,Is.Not.SameAs(first),"The upper bank must remain animated.");
            Assert.That(water.All(a=>a.Visual.sprite==water[0].Visual.sprite),Is.True,"Upper water tiles drifted out of sync.");
            CollectionAssert.AreEqual(grass,south.Select(s=>s.sprite));

            var bridge=Object.FindFirstObjectByType<RepairableBridge>();
            Assert.That(bridge!=null&&bridge.IsRepaired,Is.True);
            var banks=Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None)
                .Where(c=>c.name=="West River Bank"||c.name=="East River Bank").ToArray();
            Assert.That(banks.Length,Is.EqualTo(2));
            Assert.That(banks.All(c=>c.enabled&&!c.isTrigger&&Mathf.Abs(c.size.y-2.9f)<.001f),Is.True);
            Physics2D.SyncTransforms();
            foreach(var bank in banks)Assert.That(bank.bounds.Contains(new Vector3(bank.bounds.center.x,7,0)),Is.True);
            Assert.That(Physics2D.OverlapCircleAll(bridge.transform.position,.35f).Any(c=>!c.isTrigger),Is.False,
                "Changing river art must leave the repaired bridge center open.");
        }
    }
}
