using System.Collections.Generic;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    public static class DungeonLayout
    {
        public static readonly Vector3 Origin = new Vector3(0, -160, 0);
        public static readonly RectInt[] Rooms = {
            new RectInt(-6, -5, 12, 10), new RectInt(-7, 11, 14, 12),
            new RectInt(-24, 12, 12, 10), new RectInt(12, 12, 12, 10),
            new RectInt(-7, 29, 14, 10), new RectInt(-10, 45, 20, 16)
        };
        public static readonly string[] Names = { "Vestibulo", "Cuartel abandonado", "Deposito hundido", "Galeria del mineral", "Antesala", "Camara del Custodio" };
        public static readonly HashSet<Vector2Int> Floor = Build();
        public static Vector3 At(float x, float y) => Origin + new Vector3(x, y, 0);
        public static Vector3 Entry => At(0, -2);
        public static Vector3 BossCenter => At(0, 53);
        private static HashSet<Vector2Int> Build()
        {
            var cells = new HashSet<Vector2Int>();
            void Add(RectInt rect) { foreach (var p in rect.allPositionsWithin) cells.Add(p); }
            foreach (var room in Rooms) Add(room);
            Add(new RectInt(-2, 5, 4, 6)); Add(new RectInt(-12, 15, 5, 4));
            Add(new RectInt(7, 15, 5, 4)); Add(new RectInt(-2, 23, 4, 6));
            Add(new RectInt(-2, 39, 4, 6));
            Add(new RectInt(-20, 22, 4, 13)); Add(new RectInt(-16, 31, 9, 4));
            return cells;
        }
        public static bool Walkable(Vector2 position, float radius = .3f)
        {
            Vector2 p = position - (Vector2)Origin;
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
                if (!Floor.Contains(Vector2Int.FloorToInt(p + new Vector2(x, y) * radius))) return false;
            return true;
        }
        public static int RoomAt(Vector2 position)
        {
            var p = Vector2Int.FloorToInt(position - (Vector2)Origin);
            for (int i = 0; i < Rooms.Length; i++) if (Rooms[i].Contains(p)) return i;
            return -1;
        }
    }
}
