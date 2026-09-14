using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Runtime.World
{
    public static class VillageLayout
    {
        public readonly struct Lot
        {
            public readonly string Id, Name;
            public readonly Vector3 Position;
            public readonly int InitialFacade, RestoredFacade;

            public Lot(string id, string name, Vector3 position, int initialFacade, int restoredFacade)
            {
                Id = id; Name = name; Position = position;
                InitialFacade = initialFacade; RestoredFacade = restoredFacade;
            }
        }

        public const float HouseWidth = 2.2f;
        public static readonly Rect Bounds = new Rect(-12, -7, 24, 14);
        public static readonly Vector3 Well = new Vector3(-1.1f, .25f);
        public static readonly Vector3 Note = new Vector3(1.7f, 1.65f);
        public static readonly Vector3 CampExit = new Vector3(9.8f, 1.1f);
        public static readonly Vector3 Garden = new Vector3(9.1f, -4.2f);
        public static readonly Lot[] Lots = {
            new Lot("mara", "Casa de Mara", new Vector3(-5.6f, 1.7f), 1, 1),
            new Lot("dalia", "Casa del molinero", new Vector3(5.6f, 1.7f), 0, 1),
            new Lot("nico", "Taller de Nico", new Vector3(-5.6f, -3.4f), 0, 1),
            new Lot("rolo", "Mercado de Rolo", new Vector3(5.6f, -3.4f), 1, 1)
        };

        public static Lot GetLot(string id)
        {
            foreach (var lot in Lots) if (lot.Id == id) return lot;
            throw new System.ArgumentException("Unknown village lot: " + id, nameof(id));
        }

        static readonly Rect[] roads = {
            new Rect(-12, -.75f, 24, 1.5f),
            new Rect(-2, -1.5f, 4, 3),
            new Rect(-.25f, 1, 1.5f, 1),
            new Rect(-5.5f, .5f, 1.25f, 1.5f),
            new Rect(5.5f, .5f, 1.25f, 1.5f),
            new Rect(-3.25f, -4.5f, 1.25f, 4.5f),
            new Rect(2.25f, -4.5f, 1.25f, 4.5f),
            new Rect(-7.75f, -4.75f, 15.25f, 1.25f),
            new Rect(-.75f, -7, 1.5f, 3),
            new Rect(-.75f, 4.75f, 1.5f, 2.25f),
            new Rect(2.25f, .5f, 1.25f, 5.25f),
            new Rect(-.75f, 5, 4.25f, 1)
        };

        public static bool IsRoad(Vector2 p) => roads.Any(r => r.Contains(p));
        public static bool IsVillage(Vector2 p) => Bounds.Contains(p);
        public static bool IsGarden(Vector2 p) => p.y < -1.8f && p.y > -6.5f && Mathf.Abs(p.x) > 7.5f && Mathf.Abs(p.x) < 11.5f;

        public static void ArrangeResources()
        {
            var spawns = Object.FindObjectsByType<ResourceSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(s => IsVillage(s.transform.position)).OrderBy(s => s.PersistentId).ToArray();
            Physics2D.SyncTransforms();
            foreach (var spawn in spawns)
            {
                Vector2 p = spawn.transform.position;
                if (Mathf.Abs(p.x) > 8.5f && Mathf.Abs(p.y) > 1.7f) continue;
                bool moved = false;
                for (int row = 0; row < 6 && !moved; row++) for (int col = 0; col < 2 && !moved; col++)
                {
                    var target = new Vector3(-11 + col * 1.75f, -5.8f + row * 2.1f);
                    if (IsRoad(target) || spawns.Any(s => s != spawn && Vector2.Distance(s.transform.position, target) < 1.45f)) continue;
                    if (Physics2D.OverlapCircleAll(target, .55f).Any(c => !c.isTrigger && !c.transform.IsChildOf(spawn.transform))) continue;
                    spawn.transform.position = target;
                    var animal = spawn.GetComponentInChildren<AnimalRoamingVisual>();
                    if (animal != null) animal.ResetHome();
                    moved = true;
                }
            }
            Physics2D.SyncTransforms();
            int animalIndex = 0;
            foreach (var animal in Object.FindObjectsByType<AnimalRoamingVisual>(FindObjectsSortMode.None).OrderBy(a => a.name))
            {
                Vector2 p = animal.transform.position;
                if (!IsVillage(p) || Mathf.Abs(p.x) > 7 || Mathf.Abs(p.y) > 2) continue;
                animal.transform.position = new Vector3(-8.2f + animalIndex % 2 * .6f, -5.9f + animalIndex / 2 * .6f);
                animal.Radius = .6f;
                animal.ResetHome();
                animalIndex++;
            }
        }

        public static void ConnectPaths()
        {
            var paths = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Farm Paths");
            if (paths == null) return;
            TileBase dirt = null;
            foreach (var cell in paths.cellBounds.allPositionsWithin)
                if (paths.HasTile(cell)) { dirt = paths.GetTile(cell); break; }
            if (dirt == null) return;
            var min = paths.WorldToCell(Bounds.min);
            var max = paths.WorldToCell(Bounds.max);
            for (int x = min.x; x <= max.x; x++) for (int y = min.y; y <= max.y; y++)
            {
                var cell = new Vector3Int(x, y);
                Vector2 p = paths.GetCellCenterWorld(cell);
                if (IsVillage(p)) paths.SetTile(cell, IsRoad(p) ? dirt : null);
            }
        }
    }
}
