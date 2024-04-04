using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Testing : MonoBehaviour
{
    public RawImage img;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
            StartTest();
    }

    public void StartTest()
    {
    }
}
