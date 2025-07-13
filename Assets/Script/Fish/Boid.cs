using System.Collections.Generic;
using UnityEngine;
using Unity.Collections; // 현재 Boid 스크립트에서는 직접 사용되지 않지만, Unity.Mathematics와 함께 습관적으로 포함될 수 있습니다.
using Unity.Mathematics; // 현재 Boid 스크립트에서는 직접 사용되지 않지만, 필요에 따라 사용될 수 있습니다.

public class Boid : MonoBehaviour
{
    public FishData targetFishData;
    public Biome currentBiome; // 이 Boid가 스폰될 Biome 정보 (SpawnManager에서 할당)

    // 이 Boid가 스폰한 개별 물고기들이 활동할 영역 (Boid의 활동 경계)
    private Vector2 _boidActivityCenter;
    private float _boidActivityRadius;

    // Boid의 활동 경계(스폰 영역)를 설정하는 메서드 (SpawnManager에서 호출)
    public void SetBoidActivityBounds(Vector2 center, float radius)
    {
        _boidActivityCenter = center;
        _boidActivityRadius = radius;
    }

    void Start()
    {
        if (targetFishData == null)
        {
            Debug.LogError("Boid에 targetFishData가 할당되지 않았습니다. Boid를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        if (targetFishData.fishPrefab == null)
        {
            Debug.LogError($"Boid에 할당된 FishData ({targetFishData.name})에 Fish Prefab이 할당되지 않았습니다. Boid를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        // Boid가 활성화되는 시점에 자기가 속한 바이옴의 월드 좌표 경계를 계산
        Vector2 biomeMinBounds = Vector2.zero;
        Vector2 biomeMaxBounds = Vector2.zero;
        if (currentBiome != null && MapManager.Instance != null)
        {
            // Biome의 로컬 좌표를 월드 좌표로 변환
            Vector3 biomeWorldCenter = MapManager.Instance.transform.position + currentBiome.center;
            Vector3 biomeWorldSize = currentBiome.size; // Biome의 크기가 3D라면 Vector3
            biomeMinBounds = new Vector2(biomeWorldCenter.x - biomeWorldSize.x / 2f, biomeWorldCenter.y - biomeWorldSize.y / 2f);
            biomeMaxBounds = new Vector2(biomeWorldCenter.x + biomeWorldSize.x / 2f, biomeWorldCenter.y + biomeWorldSize.y / 2f);
        }

        SpawnIndividualFish(biomeMinBounds, biomeMaxBounds);
    }

    private void SpawnIndividualFish(Vector2 biomeMinBounds, Vector2 biomeMaxBounds)
    {
        // PoissonDiskSampling은 사각형 영역을 기반으로 하므로, 원형 스폰 영역을 사각형으로 변환
        Vector2 tempRectSize = new Vector2(_boidActivityRadius * 2, _boidActivityRadius * 2);
        Vector2 tempRectOffset = _boidActivityCenter - new Vector2(_boidActivityRadius, _boidActivityRadius);

        float individualMinSeparation = 0.5f; // 개별 물고기 간 최소 간격

        List<Vector2> spawnPoints = PoissonDiskSampling2D.GeneratePoints(
            individualMinSeparation,
            tempRectSize,
            rejectionSamples: 30,
            offset: tempRectOffset
        );

        // 현재 Boid 인스턴스의 고유 ID를 부모 ID로 사용합니다.
        int boidParentID = this.GetInstanceID();

        int spawnedCount = 0;
        foreach (Vector2 point in spawnPoints)
        {
            // 실제 원형 스폰 영역 내에 있는 경우에만 스폰
            if (Vector2.Distance(point, _boidActivityCenter) > _boidActivityRadius)
            {
                continue;
            }

            if (spawnedCount >= targetFishData.fishUnitCount) break; // FishData에 정의된 개수만큼만 스폰

            Vector3 spawnPosition = new Vector3(point.x, point.y, 0f);

            GameObject fishObj = Instantiate(targetFishData.fishPrefab, spawnPosition, Quaternion.identity, this.transform);

            Fish individualFish = fishObj.GetComponent<Fish>();
            if (individualFish != null)
            {
                individualFish.fishData = targetFishData;
                individualFish.parentID = boidParentID;

                // ===== 추가: Fish에게 Boid의 활동 경계 정보 전달 =====
                individualFish.boidSpawnAreaCenter = _boidActivityCenter;
                individualFish.boidSpawnAreaRadius = _boidActivityRadius;

                // ===== 추가: Fish에게 바이옴 경계 정보 전달 =====
                individualFish.biomeWorldMinBounds = biomeMinBounds;
                individualFish.biomeWorldMaxBounds = biomeMaxBounds;

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
        // 각 Fish의 OnDisable에서 FishSimulationManager.Instance.UnregisterFish(this)가 호출되므로
        // Boid에서는 특별히 추가적인 언레지스터 로직이 필요하지 않습니다.
    }

    // Gizmos for Debugging (Boid의 활동 경계 시각화)
    void OnDrawGizmosSelected()
    {
        if (targetFishData == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_boidActivityCenter, _boidActivityRadius);
    }
}

// PoissonDiskSampling2D 클래스는 변경 사항이 없습니다.
// 여기서는 UnityEngine.Random을 사용하고 있으므로, Job에서 호출되지 않아야 합니다.
// (SpawnManager나 Boid와 같은 MonoBehaviour에서만 호출되어야 합니다.)
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
            int spawnIndex = UnityEngine.Random.Range(0, spawnPoints.Count);
            Vector2 spawnCenter = spawnPoints[spawnIndex];
            bool found = false;

            for (int i = 0; i < rejectionSamples; i++)
            {
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