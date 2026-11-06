using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleColony
{
    public sealed class HoldDescription : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICancelHandler
    {
        DescriptionPopup popup;
        Func<string> description;
        PointerEventData pointer;
        Vector2 origin;
        float pressedAt, suppressUntil;
        bool pressed, shown;
        public bool SuppressClick => shown && (pressed || Time.unscaledTime <= suppressUntil);

        public void Initialize(DescriptionPopup popup, Func<string> description)
        {
            this.popup = popup;
            this.description = description;
        }
        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            pointer = data;
            origin = data.position;
            pressedAt = Time.unscaledTime;
            pressed = true;
            shown = false;
        }
        void Update()
        {
            if (!pressed) return;
            if (Vector2.Distance(origin, pointer.position) > 12) { Cancel(); return; }
            if (shown || Time.unscaledTime - pressedAt < .55f) return;
            shown = true;
            popup.Show(this, description(), origin);
        }
        public void OnPointerUp(PointerEventData data)
        {
            suppressUntil = Time.unscaledTime + .15f;
            Cancel();
        }
        public void OnPointerExit(PointerEventData data) { Cancel(); }
        public void OnCancel(BaseEventData data) { Cancel(); }
        void OnDisable() { Cancel(); }
        void Cancel()
        {
            pressed = false;
            if (popup != null) popup.Hide(this);
        }
    }

}
