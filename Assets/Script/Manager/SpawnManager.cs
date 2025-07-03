using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour 
{
    // �̱���
    public static SpawnManager Instance { get; private set; } // �̱������� ���� ����

    [Header("������ ����� ���� ������")]
    public List<SpawnList> spawnFishUnitData; // �ν����Ϳ��� ������ ���� ����Ʈ

    [Header("���� ������ (Boid ������Ʈ ����)")]
    public Boid boidPrefab; // Boid ��ũ��Ʈ�� ���� ������ (�ν��Ͻ�ȭ�� ���)

    [Header("���Ƽ� ��ũ ���ø� ����")]
    public float minSpawnDistance = 2f; // �����(����) �� �ּ� �̰� �Ÿ�
    public int rejectionSamples = 30; // �� ������ ã�� ���� �õ��� �ִ� Ƚ��

    private List<Vector3> debugSpawnPoints = new List<Vector3>(); // ������ ���� ��ġ ����

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SpawnManager�� ������, �ϳ��� ����");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // ���� ���� �� ��� ����� ���� ��ȯ
        SpawnAllFishUnits();
    }

    public void SpawnAllFishUnits()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError("MapManager�� ���� �����ϴ�. ����� ���� �Ұ�.");
            return;
        }

        foreach (SpawnList spawnItem in spawnFishUnitData)
        {
            if (spawnItem.fishData == null)
            {
                Debug.LogWarning("SpawnList�� FishData�� �Ҵ���� �ʾҽ��ϴ�. �ǳʍ�.");
                continue;
            }
            SpawnFishBoids(spawnItem.fishData, spawnItem.boidSpawnCount);
        }
    }

    // ������ FishData�� ����Ͽ� Boid ������ ������ ����ŭ ��ȯ�մϴ�.
    // ��ȯ ��ġ�� �ش� ������� ���� �� ������ ���ǿ� �°� ���Ƽ� ��ũ ���ø����� �����˴ϴ�.
    // Z���� �׻� 0���� �����˴ϴ�.
    public void SpawnFishBoids(FishData fishToSpawn, int count)
    {
        List<Vector3> possibleSpawnPositions = new List<Vector3>();
        debugSpawnPoints.Clear(); // ���ο� ȣ�⸶�� �ʱ�ȭ

        int spawnAttemptCount = 0;
        int maxAttemptsPerFish = 1000; // ���� ���� ����

        // Y�� ���� ���� ��� (���� ��ǥ ����)
        // Y���� �����ϼ��� ������Ƿ�, minDepth�� Y���� 0�� ����� ����, maxDepth�� �� ������ ��
        float worldMinDepthY = -fishToSpawn.minDepth;
        float worldMaxDepthY = -fishToSpawn.maxDepth;

        // ���� Y ������ ������� ���� Y ������ �����Ͽ� ���� ���� ������ Y ���� ����
        // MapManager�� Y=0�� �����̰�, Y=-mapSize.y�� �ٴ��̶�� ����
        float mapBottomY = MapManager.Instance.transform.position.y - MapManager.Instance.mapSize.y / 2f;
        float mapTopY = MapManager.Instance.transform.position.y + MapManager.Instance.mapSize.y / 2f;

        // ���� ���� ������ Y ����: minDepth(���� ��)�� Y���� ����, maxDepth(���� ��)�� Y���� ����
        float spawnRangeYMin = Mathf.Max(mapBottomY, worldMaxDepthY); // �� ���� Y�� (���� ��)
        float spawnRangeYMax = Mathf.Min(mapTopY, worldMinDepthY);   // �� ���� Y�� (ū ��)

        // ���� X ���� (���� ��ǥ ����)
        float mapWorldMinX = MapManager.Instance.transform.position.x - MapManager.Instance.mapSize.x / 2f;
        float mapWorldMaxX = MapManager.Instance.transform.position.x + MapManager.Instance.mapSize.x / 2f;


        while (possibleSpawnPositions.Count < count && spawnAttemptCount < maxAttemptsPerFish)
        {
            spawnAttemptCount++;

            // ���� ���� ��ǥ ���� (���� X ������ ������� ���� Y ���� ���)
            float randomX = Random.Range(mapWorldMinX, mapWorldMaxX);
            float randomY = Random.Range(spawnRangeYMin, spawnRangeYMax);
            float randomZ = 0f; 

            Vector3 candidatePosition = new Vector3(randomX, randomY, randomZ);

            // �ش� ��ġ�� ���̿� ���� ��������
            Biome biomeAtPosition = MapManager.Instance.GetBiomeAtPosition(candidatePosition);

            if (biomeAtPosition == null)
            {
                // �� ������ ��� ��� (MapManager.GetBiomeAtPosition���� �̹� üũ)
                continue;
            }

            // ������� ������(FishHabitat)�� ���� ���̿��� HabitatType�� ���ԵǴ��� Ȯ��
            bool habitatMatches = fishToSpawn.habitats.Contains(biomeAtPosition.habitatType);

            if (!habitatMatches)
            {
                continue; // �������� ���� ������ ���� �õ�
            }

            // ���Ƽ� ��ũ ���ø�: ���� ��ȯ ��ġ��� �ּ� �Ÿ� ����
            bool tooClose = false;
            foreach (Vector3 existingPos in possibleSpawnPositions)
            {
                // Z���� 0���� �����Ǿ����Ƿ� 2D ��� �Ÿ� ���� ��������
                //if (Vector3.Distance(candidatePosition, existingPos) < minSpawnDistance)
                if (Vector3.Distance(candidatePosition, existingPos) < fishToSpawn.scopeOfActivity * 2)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                // ��ȿ�� ��ġ�� ã���� ����Ʈ�� �߰�
                possibleSpawnPositions.Add(candidatePosition);
                debugSpawnPoints.Add(candidatePosition); // ����� ����� ��ǥ ����
            }
        }

        // ��ȿ�� ��ġ�� Boid ������ �ν��Ͻ�ȭ
        foreach (Vector3 spawnPos in possibleSpawnPositions)
        {
            if (boidPrefab == null)
            {
                Debug.LogError("Boid Prefab�� SpawnManager�� �Ҵ���� �ʾҽ��ϴ�!");
                return;
            }

            Boid newBoid = Instantiate(boidPrefab, spawnPos, Quaternion.identity);
            newBoid.targetFishData = fishToSpawn; // Boid�� FishData �Ҵ�

            Debug.Log($"Spawned {fishToSpawn.fishName} Boid at {spawnPos} (Z:{spawnPos.z}) in Biome: {MapManager.Instance.GetBiomeAtPosition(spawnPos)?.biomeName}");
        }

        if (possibleSpawnPositions.Count < count)
        {
            Debug.LogWarning($"Requested {count} {fishToSpawn.fishName} but only managed to spawn {possibleSpawnPositions.Count} due to space/habitat constraints.");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        foreach (Vector3 pos in debugSpawnPoints)
        {
            Gizmos.DrawWireSphere(pos, minSpawnDistance * 0.5f); // �ּ� �Ÿ� �������� ������ ���� �� �׸���
            Gizmos.DrawSphere(pos, 0.1f); // �߽���
        }
    }
}

[System.Serializable]
public class SpawnList
{
    public FishData fishData; // ��ȯ�� ����� ������
    public int boidSpawnCount; // ��ȯ�� ����� �׷� ��
}