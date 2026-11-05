using UnityEngine;
namespace SimpleColony
{
    public sealed class ColonyView : MonoBehaviour
    {
        World world; Texture2D disc; int selected = 14, role, speed = 1; float accumulator, tickMs; GUIStyle title, label, small, button;
        const int RoadWidth = 840, RoadHeight = 660;
        Texture2D roads;
        World roadWorld;
        uint roadVisibility;
        readonly string[] names = { "Собиратель", "Защитник", "Разведчик", "Исследователь", "Строитель", "Снабженец", "Медик", "Стабилизатор" };
        readonly string[] resources = { "Питание", "Угроза", "Разведка", "Знание", "Материя", "Заряд", "Лечение", "Стабильность" };
        readonly Color[] colors = { new Color(1, .78f, .24f), new Color(1, .32f, .37f), new Color(.4f, .87f, .51f), new Color(.37f, .58f, 1), new Color(1, .51f, .25f), new Color(.25f, .85f, .91f), new Color(.88f, .93f, 1), new Color(.75f, .48f, 1) };
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot() { if (FindAnyObjectByType<ColonyView>() == null) new GameObject("Simple Colony").AddComponent<ColonyView>(); }
        void Awake() { world = new World(4271); Application.targetFrameRate = 60; Screen.orientation = ScreenOrientation.LandscapeLeft; disc = new Texture2D(64, 64); for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) { float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)); disc.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(31.5f - d))); } disc.Apply(); if (Camera.main) { Camera.main.orthographic = true; Camera.main.backgroundColor = new Color(.035f, .055f, .08f); Camera.main.clearFlags = CameraClearFlags.SolidColor; } }
        void Update() { accumulator += Time.unscaledDeltaTime * speed; int steps = 0; while (accumulator >= .1f && steps++ < 30) { var timer = System.Diagnostics.Stopwatch.StartNew(); world.Step(); tickMs = (float)timer.Elapsed.TotalMilliseconds; accumulator -= .1f; } }
        void OnDestroy() { if (disc) Destroy(disc); if (roads) Destroy(roads); }
        void InitStyles() { if (title != null) return; title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold }; label = new GUIStyle(GUI.skin.label) { fontSize = 17 }; small = new GUIStyle(GUI.skin.label) { fontSize = 13 }; button = new GUIStyle(GUI.skin.button) { fontSize = 16 }; }
        void Box(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        void Dot(Vector2 v, float r, Color c) { GUI.color = c; GUI.DrawTexture(new Rect(v.x - r, v.y - r, r * 2, r * 2), disc); GUI.color = Color.white; }
        Vector2 Pos(int i) { return new Vector2(65 + world.Nodes[i].X * 133, 160 + world.Nodes[i].Y * 112); }
        void DrawRoads()
        {
            if (Event.current.type != EventType.Repaint) return;
            uint visibility = 0;
            for (int i = 0; i < world.Nodes.Length; i++)
                if (world.Nodes[i].Known) visibility |= 1u << i;

            // Cache a single unrotated image in the same canvas coordinates as nodes.
            // Rebuild only when the map or its discovered territories change.
            if (!roads || roadWorld != world || roadVisibility != visibility)
            {
                if (!roads)
                    roads = new Texture2D(RoadWidth, RoadHeight, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[RoadWidth * RoadHeight];
                for (int i = 0; i < world.Nodes.Length; i++)
                    foreach (int neighbor in world.Nodes[i].Links)
                        if (neighbor > i && (world.Nodes[i].Known || world.Nodes[neighbor].Known))
                            RasterRoad(pixels, Pos(i), Pos(neighbor));
                roads.SetPixels32(pixels);
                roads.Apply(false);
                roadWorld = world;
                roadVisibility = visibility;
            }
            GUI.DrawTexture(new Rect(0, 0, RoadWidth, RoadHeight), roads);
        }

        static void RasterRoad(Color32[] pixels, Vector2 a, Vector2 b)
        {
            var direction = b - a;
            float lengthSquared = direction.sqrMagnitude;
            if (lengthSquared < .001f) return;
            int left = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - 2));
            int right = Mathf.Min(RoadWidth - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + 2));
            int top = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - 2));
            int bottom = Mathf.Min(RoadHeight - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + 2));
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
            {
                var sample = new Vector2(x + .5f, y + .5f);
                float t = Mathf.Clamp01(Vector2.Dot(sample - a, direction) / lengthSquared);
                float distance = Vector2.Distance(sample, a + direction * t);
                byte alpha = (byte)(Mathf.Clamp01(1.75f - distance) * 255);
                int index = (RoadHeight - 1 - y) * RoadWidth + x;
                if (alpha > pixels[index].a) pixels[index] = new Color32(60, 92, 110, alpha);
            }
        }
        bool Btn(float x, float y, float w, string s) { return GUI.Button(new Rect(x, y, w, 38), s, button); }
        void OnGUI()
        {
            InitStyles(); float scale = Mathf.Min(Screen.width / 1200f, Screen.height / 760f); GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1200 * scale) / 2, (Screen.height - 760 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1)); Box(new Rect(0, 0, 1200, 760), new Color(.035f, .055f, .08f));
            GUI.Label(new Rect(30, 17, 500, 40), "SIMPLE COLONY  /  маленький мир", title); GUI.Label(new Rect(30, 60, 740, 25), "Исследуй связи. Наладь снабжение. Удержи границу.", label);
            if (Btn(810, 25, 100, speed == 0 ? "▶ Играть" : "Ⅱ Пауза")) speed = speed == 0 ? 1 : 0; if (Btn(920, 25, 75, "×" + speed)) speed = speed == 3 ? 1 : 3; if (Btn(1005, 25, 165, "Новая карта")) { world = new World(world.Seed + 1); selected = 14; accumulator = 0; }
            DrawRoads();
            for (int i = 0; i < 30; i++) { var n = world.Nodes[i]; if (!n.Known && !world.Frontier(i)) continue; Vector2 v = Pos(i); if (selected == i) Dot(v, 32, new Color(.55f, .73f, .83f)); if (n.Shield > 0) Dot(v, 29, colors[7]); Dot(v, 26, n.Known ? new Color(.12f, .19f, .24f) : new Color(.08f, .11f, .15f)); Dot(v, 7, i == 14 ? Color.white : n.Known ? colors[n.Kind] : Color.gray); if (n.Threat > 0 && n.Known) { Dot(v + new Vector2(17, -18), 9, colors[1]); GUI.Label(new Rect(v.x + 12, v.y - 29, 40, 25), "!", small); } GUI.Label(new Rect(v.x - 35, v.y + 26, 80, 25), i == 14 ? "ЯДРО" : n.Known ? resources[n.Kind] + " " + (int)n.Stock : "?", small); if (GUI.Button(new Rect(v.x - 28, v.y - 28, 56, 56), GUIContent.none, GUIStyle.none)) selected = i; }
            foreach (var p in world.Pawns) { if (p.Health <= 0) continue; Vector2 v = Pos(p.At); if (p.Next >= 0) v = Vector2.Lerp(v, Pos(p.Next), Mathf.Clamp01(p.Progress + accumulator / .1f * .035f * world.SpeedBonus / Vector2.Distance(new Vector2(world.Nodes[p.At].X, world.Nodes[p.At].Y), new Vector2(world.Nodes[p.Next].X, world.Nodes[p.Next].Y)))); else { int idx = world.Pawns.IndexOf(p); v += new Vector2(Mathf.Cos(idx * 2.4f), Mathf.Sin(idx * 2.4f)) * 16; } Dot(v, 5, colors[p.Role]); if (p.Cargo > 0) Box(new Rect(v.x + 5, v.y - 7, 5, 5), colors[p.Role]); }
            Box(new Rect(840, 90, 335, 635), new Color(.065f, .095f, .13f)); var node = world.Nodes[selected]; GUI.Label(new Rect(860, 105, 310, 35), selected == 14 ? "Ядро колонии" : "Территория " + selected, title); GUI.Label(new Rect(860, 145, 300, 30), node.Known ? (selected == 14 ? "Склад · отдых · профессии" : resources[node.Kind] + " / запас " + node.Stock.ToString("0")) : "Не исследована", label);
            if (selected != 14) { if (Btn(860, 180, 295, "Приоритет: " + node.Priority)) node.Priority = (node.Priority + 1) % 4; if (Btn(860, 225, 295, node.Blocked ? "Открыть доступ" : "Запретить гражданским")) node.Blocked = !node.Blocked; }
            GUI.Label(new Rect(860, 275, 300, 25), "ПРОФЕССИИ / назначение на базе", small); if (Btn(860, 305, 295, names[role] + "  →")) role = (role + 1) % 8;
            if (Btn(860, 350, 295, "Создать / 25 питания")) world.Recruit(role);
            if (Btn(860, 395, 295, "Перевести одного в профессию")) { foreach (var p in world.Pawns) if (p.Role != role && p.Health > 0 && p.PendingRole < 0) { p.PendingRole = role; break; } }
            if (Btn(860, 440, 295, "Скорость +15% / 15 знаний")) world.Upgrade();
            for (int k = 0; k < 8; k++) { GUI.color = colors[k]; GUI.Label(new Rect(860 + (k % 2) * 150, 490 + (k / 2) * 29, 150, 28), resources[k] + ": " + world.Stores[k].ToString("0"), small); GUI.color = Color.white; }
            int known = 0, alive = 0, idle = 0; foreach (var n in world.Nodes) if (n.Known) known++; foreach (var p in world.Pawns) { if (p.Health > 0) alive++; if (p.State == "Нет работы") idle++; }
            GUI.Label(new Rect(860, 620, 310, 30), "Пешки " + alive + "   Территории " + known + "/30", label); GUI.Label(new Rect(860, 653, 310, 55), "Цель: 20 территорий + технология\n" + (world.Lost ? "Колония потеряна" : known >= 20 && world.Tech > 0 ? "Цель достигнута — продолжай!" : "Развивай колонию"), small);
            GUI.Label(new Rect(30, 660, 800, 30), world.Event, label); GUI.Label(new Rect(30, 700, 800, 28), "Seed " + world.Seed + "   •   " + (int)world.Time + " с   •   тик " + tickMs.ToString("0.00") + " мс   •   без работы " + idle + "   •   пути " + world.Searches, small);
        }
    }
}

