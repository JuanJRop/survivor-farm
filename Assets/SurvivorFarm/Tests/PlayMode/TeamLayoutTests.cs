using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    public sealed class TeamLayoutTests
    {
        [Test]
        public void PreviewAndRuntimeShareUniqueVillageLots()
        {
            Assert.AreEqual(4, VillageLayout.Lots.Length);
            Assert.AreEqual(VillageLayout.Lots.Length, VillageLayout.Lots.Select(lot => lot.Id).Distinct().Count());
            foreach (var lot in VillageLayout.Lots)
            {
                Assert.IsTrue(VillageLayout.IsVillage(lot.Position), lot.Name);
                Assert.AreEqual(lot.Position, VillageLayout.GetLot(lot.Id).Position);
                Assert.GreaterOrEqual(lot.RestoredFacade, 1, lot.Name + " remains boarded after repair");
                Assert.Greater(VillageLayout.HouseWidth, 0);
            }
        }

        [Test]
        public void VillageRoadsPreserveCentralPlazaAndConnections()
        {
            Assert.IsTrue(VillageLayout.IsRoad(Vector2.zero));
            Assert.IsTrue(VillageLayout.IsRoad(new Vector2(-11, 0)));
            Assert.IsTrue(VillageLayout.IsRoad(new Vector2(11, 0)));
            Assert.IsTrue(VillageLayout.IsRoad(new Vector2(0, -6)));
            Assert.IsFalse(VillageLayout.IsRoad(VillageLayout.Garden));
            Assert.IsTrue(VillageLayout.IsGarden(VillageLayout.Garden));
        }

        [Test]
        public void UnknownLotIsNotSilentlyPreviewedAtOrigin()
        {
            Assert.Throws<System.ArgumentException>(() => VillageLayout.GetLot("missing"));
        }

#if UNITY_EDITOR
        [Test]
        public void FarmingPlotUsesStableIdentityAndKeepsItsLegacyAlias()
        {
            var root = new GameObject("Stable plot integration");
            try
            {
                var plot = root.AddComponent<SurvivorFarm.Runtime.Gameplay.FarmingPlot>();
                string previous = plot.PersistentId;
                var id = root.AddComponent<SurvivorFarm.Runtime.Core.StableSaveId>();
                id.AssignPreservingLegacy(previous);
                Assert.AreEqual(id.Id, plot.PersistentId);
                Assert.IsTrue(SurvivorFarm.Runtime.Core.StableSaveId.Matches(plot, previous));
                root.name = "Renamed plot";
                Assert.AreEqual(id.Id, plot.PersistentId);
            }
            finally { Object.DestroyImmediate(root); }
        }
#endif
    }
}
