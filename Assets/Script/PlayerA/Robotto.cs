using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robotto : MonoBehaviour
{

    [SerializeField] private Transform player;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float stoppingDistance = 1.2f;

    bool Chice_flag = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        if (Chice_flag == true)
        {
            Vector3 toPlayer = player.position - transform.position;
            float dist = toPlayer.magnitude;

            if (dist > stoppingDistance)
            {
                // 回転（プレイヤー方向を向く）
                Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

                // 前進
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
            }

        }


    }


    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            Debug.Log("プレイヤーと衝突");
            Chice_flag = true;


        }

    }






}

