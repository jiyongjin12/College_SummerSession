using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tergat : MonoBehaviour
{
    void Update()
    {
        Cursor.visible = false;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = Camera.main.transform.position.z + Camera.main.nearClipPlane;
        transform.position = mousePosition;
    }
}
