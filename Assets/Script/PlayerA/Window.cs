using UnityEngine;

public class Window : MonoBehaviour
{
    public bool WindowOpen { get; private set; }
    [SerializeField] GameObject image;
    [SerializeField] float moveDistance = 100f; // UIならピクセル想定にするのがおすすめ
    [SerializeField] float moveSpeed = 10f;

    Vector3 originalPosition;
    Vector3 openPosition;
    Vector3 targetPosition;

    void Awake()
    {
        originalPosition = image.transform.localPosition;
        openPosition = originalPosition + new Vector3(0, moveDistance, 0);
        targetPosition = originalPosition;
        WindowOpen = false;
    }

    public void Open()
    {
        WindowOpen = true;
        image.SetActive(true);
        targetPosition = openPosition;

      
    }

    void Update()
    {
        var before = image.transform.localPosition;

        float dt = Time.deltaTime;
        float step = moveSpeed * dt;
        float dist = Vector3.Distance(before, targetPosition);

        image.transform.localPosition = Vector3.MoveTowards(before, targetPosition, step);

        // 毎フレーム出すと多いので、変化があった時だけ
        if ((image.transform.localPosition - before).sqrMagnitude > 0.000001f)
        {
         
        }
    }

    public void Close()
    {
        WindowOpen = false;
        targetPosition = originalPosition;
        Debug.Log("ウィンドウを閉じました");
    }

    public void Toggle()
    {
       
        if (WindowOpen) Close();
        else Open();
    }
}