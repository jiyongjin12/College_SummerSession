using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // 싱글톤
    public static SpawnManager Instance { get; private set; }

    [Header("스폰할 물고기 유닛 데이터")]
    public List<SpawnList> spawnFishUnitData;

    [Header("군집 프리팹")]
    public Boid boidPrefab;

    [Header("포아송 디스크 샘플링")]
    public float minSpawnDistance = 4f;
    public int rejectionSamples = 30;

    public LayerMask wallLayer;

    private List<Vector3> debugSpawnPoints = new List<Vector3>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SpawnManager가 여러개, 하나로 조정");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        SpawnAllFishUnits();
    }

    public void SpawnAllFishUnits()
    {
        if (MapManager.Instance == null)
        {
            return;
        }

        foreach (SpawnList spawnItem in spawnFishUnitData)
        {
            if (spawnItem.fishData == null)
            {
                Debug.LogWarning("SpawnList에 FishData가 할당되지 않았습니다. 건너뜜.");
                continue;
            }
            SpawnFishBoids(spawnItem.fishData, spawnItem.boidSpawnCount);
        }
    }

    public void SpawnFishBoids(FishData fishToSpawn, int count)
    {
        List<Vector3> possibleSpawnPositions = new List<Vector3>();
        int spawnAttemptCount = 0;
        int maxAttemptsPerFish = 1000;

        float worldMinDepthY = -fishToSpawn.minDepth;
        float worldMaxDepthY = -fishToSpawn.maxDepth;

        float mapBottomY = MapManager.Instance.transform.position.y - MapManager.Instance.mapSize.y / 2f;
        float mapTopY = MapManager.Instance.transform.position.y + MapManager.Instance.mapSize.y / 2f;

        float spawnRangeYMin = Mathf.Max(mapBottomY, worldMaxDepthY);
        float spawnRangeYMax = Mathf.Min(mapTopY, worldMinDepthY);

        float mapWorldMinX = MapManager.Instance.transform.position.x - MapManager.Instance.mapSize.x / 2f;
        float mapWorldMaxX = MapManager.Instance.transform.position.x + MapManager.Instance.mapSize.x / 2f;

        List<Biome> spawnedBiomesForBoids = new List<Biome>();


        while (possibleSpawnPositions.Count < count && spawnAttemptCount < maxAttemptsPerFish)
        {
            spawnAttemptCount++;

            float randomX = Random.Range(mapWorldMinX, mapWorldMaxX);
            float randomY = Random.Range(spawnRangeYMin, spawnRangeYMax);
            float randomZ = 0f;

            Vector3 candidatePosition = new Vector3(randomX, randomY, randomZ);

            Biome biomeAtPosition = MapManager.Instance.GetBiomeAtPosition(candidatePosition);

            if (biomeAtPosition == null)
            {
                continue;
            }

            bool habitatMatches = fishToSpawn.habitats.Contains(biomeAtPosition.habitatType);

            if (!habitatMatches)
            {
                continue;
            }

            float checkRadius = 2f;
            if (Physics2D.OverlapCircle(candidatePosition, checkRadius, wallLayer))
            {
                continue;
            }

            bool tooClose = false;
            foreach (Vector3 existingPos in possibleSpawnPositions)
            {
                if (Vector3.Distance(candidatePosition, existingPos) < minSpawnDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                possibleSpawnPositions.Add(candidatePosition);
                debugSpawnPoints.Add(candidatePosition);
                spawnedBiomesForBoids.Add(biomeAtPosition);
            }
        }

        for (int i = 0; i < possibleSpawnPositions.Count; i++)
        {
            Vector3 spawnPos = possibleSpawnPositions[i];
            Biome boidBiome = spawnedBiomesForBoids[i]; // 해당 Boid의 Biome 가져오기

            if (boidPrefab == null)
            {
                Debug.LogError("Boid Prefab이 SpawnManager에 할당되지 않았습니다!");
                return;
            }

            Boid newBoid = Instantiate(boidPrefab, spawnPos, Quaternion.identity, this.transform);
            newBoid.targetFishData = fishToSpawn;
            newBoid.currentBiome = boidBiome; // Boid에 바이옴 정보 할당 (Start에서 활용됨)

            // ===== 변경: SetBoidActivityBounds 호출 (이제는 Boid의 활동 영역을 의미) =====
            // fishToSpawn.scopeOfActivity는 개별 물고기의 활동 범위로, Boid의 활동 영역 반경으로 적합
            newBoid.SetBoidActivityBounds(spawnPos, fishToSpawn.scopeOfActivity);
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
            Gizmos.DrawWireSphere(pos, minSpawnDistance * 0.5f);
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }
}

[System.Serializable]
public class SpawnList
{
    public FishData fishData; // 소환할 물고기 데이터
    public int boidSpawnCount; // 소환할 물고기 그룹 수
}