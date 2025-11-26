using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{

    public Window window;
    public Shop_Maneger Maneger;

    public void OnButtonClick()
    {
       window.ToggleWindow();
       Maneger.BuyKagu();

    }
}

