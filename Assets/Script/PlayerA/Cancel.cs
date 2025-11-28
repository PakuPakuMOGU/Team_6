using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cancel : MonoBehaviour
{
    public Shop_Maneger Maneger;

    public void OnButtonClick()
    {
        Maneger.Cancel();

    }
}
