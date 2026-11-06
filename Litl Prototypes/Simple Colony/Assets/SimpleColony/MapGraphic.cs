using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SimpleColony
{
    // All geometry shares this local coordinate space, including hit testing and gestures.
    public sealed class MapGraphic : MaskableGraphic, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        public ColonyView Owner;
        [System.NonSerialized] public World World;
        public float Zoom = 60;
        public Vector2 Pan;
        bool dragged;
        bool pinchedGesture;
        Vector2 dragOrigin, dragPan;
        public string InputStatus { get; private set; } = "Ожидание ввода";
        float pinchDistance;
        Vector2 pinchCenter;
        public float ObjectScale => Zoom / 60f;
        public Vector2 Position(Node node) => new Vector2(node.X, node.Y) * Zoom + Pan;
        public Vector2 Position(Pawn pawn)
        {
            Vector2 position = Position(World.Nodes[pawn.At]);
            if (pawn.Next >= 0) return Vector2.Lerp(position, Position(World.Nodes[pawn.Next]), pawn.Progress);
            float angle = pawn.Id * 2.4f;
            return position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (14 * ObjectScale);
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); if (World == null) return;
            foreach (var node in World.Nodes)
                foreach (int next in node.Links)
                    if (next > node.Id && (node.Known || World.Nodes[next].Known))
                    {
                        Vector2 a = Position(node), b = Position(World.Nodes[next]);
                        var bounds = Rect.MinMaxRect(Mathf.Min(a.x, b.x) - 2, Mathf.Min(a.y, b.y) - 2, Mathf.Max(a.x, b.x) + 2, Mathf.Max(a.y, b.y) + 2);
                        if (!rectTransform.rect.Overlaps(bounds)) continue;
                        float cost = World.Travel.FoodCost(node, World.Nodes[next]);
                        Color color = Color.Lerp(new Color(.2f, .7f, .52f), new Color(.95f, .3f, .22f), Mathf.Clamp01((cost - .5f) / 1.5f));
                        Line(mesh, Position(node), Position(World.Nodes[next]), Mathf.Max(.6f, 2 * ObjectScale), color * new Color(1, 1, 1, .6f));
                    }
            foreach (var node in World.Nodes)
            {
                if (!Visible(node)) continue;
                Vector2 position = Position(node);
                float extent = 36 * ObjectScale;
                if (!rectTransform.rect.Overlaps(new Rect(position - Vector2.one * extent, Vector2.one * extent * 2))) continue;
                int firstVertex = mesh.currentVertCount;
                if (Owner.SelectedPawn < 0 && Owner.SelectedNode == node.Id) Circle(mesh, position, 30, new Color(.75f, .9f, 1));
                Circle(mesh, position, 26, new Color(.17f, .21f, .25f));
                Circle(mesh, position, 21, new Color(.07f, .1f, .13f));
                if (node.Known && node.Deposits.Count > 0)
                {
                    float sector = 360f / node.Deposits.Count;
                    for (int i = 0; i < node.Deposits.Count; i++)
                    {
                        var deposit = node.Deposits[i];
                        float start = i * sector + 3, span = sector - 6;
                        Arc(mesh, position, 22, 26, start, span, new Color(.25f, .28f, .32f));
                        Arc(mesh, position, 22, 26, start, span * deposit.Amount / deposit.Capacity, Palette.Resources[(int)deposit.Type]);
                    }
                }
                if (node.Id == World.Home)
                {
                    Arc(mesh, position, 29, 31, 0, 360, new Color(.6f, .9f, 1));
                    Line(mesh, position + Vector2.left * 11, position + Vector2.up * 10, 3, Color.white);
                    Line(mesh, position + Vector2.up * 10, position + Vector2.right * 11, 3, Color.white);
                    Line(mesh, position + Vector2.down * 8, position + Vector2.up * 5, 5, Color.white);
                }
                if (!node.Known) Circle(mesh, position, 3, Color.gray);
                if (node.Blocked) Line(mesh, position + new Vector2(-10, -10), position + new Vector2(10, 10), 3, Color.gray);
                if (node.Shield > 0) Arc(mesh, position, 33, 35, 0, 360, Palette.Resources[5]);
                // Frontier threats remain visible even before surveying the territory.
                if (node.Threat > 0)
                {
                    Vector2 warning = position + new Vector2(22, 23);
                    Circle(mesh, warning, 10, new Color(.95f, .23f, .29f));
                    Line(mesh, warning + Vector2.up * 5, warning, 2, Color.white);
                    Circle(mesh, warning + Vector2.down * 4, 1.5f, Color.white);
                }
                ScaleGeometry(mesh, firstVertex, position, ObjectScale);
            }
            foreach (var pawn in World.Pawns)
            {
                if (pawn.Health <= 0) continue;
                Vector2 position = Position(pawn);
                if (!rectTransform.rect.Overlaps(new Rect(position - Vector2.one * 14 * ObjectScale, Vector2.one * 28 * ObjectScale))) continue;
                int firstVertex = mesh.currentVertCount;
                if (Owner.SelectedPawn == pawn.Id) Circle(mesh, position, 11, Color.white);
                Circle(mesh, position, 8, new Color(.025f, .035f, .05f));
                Circle(mesh, position, 6, Palette.Professions[(int)pawn.Role]);
                if (pawn.Cargo > 0) Square(mesh, position + new Vector2(9, 8), 3, Palette.Resources[(int)pawn.CargoType]);
                ScaleGeometry(mesh, firstVertex, position, ObjectScale);
            }
        }
        bool Visible(Node node) => node.Known || World.Frontier(node.Id) || node.Threat > 0;
        public void OnPointerClick(PointerEventData data)
        {
            if (dragged || pinchDistance > 0 || !Owner.MapInteractive) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out Vector2 point);
            foreach (var pawn in World.Pawns)
                if (pawn.Health > 0 && Vector2.Distance(point, Position(pawn)) < 14 * ObjectScale) { Owner.SelectPawn(pawn.Id); return; }
            foreach (var node in World.Nodes)
                if (Visible(node) && Vector2.Distance(point, Position(node)) < 30 * ObjectScale) { Owner.SelectNode(node.Id); return; }
        }
        public void OnBeginDrag(PointerEventData data)
        {
            InputStatus = "Начало перетаскивания";
            dragged = true;
            dragPan = Pan;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.pressPosition, data.pressEventCamera, out dragOrigin);
        }
        public void OnDrag(PointerEventData data)
        {
            if (pinchedGesture || !Owner.MapInteractive) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out Vector2 current);
            Pan = dragPan + current - dragOrigin;
            InputStatus = "Перетаскивание " + Pan;
            SetVerticesDirty();
        }
        public void OnEndDrag(PointerEventData data) { }
        public void OnPointerDown(PointerEventData data)
        {
            dragged = false;
            pinchedGesture = false;
            dragPan = Pan;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out dragOrigin);
            InputStatus = "Нажатие";
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (pinchedGesture || !Owner.MapInteractive) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out Vector2 current);
            if (Vector2.Distance(current, dragOrigin) < 8) return;
            Pan = dragPan + current - dragOrigin;
            dragged = true;
            InputStatus = "Карта сдвинута " + Pan;
            SetVerticesDirty();
        }
        public void OnScroll(PointerEventData data)
        {
            if (!Owner.MapInteractive) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.enterEventCamera, out Vector2 point);
            Scale(Mathf.Exp(data.scrollDelta.y * .1f), point);
        }
        public void Scale(float factor, Vector2 pivot)
        { float next = Mathf.Clamp(Zoom * factor, 18, 180); Pan = pivot - (pivot - Pan) * (next / Zoom); Zoom = next; SetVerticesDirty(); }
        void Update()
        {
            if (!Owner.MapInteractive) { pinchDistance = 0; return; }
            var screen = Touchscreen.current;
            if (screen != null && screen.touches.Count > 1 && screen.touches[0].press.isPressed && screen.touches[1].press.isPressed)
            {
                Vector2 a = screen.touches[0].position.ReadValue(), b = screen.touches[1].position.ReadValue();
                Vector2 center = (a + b) * .5f;
                if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, a) || !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, b)) { pinchDistance = 0; return; }
                float distance = Vector2.Distance(a, b);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, center, null, out Vector2 local);
                if (pinchDistance > 0)
                {
                    Pan += (center - pinchCenter) / canvas.scaleFactor;
                    Scale(distance / pinchDistance, local);
                }
                pinchDistance = Mathf.Max(1, distance); pinchCenter = center; dragged = true; pinchedGesture = true;
            }
            else pinchDistance = 0;
            SetVerticesDirty();
        }
        public static void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 delta = b - a; if (delta.sqrMagnitude < .0001f) return;
            Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
            Color clear = color; clear.a = 0;
            int first = mesh.currentVertCount;
            float inside = Mathf.Max(0, width * .5f - .5f);
            float outside = width * .5f + .5f;
            foreach (Vector2 point in new[] { a, b })
            {
                mesh.AddVert(point - normal * outside, clear, Vector2.zero);
                mesh.AddVert(point - normal * inside, color, Vector2.zero);
                mesh.AddVert(point + normal * inside, color, Vector2.zero);
                mesh.AddVert(point + normal * outside, clear, Vector2.zero);
            }
            ConnectStrip(mesh, first, first + 4, 4);
        }
        public static void Square(VertexHelper mesh, Vector2 p, float radius, Color color)
        { Quad(mesh, p + new Vector2(-radius, -radius), p + new Vector2(-radius, radius), p + new Vector2(radius, radius), p + new Vector2(radius, -radius), color); }
        static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int index = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero); mesh.AddVert(b, color, Vector2.zero); mesh.AddVert(c, color, Vector2.zero); mesh.AddVert(d, color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index, index + 2, index + 3);
        }
        public static void Circle(VertexHelper mesh, Vector2 center, float radius, Color color)
        {
            int count = Mathf.Clamp(Mathf.CeilToInt(radius * 1.2f), 12, 40);
            int start = mesh.currentVertCount;
            Color clear = color; clear.a = 0;
            mesh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= count; i++)
            {
                float angle = i * Mathf.PI * 2 / count;
                Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                mesh.AddVert(center + direction * Mathf.Max(0, radius - .6f), color, Vector2.zero);
                mesh.AddVert(center + direction * (radius + .6f), clear, Vector2.zero);
                if (i > 0)
                {
                    int current = start + 1 + i * 2;
                    mesh.AddTriangle(start, current - 2, current);
                    ConnectStrip(mesh, current - 2, current, 2);
                }
            }
        }
        public static void Arc(VertexHelper mesh, Vector2 center, float inner, float outer, float start, float span, Color color)
        {
            int count = Mathf.CeilToInt(span / 8);
            if (count <= 0) return;
            int startIndex = mesh.currentVertCount;
            Color clear = color; clear.a = 0;
            for (int i = 0; i <= count; i++)
            {
                float angle = (start + span * i / count) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                float feather = Mathf.Min(.6f, (outer - inner) * .3f);
                mesh.AddVert(center + direction * (inner - feather), clear, Vector2.zero);
                mesh.AddVert(center + direction * (inner + feather), color, Vector2.zero);
                mesh.AddVert(center + direction * (outer - feather), color, Vector2.zero);
                mesh.AddVert(center + direction * (outer + feather), clear, Vector2.zero);
                if (i == 0) continue;
                int current = startIndex + i * 4;
                ConnectStrip(mesh, current - 4, current, 4);
            }
        }
        static void ConnectStrip(VertexHelper mesh, int previous, int current, int width)
        {
            for (int i = 0; i < width - 1; i++)
            {
                mesh.AddTriangle(previous + i, previous + i + 1, current + i + 1);
                mesh.AddTriangle(previous + i, current + i + 1, current + i);
            }
        }
        static void ScaleGeometry(VertexHelper mesh, int firstVertex, Vector2 center, float scale)
        {
            var vertex = new UIVertex();
            for (int i = firstVertex; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                vertex.position = (Vector3)center + (vertex.position - (Vector3)center) * scale;
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
    public static class Palette
    {
        public static readonly Color[] Resources = { new Color(1, .78f, .24f), new Color(.4f, .6f, 1), new Color(1, .5f, .25f), new Color(.25f, .85f, .9f), new Color(.9f, .94f, 1), new Color(.75f, .5f, 1) };
        public static readonly Color[] Professions = { Resources[0], new Color(1, .3f, .37f), new Color(.4f, .88f, .5f), Resources[1], Resources[2], Resources[3], Resources[4], Resources[5], new Color(.55f, .6f, .65f) };
    }
}
