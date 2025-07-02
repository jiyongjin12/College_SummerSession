using UnityEngine;

public class Flocking_Spawn_Test : MonoBehaviour
{
    public GameObject fishPrefab;

    [Range(0, 300)]
    public int number;

    private void Start()
    {
        for (int i = 0; i < number; i++)
        {
            Instantiate(fishPrefab, Vector3.zero, Quaternion.identity);
        }
    }
}
