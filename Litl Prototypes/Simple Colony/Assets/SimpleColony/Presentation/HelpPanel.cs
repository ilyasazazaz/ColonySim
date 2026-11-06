using UnityEngine;
using UnityEngine.UI;

namespace SimpleColony
{
    public sealed class HelpPanel : MonoBehaviour
    {
        RectTransform panel, content;
        GridLayoutGroup grid;
        public void Initialize(RectTransform parent, Font font, DescriptionPopup descriptions)
        {
            transform.SetParent(parent, false);
            panel = (RectTransform)transform;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = new Vector2(88, -72);
            gameObject.AddComponent<Image>().color = new Color(.08f, .115f, .15f);
            var close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            close.transform.SetParent(transform, false);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = Vector2.one; closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-8, -8); closeRect.sizeDelta = new Vector2(48, 48);
            close.GetComponent<Image>().color = new Color(.13f, .2f, .26f);
            close.GetComponent<Button>().onClick.AddListener(() => gameObject.SetActive(false));
            Icon(close.transform, "×", Color.white, new Vector2(.5f, .5f), Vector2.zero);

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(transform, false);
            viewportObject.GetComponent<Image>().color = Color.clear;
            var viewport = (RectTransform)viewportObject.transform;
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(12, 12); viewport.offsetMax = new Vector2(-12, -64);
            var contentObject = new GameObject("Legend", typeof(RectTransform), typeof(GridLayoutGroup));
            contentObject.transform.SetParent(viewport, false); content = (RectTransform)contentObject.transform;
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            grid = contentObject.GetComponent<GridLayoutGroup>(); grid.spacing = new Vector2(8, 8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            var scroll = viewportObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;

            string[] icons = { "food", "research", "matter", "charge", "medicine", "shield", "pawn", "cargo", "ring", "road", "threat", "⌂", "pan", "zoom", "hire" };
            string[] labels = { "Питание", "Знания", "Материя", "Заряд", "Лекарства", "Защита", "Пешка", "Груз", "Остаток", "Цена пути", "Угроза", "Смена роли", "Перемещение", "Масштаб", "Найм" };
            string[] explanations = {
                "Питание пополняется на ядре и расходуется на жизнь и переходы.",
                "Исследователи добывают знания. На них покупаются улучшения колонии.",
                "Материя со склада автоматически улучшает транспорт.",
                "Заряд со склада восстанавливает запас питания на территории.",
                "Лекарства восстанавливают здоровье пешек на ядре.",
                "Стабилизаторы используют этот ресурс для защиты соседних узлов.",
                "Круг — пешка. Касание открывает выбор её профессии.",
                "Квадрат рядом с пешкой — переносимый ресурс.",
                "Цветные секторы кольца показывают остатки ресурсов территории.",
                "Цвет дороги показывает расход питания. Зелёная дешевле, красная дороже.",
                "Угрозы появляются в наиболее удалённых от ядра узлах. Защитники подавляют очаги.",
                "Назначенная смена профессии выполняется после возвращения пешки на ядро.",
                "Для перемещения карты зажмите мышь или палец и потяните.",
                "Масштаб меняется колесом мыши или разведением двух пальцев.",
                "Найм доступен на левой панели управления колонией. Цена — 25 питания."
            };
            for (int i = 0; i < icons.Length; i++)
            {
                var tile = new GameObject(labels[i], typeof(RectTransform), typeof(Image), typeof(HoldDescription)); tile.transform.SetParent(content, false);
                tile.GetComponent<Image>().color = Color.clear;
                string explanation = explanations[i];
                tile.GetComponent<HoldDescription>().Initialize(descriptions, () => explanation);
                Icon(tile.transform, icons[i], i < 6 ? Palette.Resources[i] : new Color(.8f, .9f, 1), new Vector2(.5f, 1), new Vector2(0, -24));
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text)); labelObject.transform.SetParent(tile.transform, false);
                var label = labelObject.GetComponent<Text>(); label.font = font; label.fontSize = 17; label.text = labels[i];
                label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false;
                var rect = label.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(0, 28);
            }
            Resize();
        }
        static void Icon(Transform parent, string symbol, Color color, Vector2 anchor, Vector2 offset)
        {
            var obj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiIcon)); obj.transform.SetParent(parent, false);
            var icon = obj.GetComponent<UiIcon>(); icon.Symbol = symbol; icon.color = color; icon.raycastTarget = false;
            var rect = icon.rectTransform; rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = offset; rect.sizeDelta = new Vector2(32, 32);
        }
        void OnEnable() { if (panel != null) Resize(); }
        void Update() { Resize(); }
        void Resize()
        {
            var parent = (RectTransform)panel.parent;
            bool portrait = Screen.height > Screen.width;
            panel.anchorMin = panel.anchorMax = panel.pivot = portrait ? new Vector2(.5f, 0) : new Vector2(0, 1);
            panel.anchoredPosition = portrait ? new Vector2(0, 192) : new Vector2(88, -72);
            float width = portrait ? Mathf.Min(410, parent.rect.width - 24) : Mathf.Min(410, parent.rect.width * .4f);
            float height = portrait ? 350 : Mathf.Min(500, parent.rect.height - 228);
            panel.sizeDelta = new Vector2(width, height);
            int columns = width >= 460 ? 3 : 2;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2((width - 24 - (columns - 1) * 8) / columns, 76);
            int rows = Mathf.CeilToInt(content.childCount / (float)columns);
            content.sizeDelta = new Vector2(0, rows * 84 - 8);
        }
    }
}
