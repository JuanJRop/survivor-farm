using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.World;
using UnityEditor;
using UnityEngine;

namespace SurvivorFarm.Editor
{
    public sealed class RaizclaraPreviewWindow : EditorWindow
    {
        readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        readonly List<PreviewProp> props = new List<PreviewProp>();
        static readonly string[] States = { "En ruinas", "Taller reparado", "Reconstruido" };
        [SerializeField] int stage;
        [SerializeField] float zoom = 1;
        [SerializeField] Vector2 pan;
        [SerializeField] string selectedLot;
        [SerializeField] bool showNames;
        Vector2 center;
        float units;
        Rect viewport;

        struct PreviewProp
        {
            public Texture2D Texture;
            public Rect Source;
            public Vector2 Position;
            public float Width;
            public Color Tint;
            public string LotId;
        }

        [MenuItem("Tools/Survivor Farm/Vista del pueblo")]
        public static void Open()
        {
            var window = GetWindow<RaizclaraPreviewWindow>();
            window.titleContent = new GUIContent("Raizclara");
            window.minSize = new Vector2(640, 430);
            window.Show();
        }

        void OnEnable()
        {
            EditorApplication.projectChanged += Refresh;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnDisable()
        {
            EditorApplication.projectChanged -= Refresh;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            textures.Clear();
        }

        void OnPlayModeChanged(PlayModeStateChange _) => Refresh();
        void Refresh() { textures.Clear(); Repaint(); }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                stage = GUILayout.Toolbar(stage, States, EditorStyles.toolbarButton, GUILayout.Width(300));
                GUILayout.FlexibleSpace();
                showNames = GUILayout.Toggle(showNames, "Nombres", EditorStyles.toolbarButton, GUILayout.Width(68));
                if (GUILayout.Button(new GUIContent("Centrar", "Encuadrar el pueblo completo"), EditorStyles.toolbarButton, GUILayout.Width(62)))
                { zoom = 1; pan = Vector2.zero; }
                if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), EditorStyles.toolbarButton, GUILayout.Width(28))) Refresh();
            }

            viewport = new Rect(0, 21, position.width, Mathf.Max(1, position.height - 66));
            HandleNavigation();
            GUI.BeginGroup(viewport);
            var local = new Rect(Vector2.zero, viewport.size);
            EditorGUI.DrawRect(local, new Color(.05f, .13f, .15f));
            units = Mathf.Min(viewport.width / 28f, viewport.height / 18f) * zoom;
            center = local.center + pan + Vector2.up * units * .8f;
            DrawTerrain();
            CollectProps();
            props.Sort((a, b) => b.Position.y.CompareTo(a.Position.y));
            foreach (var prop in props) DrawProp(prop);
            GUI.EndGroup();

            GUILayout.Space(viewport.height + 2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Escala", GUILayout.Width(42));
                zoom = EditorGUILayout.Slider(zoom, .7f, 2.5f, GUILayout.MaxWidth(230));
                GUILayout.FlexibleSpace();
                string caption = string.IsNullOrEmpty(selectedLot) ? "Raizclara" : VillageLayout.GetLot(selectedLot).Name;
                GUILayout.Label(caption, EditorStyles.boldLabel);
            }
            if (Event.current.type == EventType.MouseMove) Repaint();
        }

        void HandleNavigation()
        {
            var e = Event.current;
            if (!viewport.Contains(e.mousePosition)) return;
            if (e.type == EventType.ScrollWheel)
            {
                zoom = Mathf.Clamp(zoom * Mathf.Pow(1.08f, -e.delta.y), .7f, 2.5f);
                e.Use(); Repaint();
            }
            else if (e.type == EventType.MouseDrag && (e.button == 1 || e.button == 2))
            { pan += e.delta; e.Use(); Repaint(); }
            else if (e.type == EventType.MouseDown && e.button == 0 && e.clickCount == 2)
            { zoom = 1; pan = Vector2.zero; e.Use(); Repaint(); }
        }

        Texture2D Texture(string name)
        {
            if (textures.TryGetValue(name, out var found) && found != null) return found;
            found = Resources.Load<Texture2D>("StoryArt/" + name);
            textures[name] = found;
            return found;
        }

        Vector2 ScreenPoint(Vector2 world) => center + new Vector2(world.x, -world.y) * units;
        Rect WorldRect(float x, float y, float w, float h)
        {
            var topLeft = ScreenPoint(new Vector2(x, y + h));
            return new Rect(topLeft, new Vector2(w, h) * units);
        }

        void DrawTerrain()
        {
            var grass = Texture("Grass");
            var atlas = Texture("TerrainAtlas");
            for (int x = -12; x < 12; x++) for (int y = -7; y < 7; y++)
            {
                Rect cell = WorldRect(x, y, 1, 1);
                if (grass != null) GUI.DrawTexture(cell, grass, ScaleMode.StretchToFill, true);
                else EditorGUI.DrawRect(cell, new Color(.43f, .68f, .3f));
            }
            if (atlas == null) return;
            // Sample the same road predicate as runtime without constructing a scene.
            for (int x = -24; x < 24; x++) for (int y = -14; y < 14; y++)
            {
                Vector2 p = new Vector2(x * .5f + .25f, y * .5f + .25f);
                if (!VillageLayout.IsRoad(p)) continue;
                for (int qy = 0; qy < 2; qy++) for (int qx = 0; qx < 2; qx++)
                {
                    bool left = qx == 0, top = qy == 1;
                    Vector2 h = Vector2.right * (left ? -.5f : .5f);
                    Vector2 v = Vector2.up * (top ? .5f : -.5f);
                    bool edgeH = !VillageLayout.IsRoad(p + h), edgeV = !VillageLayout.IsRoad(p + v);
                    bool inner = !edgeH && !edgeV && !VillageLayout.IsRoad(p + h + v);
                    int sx = (edgeH ? left ? 64 : 112 : 88) + qx * 8;
                    int sy = (edgeV ? top ? 128 : 176 : 152) + (top ? 0 : 8);
                    if (inner) { sx = left ? 30 : 22; sy = top ? 144 : 136; }
                    DrawTexture(WorldRect(x * .5f + qx * .25f, y * .5f + qy * .25f, .25f, .25f), atlas,
                        new Rect(sx, atlas.height - sy - 8, 8, 8), Color.white);
                }
            }
        }

        void Add(string art, Vector2 at, float width, Color? tint = null)
        {
            var texture = Texture(art);
            if (texture == null) return;
            props.Add(new PreviewProp { Texture = texture, Source = new Rect(0, 0, texture.width, texture.height),
                Position = at, Width = width, Tint = tint ?? Color.white });
        }

        void AddFacade(Vector2 at, int facade, string lotId = null)
        {
            Sprite sprite = HouseSprites.Facade(facade);
            if (sprite == null) return;
            props.Add(new PreviewProp { Texture = sprite.texture, Source = sprite.rect, Position = at,
                Width = VillageLayout.HouseWidth, Tint = Color.white, LotId = lotId });
        }

        void CollectProps()
        {
            props.Clear();
            AddFacade(new Vector2(0, 1.7f), 0);
            foreach (var lot in VillageLayout.Lots)
                AddFacade(lot.Position, stage == 2 || stage == 1 && lot.Id == "nico" ? lot.RestoredFacade : lot.InitialFacade, lot.Id);
            Add("Well", VillageLayout.Well, 1);
            Add("Chest", VillageLayout.Note, .65f);
            Add("Sign", VillageLayout.CampExit, .7f);
            Add("Sign", new Vector2(-1.45f, -1.1f), .7f);
            Add("Workbench", new Vector2(-7.05f, -3.25f), .8f);
            Add("Tree", new Vector2(-8, 3.9f), 1.65f);
            Add("Tree", new Vector2(8, 3.9f), 1.65f);
            Add("Tree", new Vector2(-1.5f, -6.4f), 1.4f);
            AddResident("village:elder", new Vector2(-4.1f, 1.35f));
            AddResident("village:blacksmith", new Vector2(-4.15f, -3.3f));
            AddResident("village:farmer", new Vector2(7.25f, -3.1f));
            AddResident("village:merchant", new Vector2(5.05f, -4.2f));
            AddResident("village:guard", new Vector2(8.55f, 1.15f));
            if (stage > 0) Add("Rock", new Vector2(-7.1f, -2.5f), .45f);
            if (stage == 2)
            {
                Add("Chest", new Vector2(-7.1f, 2), .6f);
                Add("Chest", new Vector2(6.95f, -3.4f), .65f);
                for (int i = 0; i < 3; i++) Add("Crop", new Vector2(8.25f + i * .7f, -6.05f), .45f);
            }
        }

        void AddResident(string id, Vector2 anchor)
        {
            var catalog = Resources.Load<VillageNpcArtCatalog>("VillageNpcArt");
            if (catalog == null) return;
            bool restored = stage == 2 || stage == 1 && id == "village:blacksmith";
            Sprite sprite = catalog.Frame(id, false, restored, 0, 0);
            if (sprite == null) return;
            props.Add(new PreviewProp { Texture = sprite.texture, Source = sprite.rect,
                Position = anchor + (Vector2)VillageResidents.Offset(id, 12, restored), Width = 1.6f, Tint = Color.white });
        }

        void DrawProp(PreviewProp prop)
        {
            Vector2 bottom = ScreenPoint(prop.Position);
            float width = prop.Width * units, height = width * prop.Source.height / prop.Source.width;
            Rect rect = new Rect(bottom.x - width * .5f, bottom.y - height, width, height);
            DrawTexture(rect, prop.Texture, prop.Source, prop.Tint);
            if (string.IsNullOrEmpty(prop.LotId)) return;
            bool hover = rect.Contains(Event.current.mousePosition);
            if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            { selectedLot = prop.LotId; Event.current.Use(); Repaint(); }
            if (selectedLot == prop.LotId)
            {
                var c = new Color(.98f, .83f, .35f);
                EditorGUI.DrawRect(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, 2), c);
                EditorGUI.DrawRect(new Rect(rect.x - 2, rect.yMax, rect.width + 4, 2), c);
                EditorGUI.DrawRect(new Rect(rect.x - 2, rect.y, 2, rect.height), c);
                EditorGUI.DrawRect(new Rect(rect.xMax, rect.y, 2, rect.height), c);
            }
            if (hover || showNames)
            {
                var label = new GUIContent(VillageLayout.GetLot(prop.LotId).Name);
                Vector2 size = EditorStyles.helpBox.CalcSize(label);
                GUI.Label(new Rect(rect.center.x - size.x * .5f, rect.yMax + 3, size.x, size.y), label, EditorStyles.helpBox);
            }
        }

        static void DrawTexture(Rect target, Texture2D texture, Rect source, Color tint)
        {
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(target, texture,
                new Rect(source.x / texture.width, source.y / texture.height, source.width / texture.width, source.height / texture.height), true);
            GUI.color = previous;
        }
    }
}
