using System.Collections;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class FloatingFeedbackPoolTests
    {
        private GameObject root;
        private GameFeelFeedback feedback;
        private Scene fixtureScene;
        [SetUp] public void SetUp()
        {
            Time.timeScale=1;GameFeelFeedback.Enabled=true;
            // Cosmetic pools intentionally survive their callers. Give each test a fresh scene
            // so earlier combat fixtures cannot contribute to this scene's creation counters.
            fixtureScene=SceneManager.CreateScene("Floating feedback fixture");
            root=new GameObject("Floating feedback fixture");
            SceneManager.MoveGameObjectToScene(root,fixtureScene);
            feedback=root.AddComponent<GameFeelFeedback>();
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            Time.timeScale=1;GameFeelFeedback.Enabled=true;
            if(fixtureScene.IsValid()&&fixtureScene.isLoaded)yield return SceneManager.UnloadSceneAsync(fixtureScene);
        }

        [Test] public void RepeatedDamageLabelsReuseTheirTextMeshAndResetTextColorAndPosition()
        {
            feedback.Pulse("-8",Vector3.zero,true,playAudio:false);
            var pool=FloatingFeedbackPool.For(root.transform);
            var label=pool.GetComponentInChildren<FloatingFeedback>();
            var text=label.GetComponent<TextMesh>();
            Assert.AreEqual((Color32)new Color(1,.5f,.4f),(Color32)text.color);
            for(int i=0;i<64;i++)
            {
                label.ReturnToPool();label.ReturnToPool();Assert.AreEqual(string.Empty,text.text);
                feedback.Pulse("+2",Vector3.right*3,playAudio:false);
                Assert.AreSame(label,pool.GetComponentInChildren<FloatingFeedback>());
                Assert.AreSame(text,label.GetComponent<TextMesh>());
                Assert.AreEqual("+2",text.text);Assert.AreEqual((Color32)new Color(1,.95f,.6f),(Color32)text.color);
                Assert.AreEqual(new Vector3(3,.65f,0),label.transform.position);
            }
            Assert.AreEqual(4,pool.CreatedCount);Assert.AreEqual(1,pool.ActiveCount);
        }

        [UnityTest] public IEnumerator LabelsKeepScaledLifetimeAndCanBeReusedAfterExpiration()
        {
            feedback.Pulse("-8",Vector3.zero,true,playAudio:false);
            var pool=FloatingFeedbackPool.For(root.transform);
            var label=pool.GetComponentInChildren<FloatingFeedback>();
            yield return new WaitForSeconds(.2f);
            Assert.Greater(label.transform.position.y,.65f);
            Vector3 pausedPosition=label.transform.position;Time.timeScale=0;
            yield return new WaitForSecondsRealtime(.8f);
            Assert.AreEqual(pausedPosition,label.transform.position);Assert.AreEqual(1,pool.ActiveCount);
            Time.timeScale=1;yield return new WaitForSeconds(.6f);
            Assert.AreEqual(0,pool.ActiveCount);Assert.IsFalse(label.gameObject.activeSelf);
            feedback.Pulse("-4",Vector3.up,true,playAudio:false);
            Assert.AreSame(label,pool.GetComponentInChildren<FloatingFeedback>());
            yield return new WaitForSeconds(.25f);Assert.AreEqual(1,pool.ActiveCount);
            yield return new WaitForSeconds(.6f);Assert.AreEqual(0,pool.ActiveCount);
            Assert.AreEqual(4,pool.CreatedCount);
        }

        [Test] public void LabelBudgetCapsCosmeticObjectsAndReopensWhenOneReturns()
        {
            for(int i=0;i<80;i++)feedback.Pulse("-1",Vector3.zero,true,playAudio:false);
            var pool=FloatingFeedbackPool.For(root.transform);
            Assert.AreEqual(FloatingFeedbackPool.MaximumLabels,pool.ActiveCount);
            Assert.AreEqual(FloatingFeedbackPool.MaximumLabels,pool.CreatedCount);
            pool.GetComponentInChildren<FloatingFeedback>().ReturnToPool();
            feedback.Pulse("+1",Vector3.zero,playAudio:false);
            Assert.AreEqual(FloatingFeedbackPool.MaximumLabels,pool.ActiveCount);
            Assert.AreEqual(FloatingFeedbackPool.MaximumLabels,pool.CreatedCount);
            GameFeelFeedback.Enabled=false;feedback.Pulse("ignored",Vector3.zero,playAudio:false);
            Assert.AreEqual(FloatingFeedbackPool.MaximumLabels,pool.ActiveCount);
        }

        [UnityTest] public IEnumerator DisablingOrDestroyingLabelsDoesNotLeaveActiveLeases()
        {
            feedback.Pulse("-1",Vector3.zero,true,playAudio:false);
            var pool=FloatingFeedbackPool.For(root.transform);
            var label=pool.GetComponentInChildren<FloatingFeedback>();
            label.gameObject.SetActive(false);yield return null;
            Assert.AreEqual(0,pool.ActiveCount);
            feedback.Pulse("-2",Vector3.zero,true,playAudio:false);
            Object.Destroy(label.gameObject);yield return null;
            Assert.AreEqual(0,pool.ActiveCount);
            feedback.Pulse("-3",Vector3.zero,true,playAudio:false);
            Assert.AreEqual(1,pool.ActiveCount);Assert.AreEqual(4,pool.CreatedCount);
        }

        [UnityTest] public IEnumerator SceneUnloadCleansUpActiveLabelsIdleLabelsAndTheRegistry()
        {
            var scene=SceneManager.CreateScene("Floating feedback teardown");
            var actor=new GameObject("Temporary source");SceneManager.MoveGameObjectToScene(actor,scene);
            actor.AddComponent<GameFeelFeedback>().Pulse("-1",Vector3.zero,true,playAudio:false);
            var pool=FloatingFeedbackPool.For(actor.transform);
            Assert.AreEqual(1,pool.ActiveCount);Assert.AreEqual(3,pool.InactiveCount);
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.IsTrue(pool==null);
            var current=FloatingFeedbackPool.For(root.transform);Assert.NotNull(current);
            Assert.AreEqual(0,current.ActiveCount);Assert.AreEqual(4,current.InactiveCount);
        }
    }
}
