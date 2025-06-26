using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SpriteStrecher : MonoBehaviour
{
  
    void Update()
    {
        GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Tiled;
    }


}
