using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour 
{
    // 싱글톤
    public static SpawnManager Instance { get; private set; } // 싱글톤으로 쉽게 접근

    [Header("스폰할 물고기 유닛 데이터")]
    public List<SpawnList> spawnFishUnitData; // 인스펙터에서 설정할 스폰 리스트

    [Header("군집 프리팹 (Boid 컴포넌트 포함)")]
    public Boid boidPrefab; // Boid 스크립트가 붙은 프리팹 (인스턴스화할 대상)

    [Header("포아송 디스크 샘플링 설정")]
    public float minSpawnDistance = 2f; // 물고기(군집) 간 최소 이격 거리
    public int rejectionSamples = 30; // 한 지점을 찾기 위해 시도할 최대 횟수

    private List<Vector3> debugSpawnPoints = new List<Vector3>(); // 기즈모용 스폰 위치 저장

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
        // 게임 시작 시 모든 물고기 군집 소환
        SpawnAllFishUnits();
    }

    public void SpawnAllFishUnits()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError("MapManager가 씬에 없습니다. 물고기 스폰 불가.");
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

    // 지정된 FishData를 사용하여 Boid 군집을 설정된 수만큼 소환합니다.
    // 소환 위치는 해당 물고기의 수심 및 서식지 조건에 맞게 포아송 디스크 샘플링으로 결정됩니다.
    // Z축은 항상 0으로 고정됩니다.
    public void SpawnFishBoids(FishData fishToSpawn, int count)
    {
        List<Vector3> possibleSpawnPositions = new List<Vector3>();
        debugSpawnPoints.Clear(); // 새로운 호출마다 초기화

        int spawnAttemptCount = 0;
        int maxAttemptsPerFish = 1000; // 무한 루프 방지

        // Y축 수심 범위 계산 (월드 좌표 기준)
        // Y값이 음수일수록 깊어지므로, minDepth는 Y값이 0에 가까운 음수, maxDepth는 더 음수가 됨
        float worldMinDepthY = -fishToSpawn.minDepth;
        float worldMaxDepthY = -fishToSpawn.maxDepth;

        // 맵의 Y 범위와 물고기의 수심 Y 범위를 교차하여 실제 스폰 가능한 Y 범위 결정
        // MapManager의 Y=0이 수면이고, Y=-mapSize.y가 바닥이라고 가정
        float mapBottomY = MapManager.Instance.transform.position.y - MapManager.Instance.mapSize.y / 2f;
        float mapTopY = MapManager.Instance.transform.position.y + MapManager.Instance.mapSize.y / 2f;

        // 실제 스폰 가능한 Y 범위: minDepth(얕은 곳)는 Y값이 높고, maxDepth(깊은 곳)는 Y값이 낮음
        float spawnRangeYMin = Mathf.Max(mapBottomY, worldMaxDepthY); // 더 깊은 Y값 (작은 값)
        float spawnRangeYMax = Mathf.Min(mapTopY, worldMinDepthY);   // 더 얕은 Y값 (큰 값)

        // 맵의 X 범위 (월드 좌표 기준)
        float mapWorldMinX = MapManager.Instance.transform.position.x - MapManager.Instance.mapSize.x / 2f;
        float mapWorldMaxX = MapManager.Instance.transform.position.x + MapManager.Instance.mapSize.x / 2f;


        while (possibleSpawnPositions.Count < count && spawnAttemptCount < maxAttemptsPerFish)
        {
            spawnAttemptCount++;

            // 랜덤 월드 좌표 생성 (맵의 X 범위와 물고기의 수심 Y 범위 고려)
            float randomX = Random.Range(mapWorldMinX, mapWorldMaxX);
            float randomY = Random.Range(spawnRangeYMin, spawnRangeYMax);
            float randomZ = 0f; 

            Vector3 candidatePosition = new Vector3(randomX, randomY, randomZ);

            // 해당 위치의 바이옴 정보 가져오기
            Biome biomeAtPosition = MapManager.Instance.GetBiomeAtPosition(candidatePosition);

            if (biomeAtPosition == null)
            {
                // 맵 범위를 벗어난 경우 (MapManager.GetBiomeAtPosition에서 이미 체크)
                continue;
            }

            // 물고기의 서식지(FishHabitat)가 현재 바이옴의 HabitatType에 포함되는지 확인
            bool habitatMatches = fishToSpawn.habitats.Contains(biomeAtPosition.habitatType);

            if (!habitatMatches)
            {
                continue; // 서식지가 맞지 않으면 다음 시도
            }

            // 포아송 디스크 샘플링: 기존 소환 위치들과 최소 거리 유지
            bool tooClose = false;
            foreach (Vector3 existingPos in possibleSpawnPositions)
            {
                // Z축이 0으로 고정되었으므로 2D 평면 거리 계산과 동일해짐
                //if (Vector3.Distance(candidatePosition, existingPos) < minSpawnDistance)
                if (Vector3.Distance(candidatePosition, existingPos) < fishToSpawn.scopeOfActivity * 2)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                // 유효한 위치를 찾으면 리스트에 추가
                possibleSpawnPositions.Add(candidatePosition);
                debugSpawnPoints.Add(candidatePosition); // 기즈모에 사용할 좌표 저장
            }
        }

        // 유효한 위치에 Boid 프리팹 인스턴스화
        foreach (Vector3 spawnPos in possibleSpawnPositions)
        {
            if (boidPrefab == null)
            {
                Debug.LogError("Boid Prefab이 SpawnManager에 할당되지 않았습니다!");
                return;
            }

            Boid newBoid = Instantiate(boidPrefab, spawnPos, Quaternion.identity);
            newBoid.targetFishData = fishToSpawn; // Boid에 FishData 할당

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
            Gizmos.DrawWireSphere(pos, minSpawnDistance * 0.5f); // 최소 거리 반지름의 반으로 작은 원 그리기
            Gizmos.DrawSphere(pos, 0.1f); // 중심점
        }
    }
}

[System.Serializable]
public class SpawnList
{
    public FishData fishData; // 소환할 물고기 데이터
    public int boidSpawnCount; // 소환할 물고기 그룹 수
}