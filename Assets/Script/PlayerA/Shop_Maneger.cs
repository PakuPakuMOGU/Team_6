using System.Collections.Generic;
using UnityEngine;

public class Shop_Maneger : MonoBehaviour
{
    [Header("罠の在庫（1つずつ設置）")]
    [SerializeField] public Transform[] targets;
    public int targetIndex = 0;

    [Header("レイキャスト設定")]
    [SerializeField] private UnityEngine.Camera cam;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private bool alignToNormal = true; 

    [Tooltip("Pivotが底面なら0のままでOK。Pivotが中心の場合、モデルの半径を自動計算して補正します。")]
    [SerializeField] private float extraLift = 0.01f; 


    public LayerMask groundMask;      
    public float maxSlopeDeg = 45f;   


    public CenterRaycastSpaceApply cast;


   
    public Vector3 HitPoint { get; private set; }
    public Vector3 HitNormal { get; private set; }

   
    private Stack<(Transform t, Vector3 pos, Quaternion rot)> placedHistory = new Stack<(Transform, Vector3, Quaternion)>();

    public GameObject Button_Canbus;
 
 

    
    public bool TryGetGroundHit(out Vector3 hitPoint, out Vector3 hitNormal)
    {
        hitPoint = default;
        hitNormal = default;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            
            float cos = Vector3.Dot(hit.normal.normalized, Vector3.up);
            float slopeDeg = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
            if (slopeDeg <= maxSlopeDeg)
            {
                hitPoint = hit.point;
                hitNormal = hit.normal.normalized;
                return true;
            }
        }
        return false;
    }




    void Start()
    {
        Button_Canbus.SetActive(false);
        

    }

    void Awake()
    {
        if (cam == null) cam = UnityEngine.Camera.main;
        groundMask = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));



        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            HitPoint = hit.point;
            HitNormal = hit.normal;
        }
        else
        {
            HitPoint = ray.GetPoint(maxDistance);
            HitNormal = -ray.direction;
        }

    }



    public void BuyKagu()
    {
        
        cast.kono();

        while (targets != null && targetIndex < targets.Length && targets[targetIndex] == null)
            targetIndex++;
        if (targets == null || targetIndex >= targets.Length) return;

        Transform t = targets[targetIndex];
        if (t == null) return;

        
        placedHistory.Push((t, t.position, t.rotation));

        if (cast != null && cast.savedPositions != null && cast.savedPositions.Count > 0)
        {
            Vector3 lastPos = cast.savedPositions[cast.savedPositions.Count - 1];

            
           t.position = lastPos;

          

            targetIndex++;
            return;
        }

     
        var box = t.GetComponentInChildren<BoxCollider>();
        float half = 0f;
        Vector3 currentCenterWorld = t.position;
        if (box != null)
        {
            currentCenterWorld = t.TransformPoint(box.center);
            Vector3 ext = Vector3.Scale(box.size * 0.5f, t.lossyScale);
            Vector3 n = HitNormal.normalized;
            half = Mathf.Abs(Vector3.Dot(n, t.right)) * ext.x +
                   Mathf.Abs(Vector3.Dot(n, t.up)) * ext.y +
                   Mathf.Abs(Vector3.Dot(n, t.forward)) * ext.z;
        }
        else
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                Vector3 c = b.center;
                Vector3 e = b.extents;
                currentCenterWorld = c;

                Vector3[] corners = new Vector3[]
                {
                c + new Vector3( e.x,  e.y,  e.z),
                c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3( e.x, -e.y,  e.z),
                c + new Vector3( e.x, -e.y, -e.z),
                c + new Vector3(-e.x,  e.y,  e.z),
                c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z),
                c + new Vector3(-e.x, -e.y, -e.z)
                };
                float maxProj = 0f;
                Vector3 n = HitNormal.normalized;
                foreach (var wc in corners)
                {
                    float proj = Mathf.Abs(Vector3.Dot(wc - c, n));
                    maxProj = Mathf.Max(maxProj, proj);
                }
                half = maxProj;
            }
            else
            {
                half = 0.5f;
                currentCenterWorld = t.position;
            }
        }

        float lift = Mathf.Max(0f, extraLift);
        Vector3 desiredCenterWorld = HitPoint + HitNormal.normalized * (half + lift);
        Vector3 delta = desiredCenterWorld - currentCenterWorld;
        t.position += delta;

        Button_Canbus.SetActive(true);
       

    }


    public void Hensyu()
    {
         targetIndex++;
        Button_Canbus.SetActive(false);

    }



    public void Cancel()
    {
        if (placedHistory.Count == 0) return;

        var last = placedHistory.Pop();
        last.t.position = last.pos;
        last.t.rotation = last.rot;

        targetIndex = Mathf.Max(targetIndex - 1, 0);

        Button_Canbus.SetActive(false);

    }

    private float ComputeHalfExtentAlongNormal(Transform t, Vector3 normal)
    {

        Collider col = t.GetComponentInChildren<Collider>();
        if (col != null)
        {
            var e = col.bounds.extents;
            return Mathf.Abs(normal.x) * e.x + Mathf.Abs(normal.y) * e.y + Mathf.Abs(normal.z) * e.z;
        }

        Renderer rend = t.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            var e = rend.bounds.extents;
            return Mathf.Abs(normal.x) * e.x + Mathf.Abs(normal.y) * e.y + Mathf.Abs(normal.z) * e.z;
        }


        return 0f;
    }
}
