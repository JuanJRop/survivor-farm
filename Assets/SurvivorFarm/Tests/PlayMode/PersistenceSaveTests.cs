using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    public sealed class PersistenceSaveTests
    {
        private const string Fixture = "{\"version\":20,\"playerPosition\":{\"x\":3,\"y\":4,\"z\":0},\"inventory\":{\"seeds\":9,\"wood\":12,\"stone\":6,\"coins\":17},\"survival\":{\"maxHealth\":5,\"health\":5,\"hungerPercent\":0.75},\"day\":1,\"hour\":8,\"toolUpgrades\":{},\"crafting\":{},\"tutorialQuest\":{},\"plots\":[],\"resources\":[],\"resourceSpawns\":[],\"unlockZones\":[],\"dungeonChests\":[],\"groundLoot\":[],\"house\":null,\"adventure\":null,\"valley\":null,\"buildings\":null}";
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "SurvivorFarm-PersistenceTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (string file in Directory.GetFiles(directory)) File.Delete(file);
            Directory.Delete(directory);
        }

        [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)]
        [TestCase(9)] [TestCase(10)] [TestCase(11)] [TestCase(12)]
        [TestCase(13)] [TestCase(14)] [TestCase(15)] [TestCase(16)]
        [TestCase(17)] [TestCase(18)] [TestCase(19)] [TestCase(20)] [TestCase(21)] [TestCase(22)]
        public void SupportedVersionsPreserveLegacySeedsAndOptionalDefaults(int version)
        {
            string json = Fixture.Replace("\"version\":20", "\"version\":" + version);
            if (version < 20) json = json.Replace(",\"day\":1,\"hour\":8", "").Replace(",\"plots\":[]", "").Replace(",\"resourceSpawns\":[]", "");
            object data = Read(json);
            Assert.That(Field(data, "version"), Is.EqualTo(version));
            Assert.That(Field(Field(data, "inventory"), "seeds"), Is.EqualTo(9));
            Assert.That(Field(Field(data, "inventory"), "coins"), Is.EqualTo(17));
            Assert.That(Field(data, "day"), Is.EqualTo(1));
            Assert.That(Field(data, "hour"), Is.EqualTo(8f));
            Assert.That(Field(data, "plots"), Is.Not.Null);
            Assert.That(Field(data, "resourceSpawns"), Is.Not.Null);
            Assert.That(Field(data, "groundLoot"), Is.Not.Null);
            Assert.That(Field(data, "buildings"), Is.Not.Null);
        }

        [TestCase(4)] [TestCase(23)] [TestCase(0)]
        public void UnsupportedVersionsBlockAnIsolatedSlot(int version)
        {
            string path = Path.Combine(directory, "slot.json");
            string json = Fixture.Replace("\"version\":20", "\"version\":" + version);
            File.WriteAllText(path, json);
            var store = new SaveFileStore(path, GameSaveSystem.ValidateSaveJson);
            Assert.That(store.TryLoad(out _), Is.EqualTo(SaveLoadSource.Failed));
            Assert.That(store.TryWrite(Fixture), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(json));
        }

        [Test]
        public void LegacyDropsRemainCoinsAndNewDropsPreserveTheirItemAndEncounter()
        {
            string legacy = Fixture.Replace("\"groundLoot\":[]", "\"groundLoot\":[{\"amount\":3,\"position\":{\"x\":1,\"y\":2,\"z\":0}}]");
            var drops = (System.Collections.IList)Field(Read(legacy), "groundLoot");
            Assert.AreEqual((int)ItemKind.Coins, Field(drops[0], "item"));
            string current = legacy.Replace("\"version\":20", "\"version\":22")
                .Replace("\"amount\":3", "\"amount\":3,\"item\":" + (int)ItemKind.Ruby + ",\"encounterId\":\"demo-crypt\"");
            drops = (System.Collections.IList)Field(Read(current), "groundLoot");
            Assert.AreEqual((int)ItemKind.Ruby, Field(drops[0], "item"));
            Assert.AreEqual("demo-crypt", Field(drops[0], "encounterId"));
        }

        [TestCase("\"coins\":17", "\"coins\":-1")]
        [TestCase("\"coins\":17", "\"coins\":2147483648")]
        [TestCase("\"hungerPercent\":0.75", "\"hungerPercent\":2")]
        [TestCase("\"x\":3", "\"x\":1e999")]
        [TestCase("\"x\":3", "\"x\":null")]
        [TestCase("\"health\":5", "\"health\":6")]
        [TestCase("\"version\":20", "\"version\":20,\"selectedTool\":100")]
        [TestCase("\"plots\":[]", "\"plots\":[null]")]
        [TestCase("\"buildings\":null", "\"buildings\":[null]")]
        [TestCase("\"groundLoot\":[]", "\"groundLoot\":[null]")]
        [TestCase("\"resourceSpawns\":[]", "\"resourceSpawns\":[{\"id\":\"x\"},{\"id\":\"x\"}]")]
        [TestCase("\"plots\":[]", "\"plots\":[{\"id\":\"x\",\"state\":100}]")]
        public void InvalidValuesAreRejectedBeforeRestore(string original, string replacement)
        {
            Assert.That(GameSaveSystem.ValidateSaveJson(Fixture.Replace(original, replacement), out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [TestCase("{}")] [TestCase("{\"version\":20}")] [TestCase("null")] [TestCase("[]")]
        public void EmptyOrIncompleteRecordsAreNotNewGames(string json)
        {
            Assert.That(GameSaveSystem.ValidateSaveJson(json, out _), Is.False);
        }

        [Test]
        public void MissingCurrentProgressSectionCannotClearProgress()
        {
            Assert.That(GameSaveSystem.ValidateSaveJson(Fixture.Replace(",\"plots\":[]", ""), out _), Is.False);
            Assert.That(GameSaveSystem.ValidateSaveJson(Fixture.Replace(",\"valley\":null", ""), out _), Is.False);
        }

        [Test]
        public void DeathAndNullOptionalLegacyCollectionsRemainLoadable()
        {
            string json = Fixture.Replace("\"health\":5", "\"health\":0").Replace("\"version\":20", "\"version\":5,\"movementMode\":2")
                .Replace("\"plots\":[]", "\"plots\":null").Replace("\"toolUpgrades\":{}", "\"toolUpgrades\":null").Replace("\"crafting\":{}", "\"crafting\":null");
            object data = Read(json);
            Assert.That(Field(Field(data, "survival"), "health"), Is.EqualTo(0));
            Assert.That(Field(data, "movementMode"), Is.EqualTo(0));
            Assert.That(Field(data, "plots"), Is.Not.Null);
            Assert.That(Field(data, "crafting"), Is.Not.Null);
        }

        [Test]
        public void RecoveryDoesNotReplaceHealthyBackupWithDamagedPrimary()
        {
            string path = Path.Combine(directory, "slot.json");
            var store = new SaveFileStore(path, GameSaveSystem.ValidateSaveJson);
            Assert.That(store.TryWrite(Fixture), Is.True, store.LastError);
            string next = Fixture.Replace("\"coins\":17", "\"coins\":18");
            Assert.That(store.TryWrite(next), Is.True, store.LastError);
            File.WriteAllText(path, "{truncated");
            store = new SaveFileStore(path, GameSaveSystem.ValidateSaveJson);
            Assert.That(store.TryLoad(out string recovered), Is.EqualTo(SaveLoadSource.Backup));
            Assert.That(recovered, Is.EqualTo(Fixture));
            Assert.That(store.TryWrite(next), Is.True, store.LastError);
            Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(Fixture));
            Assert.That(Directory.GetFiles(directory, "slot.json.rejected-*").Length, Is.EqualTo(1));
        }

        [Test]
        public void TotalFailureDoesNotOverwriteAnyCandidate()
        {
            string path = Path.Combine(directory, "slot.json");
            foreach (string suffix in new[] { "", ".bak", ".tmp" }) File.WriteAllText(path + suffix, "broken" + suffix);
            var store = new SaveFileStore(path, GameSaveSystem.ValidateSaveJson);
            Assert.That(store.TryLoad(out _), Is.EqualTo(SaveLoadSource.Failed));
            Assert.That(store.IsWriteBlocked, Is.True);
            Assert.That(store.TryWrite(Fixture), Is.False);
            foreach (string suffix in new[] { "", ".bak", ".tmp" }) Assert.That(File.ReadAllText(path + suffix), Is.EqualTo("broken" + suffix));
        }

        [Test]
        public void GameSnapshotWithAbsentOptionalComponentsPassesItsOwnValidator()
        {
            var player = new GameObject("Persistence snapshot player");
            try
            {
                PlayerInventory inventory = player.AddComponent<PlayerInventory>();
                GameSaveSystem save = player.AddComponent<GameSaveSystem>();
                save.Configure(player.transform, inventory, null, null, null, null, null, null, null, null);
                object snapshot = typeof(GameSaveSystem).GetMethod("BuildSaveData", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(save, null);
                Assert.That(GameSaveSystem.ValidateSaveJson(JsonUtility.ToJson(snapshot, true), out string error), Is.True, error);
            }
            finally { UnityEngine.Object.DestroyImmediate(player); }
        }

        [Test]
        public void StableProviderKeepsAliasesAfterRenameReorderAndSerialization()
        {
            var root = new GameObject("Persistence root");
            var first = new GameObject("Original");
            var second = new GameObject("Sibling");
            first.transform.SetParent(root.transform);
            second.transform.SetParent(root.transform);
            try
            {
                const string legacy = "/0/0Original";
                Assert.That(StableSaveId.Resolve(first.transform, legacy), Is.EqualTo(legacy));
                StableSaveId stable = first.AddComponent<StableSaveId>();
                // Populate serialized fields just as the explicit editor tool does.
                JsonUtility.FromJsonOverwrite("{\"id\":\"stable:test\",\"legacyIds\":[\"/0/0Original\"],\"legacyPlotIndex\":7}", stable);
                first.name = "Renamed";
                first.transform.SetAsLastSibling();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(stable), stable);
                Assert.That(StableSaveId.Resolve(first.transform, "/0/1Renamed"), Is.EqualTo("stable:test"));
                Assert.That(StableSaveId.Matches(first.transform, legacy), Is.True);
                Assert.That(StableSaveId.Matches(first.transform, "stable:test"), Is.True);
                Assert.That(StableSaveId.Matches(first.transform, "wrong"), Is.False);
                Assert.That(stable.LegacyPlotIndex, Is.EqualTo(7));
                string[] copy = stable.LegacyIds;
                copy[0] = "modified";
                Assert.That(StableSaveId.Matches(first.transform, legacy), Is.True);

                ResourceSpawnPoint spawn = first.AddComponent<ResourceSpawnPoint>();
                spawn.Configure(null, null, legacy);
                Assert.That(spawn.PersistentId, Is.EqualTo("stable:test"));
                Assert.That(spawn.MatchesPersistentId(legacy), Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static object Read(string json)
        {
            var arguments = new object[] { json, null, null };
            MethodInfo method = typeof(GameSaveSystem).GetMethod("TryReadSaveJson", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method.Invoke(null, arguments), Is.True, arguments[2] as string);
            return arguments[1];
        }

        private static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
    }
}
