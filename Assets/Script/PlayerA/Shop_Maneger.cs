using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop_Maneger : MonoBehaviour
{
    private int targetIndex = 0;

    [Header("罠の在庫")]
    [SerializeField] private Transform[] targets;

    private Vector3 originalPosition; // 元の位置を保存
    private bool hasSaved = false;    // 保存済みかどうか

    public GameObject CancelButton;



    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void BuyKagu()
    {
        if (targetIndex >= targets.Length)
        {
            return;//置くものない
        }

        if (!hasSaved)
        {
            originalPosition = targets[targetIndex].position;
            hasSaved = true;
        }















    }


    public void Cancel(GameObject target)
    {


        if (hasSaved)
        {
            target.transform.position = originalPosition;
            hasSaved = false; // 一度戻したらリセット
        }



    }

}
