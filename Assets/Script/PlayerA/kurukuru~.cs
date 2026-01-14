using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class kurukuru : MonoBehaviour
{
    public float rotationSpeed = 50f; // ‰ñ“]‘¬“x

    void Update()
    {
        // Y²‚ğ’†S‚É‰ñ“]
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
