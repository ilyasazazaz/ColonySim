using UnityEngine;
using UnityEngine.UI;

namespace SimpleColony
{
    // Simplified Lucide/Feather geometry. See Assets/ThirdParty/Lucide-LICENSE.txt.
    // Mesh icons remain sharp at every canvas scale and require no font glyphs.
    public sealed class UiIcon : MaskableGraphic
    {
        public string Symbol;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            switch (Symbol)
            {
                case "pawn": MapGraphic.Circle(mesh, Point(12, 12), 7, color); break;
                case "cargo": MapGraphic.Square(mesh, Point(12, 12), 6, color); break;
                case "ring": Ring(mesh, 12, 12, 8, 0, 290); break;
                case "road": Path(mesh, 3, 18, 21, 6); break;
                case "threat": Path(mesh, 12, 3, 22, 21, 2, 21, 12, 3); Path(mesh, 12, 9, 12, 14); MapGraphic.Circle(mesh, Point(12, 18), 1, color); break;
                case "pan": Path(mesh, 3, 12, 21, 12); Path(mesh, 12, 3, 12, 21); Path(mesh, 6, 9, 3, 12, 6, 15); Path(mesh, 18, 9, 21, 12, 18, 15); break;
                case "zoom": Ring(mesh, 10, 10, 6, 0, 360); Path(mesh, 15, 15, 21, 21); Path(mesh, 7, 10, 13, 10); Path(mesh, 10, 7, 10, 13); break;
                case "hire": Ring(mesh, 9, 7, 4, 0, 360); Path(mesh, 2, 21, 2, 17, 5, 14, 12, 14); Path(mesh, 18, 13, 18, 21); Path(mesh, 14, 17, 22, 17); break;
                case "food": Path(mesh, 5, 20, 19, 4, 20, 12, 15, 18, 5, 20, 4, 12, 10, 6, 19, 4); break;
                case "research": Path(mesh, 9, 3, 15, 3); Path(mesh, 10, 3, 10, 9, 4, 19, 5, 21, 19, 21, 20, 19, 14, 9, 14, 3); Path(mesh, 7, 15, 17, 15); break;
                case "matter": Path(mesh, 3, 6, 21, 6, 21, 18, 3, 18, 3, 6); Path(mesh, 3, 12, 21, 12); Path(mesh, 12, 6, 12, 12); Path(mesh, 8, 12, 8, 18); Path(mesh, 16, 12, 16, 18); break;
                case "charge": Path(mesh, 13, 2, 4, 14, 11, 14, 10, 22, 20, 9, 13, 9, 13, 2); break;
                case "medicine": Path(mesh, 9, 3, 15, 3, 15, 9, 21, 9, 21, 15, 15, 15, 15, 21, 9, 21, 9, 15, 3, 15, 3, 9, 9, 9, 9, 3); break;
                case "shield": Path(mesh, 12, 2, 21, 6, 20, 15, 17, 19, 12, 22, 7, 19, 4, 15, 3, 6, 12, 2); break;
                case "▶": Path(mesh, 8, 5, 19, 12, 8, 19, 8, 5); break;
                case "Ⅱ": Path(mesh, 8, 5, 8, 19); Path(mesh, 16, 5, 16, 19); break;
                case "×": Path(mesh, 6, 6, 18, 18); Path(mesh, 18, 6, 6, 18); break;
                case "‹": Path(mesh, 15, 18, 9, 12, 15, 6); break;
                case "›": Path(mesh, 9, 18, 15, 12, 9, 6); break;
                case "⌂":
                    Path(mesh, 3, 10, 12, 3, 21, 10, 21, 21, 15, 21, 15, 13, 9, 13, 9, 21, 3, 21, 3, 10); break;
                case "↻":
                    Ring(mesh, 12, 12, 8, 45, 295); Path(mesh, 3, 4, 3, 10, 9, 10); break;
                case "?":
                    Ring(mesh, 12, 12, 9, 0, 360); Path(mesh, 9, 8, 10, 6.5f, 14, 6.5f, 16, 9, 15, 11, 12, 13, 12, 14);
                    MapGraphic.Circle(mesh, Point(12, 18), 1.3f, color); break;
                case "…":
                    Path(mesh, 5, 20, 5, 14); Path(mesh, 12, 20, 12, 4); Path(mesh, 19, 20, 19, 9); break;
            }
        }
        Vector2 Point(float x, float y) => rectTransform.rect.center + new Vector2(x - 12, 12 - y) * Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 24;
        void Path(VertexHelper mesh, params float[] points)
        { for (int i = 0; i + 3 < points.Length; i += 2) MapGraphic.Line(mesh, Point(points[i], points[i + 1]), Point(points[i + 2], points[i + 3]), 2, color); }
        void Ring(VertexHelper mesh, float x, float y, float radius, float start, float span)
        {
            float scale = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 24;
            MapGraphic.Arc(mesh, Point(x, y), radius * scale - 1, radius * scale + 1, start, span, color);
        }
    }
}
