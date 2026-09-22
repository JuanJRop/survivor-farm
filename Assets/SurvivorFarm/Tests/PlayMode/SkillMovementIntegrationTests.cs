using System.Collections;
using NUnit.Framework;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class SkillMovementIntegrationTests
    {
        private GameObject player;
        [TearDown] public void Cleanup() { if (player != null) Object.DestroyImmediate(player); Time.timeScale = 1; }

        [UnityTest]
        public IEnumerator DoubleDashGrantsExactlyTwoDashesThenRequiresRecovery()
        {
            Time.timeScale = 1;
            player = new GameObject("Double dash fixture");
            player.AddComponent<Rigidbody2D>().gravityScale = 0;
            player.AddComponent<CircleCollider2D>();
            player.AddComponent<PlayerSurvivalStats>();
            player.AddComponent<PlayerInventory>();
            var movement = player.AddComponent<PlayerMovementController>();
            var skills = player.GetComponent<SkillTreeManager>();
            skills.Restore(new SkillTreeSaveData
            {
                playerLevel = 10,
                skills = new[] { new SkillLevelSaveData { id = "mobility_double_dash", level = 1 } }
            });
            Assert.IsTrue(movement.TryDash(Vector2.right));
            Assert.IsFalse(movement.TryDash(Vector2.right), "A dash cannot restart itself while active.");
            yield return new WaitForSeconds(.24f);
            Assert.AreEqual(0, movement.DashCooldown, .001f);
            Assert.IsTrue(movement.TryDash(Vector2.left), "Learned second charge must be usable before the normal cooldown.");
            yield return new WaitForSeconds(.24f);
            Assert.IsFalse(movement.TryDash(Vector2.up), "The second charge is not an unlimited dash loop.");
            Assert.Greater(movement.DashCooldown, .1f);
            movement.RefundDashCooldown(2f);
            Assert.IsTrue(movement.TryDash(Vector2.up), "Carnage can refund the recovery timer.");
        }
    }
}
