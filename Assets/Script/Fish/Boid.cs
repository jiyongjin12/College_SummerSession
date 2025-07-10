using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public class Boid : MonoBehaviour
{
    public FishData targetFishData;
    public Biome currentBiome;

    // 개별 물고기 프리팹 (Fish.cs를 상속하는 프리팹이어야 함)
    //public GameObject fishPrefab; // FishData에 있는 fishPrefab 사용 예정이므로 이 변수는 필요없을 수도 있음

    private Vector2 _flockingBoundsCenter;
    private float _flockingBoundsRadius;

    // Boid의 경계를 설정하는 메서드 (SpawnManager에서 호출)
    public void SetFlockingBounds(Vector2 center, float radius)
    {
        _flockingBoundsCenter = center;
        _flockingBoundsRadius = radius;
    }

    void Start()
    {
        if (targetFishData == null)
        {
            Debug.LogError("Boid에 targetFishData가 할당되지 않았습니다. Boid를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        // targetFishData.fishPrefab을 사용하도록 변경
        if (targetFishData.fishPrefab == null)
        {
            Debug.LogError($"Boid에 할당된 FishData ({targetFishData.name})에 Fish Prefab이 할당되지 않았습니다. Boid를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        SpawnIndividualFish();
    }

    private void SpawnIndividualFish()
    {
        Vector2 tempRectSize = new Vector2(_flockingBoundsRadius * 2, _flockingBoundsRadius * 2);
        Vector2 tempRectOffset = _flockingBoundsCenter - new Vector2(_flockingBoundsRadius, _flockingBoundsRadius);

        float individualMinSeparation = 0.5f; // 개별 물고기 간 최소 간격

        // UnityEngine.Random을 사용하기 위한 시드 초기화는 Start() 또는 Awake()에서 한 번만 하는 것이 좋습니다.
        // 여기서는 PoissonDiskSampling2D 내부에서 Random을 사용하므로, 
        // PoissonDiskSampling2D 클래스 자체를 수정하는 것이 더 적절합니다.
        // UnityEngine.Random.InitState(System.Environment.TickCount); 

        List<Vector2> spawnPoints = PoissonDiskSampling2D.GeneratePoints(
            individualMinSeparation,
            tempRectSize,
            rejectionSamples: 30,
            offset: tempRectOffset
        );

        int spawnedCount = 0;
        foreach (Vector2 point in spawnPoints)
        {
            // 군집 반경 내에 있는 경우에만 스폰
            if (Vector2.Distance(point, _flockingBoundsCenter) > _flockingBoundsRadius)
            {
                continue;
            }

            if (spawnedCount >= targetFishData.fishUnitCount) break; // FishData에 정의된 개수만큼만 스폰

            Vector3 spawnPosition = new Vector3(point.x, point.y, 0f);

            // targetFishData.fishPrefab 사용
            GameObject fishObj = Instantiate(targetFishData.fishPrefab, spawnPosition, Quaternion.identity, this.transform);

            Fish individualFish = fishObj.GetComponent<Fish>();
            if (individualFish != null)
            {
                individualFish.fishData = targetFishData;
                individualFish.parentBoid = this;
                individualFish.currentBiome = this.currentBiome;
                individualFish.SetFlockingBounds(_flockingBoundsCenter, _flockingBoundsRadius);

                // FishSimulationManager에 개별 Fish 등록
                if (FishSimulationManager.Instance != null)
                {
                    FishSimulationManager.Instance.RegisterFish(individualFish);
                }
            }
            else
            {
                Debug.LogWarning($"Spawned object {fishObj.name} does not have a Fish component. Destroying it.");
                Destroy(fishObj);
                continue;
            }
            spawnedCount++;
        }
    }

    private void OnDestroy()
    {
        // Boid가 파괴될 때, 그 자식인 개별 물고기들도 같이 파괴됩니다.
        // 각 Fish의 OnDestroy에서 FishSimulationManager.Instance.UnregisterFish(this)가 호출되므로
        // Boid에서는 특별히 추가적인 언레지스터 로직이 필요하지 않습니다.
    }

    // Gizmos for Debugging
    void OnDrawGizmosSelected()
    {
        if (targetFishData == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_flockingBoundsCenter, _flockingBoundsRadius);
    }
}

// PoissonDiskSampling2D 클래스: 이 클래스가 Boid.cs 파일에 함께 있거나,
// 별도의 파일로 존재한다면 해당 파일의 Random 사용 부분을 수정해야 합니다.
public static class PoissonDiskSampling2D
{
    public static List<Vector2> GeneratePoints(float radius, Vector2 sampleRegionSize, int rejectionSamples = 30, Vector2 offset = default(Vector2))
    {
        float cellSize = radius / Mathf.Sqrt(2);

        int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
        List<Vector2> points = new List<Vector2>();
        List<Vector2> spawnPoints = new List<Vector2>();

        spawnPoints.Add(sampleRegionSize / 2f);
        points.Add(spawnPoints[0]);
        grid[(int)(spawnPoints[0].x / cellSize), (int)(spawnPoints[0].y / cellSize)] = 1;

        while (spawnPoints.Count > 0)
        {
            // 여기에서 UnityEngine.Random을 명시적으로 사용합니다.
            int spawnIndex = UnityEngine.Random.Range(0, spawnPoints.Count);
            Vector2 spawnCenter = spawnPoints[spawnIndex];
            bool found = false;

            for (int i = 0; i < rejectionSamples; i++)
            {
                // 여기에서 UnityEngine.Random을 명시적으로 사용합니다.
                float angle = UnityEngine.Random.value * Mathf.PI * 2;
                float r = UnityEngine.Random.Range(radius, 2 * radius);
                Vector2 candidate = spawnCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;

                if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
                {
                    int cellX = (int)(candidate.x / cellSize);
                    int cellY = (int)(candidate.y / cellSize);
                    bool ok = true;

                    for (int x = -2; x <= 2; x++)
                    {
                        for (int y = -2; y <= 2; y++)
                        {
                            int neighborX = cellX + x;
                            int neighborY = cellY + y;

                            if (neighborX >= 0 && neighborX < grid.GetLength(0) && neighborY >= 0 && neighborY < grid.GetLength(1))
                            {
                                if (grid[neighborX, neighborY] != 0)
                                {
                                    Vector2 neighborPoint = points[grid[neighborX, neighborY] - 1];
                                    if (Vector2.Distance(candidate, neighborPoint) < radius)
                                    {
                                        ok = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!ok) break;
                    }

                    if (ok)
                    {
                        points.Add(candidate);
                        spawnPoints.Add(candidate);
                        grid[cellX, cellY] = points.Count;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        for (int i = 0; i < points.Count; i++)
        {
            points[i] += offset;
        }

        return points;
    }
}