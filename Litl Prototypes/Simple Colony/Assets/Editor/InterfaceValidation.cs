using System;
using SimpleColony;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public static class InterfaceValidation
{
    [MenuItem("Simple Colony/Validate interface in Play Mode")]
    public static void Validate()
    {
        var map = UnityEngine.Object.FindAnyObjectByType<MapGraphic>();
        if (map == null || !map.Owner.MapInteractive) throw new InvalidOperationException("Start Play and close overlay panels first.");
        Vector2 savedPan = map.Pan;
        float savedZoom = map.Zoom;
        int selectedPawn = map.Owner.SelectedPawn, selectedNode = map.Owner.SelectedNode;
        var center = RectTransformUtility.WorldToScreenPoint(null, map.rectTransform.position);
        var pointer = new PointerEventData(EventSystem.current) { position = center, pressPosition = center, button = PointerEventData.InputButton.Left };
        try
        {
            ExecuteEvents.Execute(map.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(map.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.position += new Vector2(100, 60);
            ExecuteEvents.Execute(map.gameObject, pointer, ExecuteEvents.dragHandler);
            if (Vector2.Distance(savedPan, map.Pan) < 1) throw new Exception("Drag did not move map.");
            ExecuteEvents.Execute(map.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(map.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            if (map.Owner.SelectedPawn != selectedPawn || map.Owner.SelectedNode != selectedNode) throw new Exception("Drag selected an object.");
            Vector2 pivot = new Vector2(120, 30);
            Vector2 before = (pivot - map.Pan) / map.Zoom;
            map.Scale(1.2f, pivot);
            Vector2 after = (pivot - map.Pan) / map.Zoom;
            if (Vector2.Distance(before, after) > .001f) throw new Exception("Zoom moved the point under the cursor.");
            Debug.Log("COLONY INTERFACE VALIDATION PASSED");
        }
        finally
        {
            map.Pan = savedPan;
            map.Zoom = savedZoom;
            map.SetVerticesDirty();
        }
    }
}
