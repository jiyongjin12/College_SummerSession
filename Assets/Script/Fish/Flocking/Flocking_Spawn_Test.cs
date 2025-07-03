using UnityEngine;

public class Flocking_Spawn_Test : MonoBehaviour
{
    public GameObject fishPrefab; // Flocking_Test ��ũ��Ʈ�� ������ ����� ������

    [Range(1, 700)] // �ּ� 1������ �����ϵ��� ���� ����
    public int numberToSpawn = 100; // ������ ������� ���� (������ ����)

    public Vector2 spawnAreaSize = new Vector2(10, 10); // ����Ⱑ ������ �簢�� ������ ũ��

    private void Start()
    {
        for (int i = 0; i < numberToSpawn; i++) // ������ ����
        {
            // ������ ���� ���� ������ ���� ��ġ ����
            Vector2 randomPos = new Vector2(
                Random.Range(transform.position.x - spawnAreaSize.x / 2, transform.position.x + spawnAreaSize.x / 2),
                Random.Range(transform.position.y - spawnAreaSize.y / 2, transform.position.y + spawnAreaSize.y / 2)
            );
            // Z���� 0���� �����Ͽ� �ν��Ͻ�ȭ
            Vector3 spawnPosition3D = new Vector3(randomPos.x, randomPos.y, 0f);

            var obj = Instantiate(fishPrefab, spawnPosition3D, Quaternion.identity);

            // Flocking_Test ������Ʈ ��������
            Flocking_Test flockingAgent = obj.GetComponent<Flocking_Test>();
            if (flockingAgent != null)
            {
                // ������ ������Ʈ���� ��� ���� ����
                flockingAgent.SetBounds(transform.position, spawnAreaSize);
            }
            else
            {
                Debug.LogWarning($"Spawned object {obj.name} does not have a Flocking_Test component!");
            }
        }
    }

    // Scene �信�� ���� ������ �ð�ȭ�մϴ�.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        // transform.position�� �߽����� spawnAreaSize ũ���� ���̾� ť�� �׸���
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0.01f)); // Z���� ���
    }
}
