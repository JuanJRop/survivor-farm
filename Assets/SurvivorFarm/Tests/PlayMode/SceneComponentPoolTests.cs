using System.Collections;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class PoolTestBody : MonoBehaviour
    {
        public SceneComponentPool<PoolTestBody> Owner;
        private void OnDestroy() => Owner?.Forget(this);
    }

    public sealed class SceneComponentPoolTests
    {
        private GameObject root;
        private SceneComponentPool<PoolTestBody> pool;
        [SetUp] public void Setup() => root = new GameObject("Component pool tests");
        [TearDown] public void Teardown() { pool?.Dispose(); Object.DestroyImmediate(root); }
        private PoolTestBody Create(Transform parent)
        {
            var go = new GameObject("Reusable body"); go.SetActive(false); go.transform.SetParent(parent, false);
            var body = go.AddComponent<PoolTestBody>(); body.Owner = pool; return body;
        }

        [Test] public void WarmPoolReusesOneLeaseFiveThousandTimesWithoutCreatingObjects()
        {
            pool = new SceneComponentPool<PoolTestBody>(root.transform, Create, 4, 4, 4);
            var first = pool.Rent(); pool.Return(first);
            // Warm native bindings and the ownership hash set before measuring managed allocations.
            for (int i = 0; i < 32; i++) pool.Return(pool.Rent());
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            bool same = true;
            for (int i = 0; i < 5000; i++) { var item = pool.Rent(); same &= ReferenceEquals(first, item); pool.Return(item); }
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.IsTrue(same); Assert.AreEqual(4, pool.CreatedCount); Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(4, pool.InactiveCount);
            Assert.LessOrEqual(allocated, 256, "Hot reuse should not allocate managed objects after warmup.");
            TestContext.WriteLine("5,000 pool round trips: allocated bytes=" + allocated + "; created=" + pool.CreatedCount);
        }

        [Test] public void ActiveBudgetAndDoubleReturnDoNotGrowOrDuplicateThePool()
        {
            pool = new SceneComponentPool<PoolTestBody>(root.transform, Create, 0, 2, 2);
            var first = pool.Rent(); var second = pool.Rent();
            Assert.IsNull(pool.Rent()); Assert.AreEqual(2, pool.CreatedCount); Assert.AreEqual(1, pool.DeniedCount);
            Assert.IsTrue(pool.Return(first)); Assert.IsFalse(pool.Return(first));
            Assert.AreSame(first, pool.Rent()); Assert.AreEqual(2, pool.ActiveCount);
            pool.Return(first); pool.Return(second);
        }

        [UnityTest] public IEnumerator ValuableObjectsCanExceedIdleBudgetButReturnOnlyKeepsTheBudget()
        {
            pool = new SceneComponentPool<PoolTestBody>(root.transform, Create, 0, 2);
            var bodies = new PoolTestBody[9];
            for (int i = 0; i < bodies.Length; i++) bodies[i] = pool.Rent();
            Assert.AreEqual(9, pool.ActiveCount);
            foreach (var body in bodies) Assert.IsTrue(pool.Return(body));
            yield return null;
            Assert.AreEqual(2, pool.InactiveCount); Assert.AreEqual(0, pool.ActiveCount);
            for (int i = 2; i < bodies.Length; i++) Assert.IsTrue(bodies[i] == null);
        }

        [UnityTest] public IEnumerator DisposeDestroysLeasedObjectsEvenAfterReparentingAndCannotRentAgain()
        {
            pool = new SceneComponentPool<PoolTestBody>(root.transform, Create, 0, 2);
            var body = pool.Rent(); var region = new GameObject("Borrowed region"); region.transform.SetParent(root.transform);
            body.transform.SetParent(region.transform); body.gameObject.SetActive(true);
            pool.Dispose(); pool.Dispose();
            Assert.IsNull(pool.Rent()); Assert.AreEqual(0, pool.ActiveCount);
            yield return null; Assert.IsTrue(body == null);
        }

        [Test] public void ExternalHierarchyRemovalReleasesTheActiveBudget()
        {
            pool = new SceneComponentPool<PoolTestBody>(root.transform, Create, 0, 1, 1);
            var body = pool.Rent(); body.Owner = pool;
            body.gameObject.SetActive(true); Object.DestroyImmediate(body.gameObject);
            Assert.AreEqual(0, pool.ActiveCount); Assert.IsNotNull(pool.Rent());
        }
    }
}
