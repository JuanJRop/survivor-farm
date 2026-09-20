using System;
using System.Collections;
using System.IO;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Runtime.Core
{
    /// <summary>Opt-in executable stress check. Measures warmed reuse, not an FPS benchmark.</summary>
    public sealed class OptimizationSmokeCheck : MonoBehaviour
    {
        [Serializable] private sealed class Results
        {
            public int cycles, arrowsCreated, arrowLeasesRemaining, lootCreatedBefore, lootCreatedAfter;
            public int impactsCreatedBefore, impactsCreatedAfter, navigationRefreshes, navigationOverlapQueries, navigationBoundaryQueries, routeBuffers;
            public int labelsCreatedBefore, labelsCreatedAfter;
            public long arrowManagedBytes, lootManagedBytes, impactManagedBytes, labelManagedBytes, navigationManagedBytesAfterWarmup;
            public bool poolsReleasedOnSceneUnload, attachedSwordRemoved;
        }
        private string output;
        private bool finished, errors;
        private float started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (GameSaveSystem.IsQa && Array.IndexOf(Environment.GetCommandLineArgs(), "--optimization-smoke") >= 0 &&
                FindFirstObjectByType<OptimizationSmokeCheck>() == null)
                new GameObject("Optimization executable QA").AddComponent<OptimizationSmokeCheck>();
        }
        private void Awake()
        {
            started = Time.realtimeSinceStartup; Application.runInBackground = true;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../QA/Optimization"));
            Directory.CreateDirectory(output); Application.logMessageReceived += OnLog;
        }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;
            errors = true; File.AppendAllText(Path.Combine(output, "errors.txt"), message + "\n" + stack + "\n");
        }
        private void Update() { if (!finished && Time.realtimeSinceStartup - started > 120) Finish(false, "Timeout"); }
        private IEnumerator Start()
        {
            var run = Run();
            while (true)
            {
                bool next;
                try { next = run.MoveNext(); }
                catch (Exception ex) { Finish(false, ex.ToString()); yield break; }
                if (!next) break;
                yield return run.Current;
            }
            Finish(!errors, "Warmed 5,000-cycle arrow, loot, impact and floating label reuse; bounded route cache; scene cleanup and no attached sword. This is an allocation/reuse stress check, not an FPS claim.");
        }
        private IEnumerator Run()
        {
            while (PortfolioSession.Instance == null || !PortfolioSession.Instance.IsReady) yield return null;
            var session = PortfolioSession.Instance; session.BeginNewGame(); yield return null;
            var player = session.Player;
            var results = new Results { cycles = 5000, attachedSwordRemoved = player.transform.Find("Espada equipada") == null };
            Require(results.attachedSwordRemoved, "Attached sword remains.");
            int lootSceneCount = LootPickupPool.ScenePoolCount;
            Scene sandbox = SceneManager.CreateScene("Optimization QA scratch scene");
            var region = new GameObject("Repeated objects"); SceneManager.MoveGameObjectToScene(region, sandbox);
            region.transform.position = new Vector3(200, 200);
            var arrows = PlayerProjectilePool.Create(region.transform);
            var lootService = LootPickupPool.For(region.transform);
            var impacts = CombatImpactPool.For(region.transform);
            var labels = FloatingFeedbackPool.For(region.transform);
            for (int i = 0; i < 32; i++)
            {
                CycleArrow(arrows, player); CycleLoot(region.transform); CycleImpact(impacts, region.transform); CycleLabel(labels, region.transform.position);
            }
            results.lootCreatedBefore = lootService.CreatedCount; results.impactsCreatedBefore = impacts.CreatedCount;
            results.labelsCreatedBefore = labels.CreatedCount;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < results.cycles; i++) CycleArrow(arrows, player);
            results.arrowManagedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < results.cycles; i++) CycleLoot(region.transform);
            results.lootManagedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < results.cycles; i++) CycleImpact(impacts, region.transform);
            results.impactManagedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < results.cycles; i++) CycleLabel(labels, region.transform.position);
            results.labelManagedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            results.labelsCreatedAfter = labels.CreatedCount;
            results.arrowsCreated = arrows.CreatedCount; results.arrowLeasesRemaining = arrows.ActiveCount;
            results.lootCreatedAfter = lootService.CreatedCount; results.impactsCreatedAfter = impacts.CreatedCount;
            Require(results.arrowsCreated == 4 && results.arrowLeasesRemaining == 0, "Arrow pool grew during warmed stress.");
            Require(results.lootCreatedBefore == results.lootCreatedAfter && lootService.ActiveCount == 0, "Loot pool grew or kept active leases.");
            Require(results.impactsCreatedBefore == results.impactsCreatedAfter && impacts.ActiveCount == 0, "Impact pool grew or kept active leases.");
            Require(results.labelsCreatedBefore == results.labelsCreatedAfter && labels.ActiveCount == 0, "Floating label pool grew or kept active leases.");
            var navigation = new FarmRaidNavigation();
            for (int pass = 0; pass < 3; pass++)
            {
                long navigationBefore = GC.GetAllocatedBytesForCurrentThread();
                navigation.Invalidate();
                for (int i = 0; i < 48; i++) navigation.HasRoute(new Vector2(35, -20), new Vector2(-35 + i * .5f, -20));
                if (pass > 0) results.navigationManagedBytesAfterWarmup += GC.GetAllocatedBytesForCurrentThread() - navigationBefore;
            }
            results.navigationRefreshes = navigation.MapRefreshCount;
            results.navigationOverlapQueries = navigation.MapOverlapQueryCount;
            results.navigationBoundaryQueries = navigation.BoundaryOverlapQueryCount;
            results.routeBuffers = navigation.FieldBufferCount;
            Require(results.navigationRefreshes == 3 && results.navigationOverlapQueries == 3 && results.routeBuffers <= 24, "Navigation budgets exceeded.");
            yield return SceneManager.UnloadSceneAsync(sandbox);
            yield return null;
            results.poolsReleasedOnSceneUnload = arrows == null && impacts == null && labels == null && lootService == null && LootPickupPool.ScenePoolCount == lootSceneCount;
            Require(results.poolsReleasedOnSceneUnload, "Pool ownership survived scene unload.");
            File.WriteAllText(Path.Combine(output, "metrics.json"), JsonUtility.ToJson(results, true));
        }
        private static void CycleArrow(PlayerProjectilePool pool, Player.PlayerInventory player)
        {
            var arrow = pool.Rent(); Require(arrow != null, "Arrow lease missing.");
            arrow.ConfigureDirection(Vector2.right, 8, player);
            arrow.gameObject.SetActive(true); arrow.ReturnToPool();
        }
        private static void CycleLoot(Transform region)
        {
            var drop = EnemyLootPickup.Spawn(null, region.position, region, ResourceFlyweights.Item(ItemKind.Coins), 3);
            Require(drop != null, "Loot lease missing."); drop.Discard();
        }
        private static void CycleImpact(CombatImpactPool pool, Transform region)
        {
            var burst = pool.Rent(); Require(burst != null, "Impact lease missing.");
            burst.transform.SetParent(region, false);
            burst.Play(CombatFxLibrary.Impact, .28f, 1.05f, Color.white); burst.ReturnToPool();
        }
        private static void CycleLabel(FloatingFeedbackPool pool, Vector3 position)
        {
            var label = pool.Rent(); Require(label != null, "Floating label lease missing.");
            label.Play("−8", position, true); label.ReturnToPool();
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private void Finish(bool success, string message)
        {
            if (finished) return; finished = true;
            File.WriteAllText(Path.Combine(output, "result.txt"), (success ? "PASS" : "FAIL") + "\n" + message);
            Application.logMessageReceived -= OnLog; Application.Quit(success ? 0 : 1);
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
    }
}
