using UnityEngine;

public class Fish : MonoBehaviour 
{

    public Transform target;
    public float speed;
    Rigidbody2D rg;

    void Start()
    {
        rg = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Vector3 diff = transform.position + target.position;
        rg.AddForce(-diff.normalized * speed * (rg.mass));
        Debug.DrawRay(transform.position, diff.normalized, Color.red);
    }
    
}
