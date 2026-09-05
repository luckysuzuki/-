using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIRaycastDebugger : MonoBehaviour
{
    private readonly List<RaycastResult> results = new();

    private void Update()
    {
        // 按下鼠标左键时检测
        if (Input.GetMouseButtonDown(0))
        {
            PrintUIRaycastResults(Input.mousePosition);
        }
    }

    private void PrintUIRaycastResults(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("场景中没有 EventSystem");
            return;
        }

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        results.Clear();
        EventSystem.current.RaycastAll(pointerData, results);

        Debug.Log($"鼠标位置：{screenPosition}，命中 UI 数量：{results.Count}");

        for (int i = 0; i < results.Count; i++)
        {
            RaycastResult result = results[i];

            Debug.Log(
                $"命中顺序：{i}\n" +
                $"对象：{GetHierarchyPath(result.gameObject)}\n" +
                $"模块：{result.module}\n" +
                $"Sorting Layer：{result.sortingLayer}\n" +
                $"Sorting Order：{result.sortingOrder}\n" +
                $"Depth：{result.depth}",
                result.gameObject
            );
        }
    }

    private string GetHierarchyPath(GameObject target)
    {
        string path = target.name;
        Transform current = target.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}