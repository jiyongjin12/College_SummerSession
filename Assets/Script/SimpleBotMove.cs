using UnityEngine;
using UnityEngine.Sprites;

public class SimpleBotMove : MonoBehaviour 
{
    public float speed;
    public Rigidbody2D rb;

    public GameObject body;

    private float FinalSpeed;

    void Start()
    {
        FinalSpeed = speed;
    }

    void Update()
    {
        Movement();
    }

    private void Movement()
    {
        float x = Input.GetAxis("Horizontal");
        Vector3 inputDir = new Vector3(x, 0, 0);

        rb.linearVelocity = inputDir * FinalSpeed;

        bool isMoving = !Mathf.Approximately(x, 0f);

        if (x >= 0f)
        {
            FinalSpeed = speed;
        }
        else if (x < 0f)
        {
            FinalSpeed = speed * .5f;
        }
    }
}
