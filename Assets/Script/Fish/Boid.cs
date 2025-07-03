using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    [Header("군집 설정")]
    public FishData targetFishData;

    // 이 Boid 군집 안의 모든 Fish들이 활동할 원형 경계 정보
    private Vector2 flockingBoundsCenter;
    private float flockingBoundsRadius; // 원형 경계의 반지름

    // Boid의 SetFlockingBounds는 SpawnManager에서 Boid 스폰 직후 호출될 수 있습니다.
    // 하지만 Boid 내에서 Fish 스폰 시점에 이 값이 필요하므로, Boid의 Start에서 Fish를 스폰합니다.
    public void SetFlockingBounds(Vector2 center, float radius)
    {
        flockingBoundsCenter = center;
        flockingBoundsRadius = radius;
    }

    void Start()
    {
        if (targetFishData == null)
        {
            Debug.LogError("Boid에 targetFishData가 할당되지 않았습니다! 스폰 불가.");
            Destroy(gameObject);
            return;
        }

        flockingBoundsCenter = transform.position;
        flockingBoundsRadius = targetFishData.scopeOfActivity;

        SpawnIndividualFish();
    }

    /// <summary>
    /// 이 Boid 군집 내부에 개별 Fish들을 포아송 디스크 샘플링 방식으로 스폰합니다.
    /// </summary>
    private void SpawnIndividualFish()
    {
        if (targetFishData.fishPrefab == null)
        {
            Debug.LogError($"FishData '{targetFishData.fishName}'에 fishPrefab이 할당되지 않았습니다!");
            return;
        }

        // 포아송 디스크 샘플링을 위해 임시적인 사각형 영역으로 변환
        Vector2 tempRectSize = new Vector2(flockingBoundsRadius * 2, flockingBoundsRadius * 2);
        Vector2 tempRectOffset = flockingBoundsCenter - new Vector2(flockingBoundsRadius, flockingBoundsRadius);

        // 개별 Fish 간 최소 이격 거리는 FishData에 없으므로 임의의 값 사용 또는 추가 필요
        float individualMinSeparation = 0.5f;

        List<Vector2> spawnPoints = PoissonDiskSampling2D.GeneratePoints(
            individualMinSeparation,
            tempRectSize,
            rejectionSamples: 30,
            offset: tempRectOffset
        );

        int spawnedCount = 0;
        foreach (Vector2 point in spawnPoints)
        {
            // 스폰 포인트가 실제 원형 범위 내에 있는지 다시 확인
            if (Vector2.Distance(point, flockingBoundsCenter) > flockingBoundsRadius)
            {
                continue;
            }

            if (spawnedCount >= targetFishData.fishUnitCount) break;

            Vector3 spawnPosition = new Vector3(point.x, point.y, 0f);

            // FishData에 연결된 프리팹을 사용하여 개별 Fish 인스턴스 생성
            GameObject fishObj = Instantiate(targetFishData.fishPrefab, spawnPosition, Quaternion.identity, this.transform);

            Fish individualFish = fishObj.GetComponent<Fish>();
            if (individualFish != null)
            {
                individualFish.fishData = targetFishData; // FishData 할당
                individualFish.parentBoid = this; // 자기 자신 참조
                individualFish.SetFlockingBounds(flockingBoundsCenter, flockingBoundsRadius); // 개별 Fish에게 원형 경계 정보 전달
            }
            else
            {
                Debug.LogWarning($"Prefab for {targetFishData.fishName} does not have a Fish component!");
                Destroy(fishObj);
                continue;
            }
            spawnedCount++;
        }

        if (spawnedCount == 0)
        {
            Debug.LogWarning($"Failed to spawn any individual fish for Boid of {targetFishData.fishName}. Check prefab or spawn area. Requested: {targetFishData.fishUnitCount}");
        }
    }

    // 디버그를 위한 Gizmo (군집 경계 시각화 - 원형)
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && targetFishData != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, targetFishData.scopeOfActivity);
        }
    }
}

// PoissonDiskSampling2D 유틸리티는 이전과 동일합니다.
// (변동 없음)
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
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector2 spawnCenter = spawnPoints[spawnIndex];
            bool found = false;

            for (int i = 0; i < rejectionSamples; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                float r = Random.Range(radius, 2 * radius);
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
