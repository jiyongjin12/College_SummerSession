using UnityEngine;

public class test : MonoBehaviour
{
    public float speed = 1f;
    void Start()
    {
        
    }

    void Update()
    {
        Cursor.visible = false;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = Camera.main.transform.position.z + Camera.main.nearClipPlane;
        transform.position = mousePosition;
    }
}
