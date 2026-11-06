using UnityEngine;
using UnityEngine.UI;

namespace SimpleColony
{
    public sealed class DescriptionPopup : MonoBehaviour
    {
        RectTransform panel;
        Text label;
        HoldDescription owner;
        public void Initialize(Font font)
        {
            panel = (RectTransform)transform;
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
            var background = gameObject.AddComponent<Image>(); background.color = new Color(.12f, .18f, .23f); background.raycastTarget = false;
            var obj = new GameObject("Description", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(transform, false);
            label = obj.GetComponent<Text>(); label.font = font; label.fontSize = 18; label.color = Color.white; label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(12, 12); label.rectTransform.offsetMax = new Vector2(-12, -12);
            gameObject.SetActive(false);
        }
        public void Show(HoldDescription source, string text, Vector2 screenPosition)
        {
            owner = source;
            var parent = (RectTransform)transform.parent;
            float width = Mathf.Min(360, parent.rect.width - 24);
            panel.sizeDelta = new Vector2(width, 100);
            label.text = text;
            float height = Mathf.Min(parent.rect.height - 24, Mathf.Max(64, label.preferredHeight + 24));
            panel.sizeDelta = new Vector2(width, height);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null, out Vector2 point);
            point.y += height * .5f + 32;
            panel.anchoredPosition = new Vector2(
                Mathf.Clamp(point.x, parent.rect.xMin + width * .5f + 12, parent.rect.xMax - width * .5f - 12),
                Mathf.Clamp(point.y, parent.rect.yMin + height * .5f + 12, parent.rect.yMax - height * .5f - 12));
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }
        public void Hide(HoldDescription source)
        {
            if (owner != source) return;
            gameObject.SetActive(false);
            owner = null;
        }
    }
}
