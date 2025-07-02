using UnityEngine;

public class Flocking_Spawn_Test : MonoBehaviour
{
    public GameObject fishPrefab;

    [Range(0, 300)]
    public int number;

    public Vector2 spawnAreaSize = new Vector2(10, 10);  // 추가: 사각형 범위

    private void Start()
    {
        for (int i = 0; i < number; i++)
        {
            Vector2 randomPos = new Vector2(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2)
            );
            var obj = Instantiate(fishPrefab, randomPos, Quaternion.identity);
            obj.GetComponent<Flocking_Test>().SetBounds(transform.position, spawnAreaSize);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);
    }
}
