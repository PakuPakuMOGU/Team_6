using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class SoftwareCursor : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform cursorRt;     // 仮想カーソル
    GraphicRaycaster raycaster;
    PointerEventData ped;
    EventSystem es;

    void Awake()
    {
        raycaster = canvas.GetComponent<GraphicRaycaster>();
        es = EventSystem.current;
        ped = new PointerEventData(es);
    }

    void Update()
    {
        // 仮想カーソル位置からUIへレイ
        ped.position = cursorRt.position;
        var results = new List<RaycastResult>();
        raycaster.Raycast(ped, results);

        // 左クリックが押されたら、一番上のUIにクリックイベント
        if (Input.GetMouseButtonDown(0) && results.Count > 0)
        {
            var target = results[0].gameObject;
            ExecuteEvents.Execute(target, ped, ExecuteEvents.pointerClickHandler);
            // Buttonなら Submit も飛ばせる
            ExecuteEvents.Execute(target, ped, ExecuteEvents.submitHandler);
        }
    }
}
