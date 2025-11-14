using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartButton : MonoBehaviour
{
    public View viewCanNotPlay;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    // ƒvƒŒƒC‚ð‰Ÿ‚µ‚½
    public void OnButtonPlay()
    {
        viewCanNotPlay.WindowView();
    }

    // —V‚Ñ•û‚ð‰Ÿ‚µ‚½.
    public void OnButtonGameWork()
    {

    }
}
