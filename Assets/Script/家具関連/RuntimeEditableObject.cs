
using UnityEngine;
using UnityEngine.EventSystems;
using UCamera = UnityEngine.Camera;

public class RuntimeEditableObject : MonoBehaviour
{
    [Header("地面レイヤー")]
    public LayerMask groundLayer;

    [Header("スナップ")]
    public float gridSize = 1.0f;
    public float angleSnap = 15f;

    [Header("ナッジステップ")]
    public float nudgeStep = 0.05f;
    public float rotateStep = 5f;

    [Header("傾き上限（Align時の制限共有）")]
    public float maxTiltDegrees = 10f;

    private enum EditMode { None, Move, Rotate }
    private EditMode mode = EditMode.None;

    private bool selected = false;
    private bool snapGridOn = true;
    private bool snapAngleOn = true;

    private UCamera cam;
    private Vector3 dragOffset;
    private float baseY;

    private void Start()
    {
        cam = UCamera.main;
        if (cam == null)
        {
            Debug.LogWarning("MainCamera が見つかりません。タグ設定を確認してください。");
            cam = Object.FindObjectOfType<UCamera>();
        }
        baseY = transform.position.y;
    }

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        HandleSelection();
        if (!selected) return;

        HandleModeKeys();
        HandleEditActions();
        HandleNudgeKeys();
        HandleDelete();
    }

    private void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    selected = true;
                    Highlight(true);
                }
                else
                {
                    if (selected)
                    {
                        selected = false;
                        Highlight(false);
                        mode = EditMode.None;
                    }
                }
            }
        }
    }

    private void Highlight(bool on)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r.material.HasProperty("_Color"))
            {
                var c = r.material.color;
                r.material.color = on ? c * 1.15f : c * (1f / 1.15f);
            }
        }
    }

    private void HandleModeKeys()
    {
        if (Input.GetKeyDown(KeyCode.W)) mode = EditMode.Move;
        if (Input.GetKeyDown(KeyCode.E)) mode = EditMode.Rotate;

        if (Input.GetKeyDown(KeyCode.T)) snapGridOn = !snapGridOn;
        if (Input.GetKeyDown(KeyCode.Y)) snapAngleOn = !snapAngleOn;
    }

    private void HandleEditActions()
    {
        if (mode == EditMode.Move)
        {
            if (Input.GetMouseButtonDown(0))
                dragOffset = transform.position - GetGroundPointUnderMouse(baseY);

            if (Input.GetMouseButton(0))
            {
                Vector3 target = GetGroundPointUnderMouse(baseY) + dragOffset;
                if (snapGridOn && gridSize > 0f)
                {
                    target.x = Mathf.Round(target.x / gridSize) * gridSize;
                    target.z = Mathf.Round(target.z / gridSize) * gridSize;
                }
                transform.position = target;
            }
        }
        else if (mode == EditMode.Rotate)
        {
            if (Input.GetMouseButton(0))
            {
                float delta = Input.GetAxis("Mouse X") * 5f;
                float y = transform.eulerAngles.y + delta;
                if (snapAngleOn && angleSnap > 0f)
                    y = Mathf.Round(y / angleSnap) * angleSnap;
                transform.rotation = Quaternion.Euler(0f, y, 0f);
            }
        }
    }

    private Vector3 GetGroundPointUnderMouse(float fixedY)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
            return new Vector3(hit.point.x, fixedY, hit.point.z);

        Plane plane = new Plane(Vector3.up, new Vector3(0f, fixedY, 0f));
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);

        return transform.position;
    }

    private void HandleNudgeKeys()
    {
        Vector3 pos = transform.position;

        if (Input.GetKey(KeyCode.LeftArrow)) pos.x -= nudgeStep;
        if (Input.GetKey(KeyCode.RightArrow)) pos.x += nudgeStep;
        if (Input.GetKey(KeyCode.UpArrow)) pos.z += nudgeStep;
        if (Input.GetKey(KeyCode.DownArrow)) pos.z -= nudgeStep;

        if (snapGridOn && gridSize > 0f)
        {
            pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
            pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
        }

        transform.position = pos;

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
        {
            float y = transform.eulerAngles.y;
            y += (Input.GetKeyDown(KeyCode.Q) ? -rotateStep : rotateStep);
            if (snapAngleOn && angleSnap > 0f)
                y = Mathf.Round(y / angleSnap) * angleSnap;
            transform.rotation = Quaternion.Euler(0f, y, 0f);
        }
    }

    private void HandleDelete()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
            Destroy(gameObject);
    }
}
