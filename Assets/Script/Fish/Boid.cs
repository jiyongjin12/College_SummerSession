using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    [Header("���� ����")]
    public FishData targetFishData;

    // �� Boid ���� ���� ��� Fish���� Ȱ���� ���� ��� ����
    private Vector2 flockingBoundsCenter;
    private float flockingBoundsRadius; // ���� ����� ������

    // Boid�� SetFlockingBounds�� SpawnManager���� Boid ���� ���� ȣ��� �� �ֽ��ϴ�.
    // ������ Boid ������ Fish ���� ������ �� ���� �ʿ��ϹǷ�, Boid�� Start���� Fish�� �����մϴ�.
    public void SetFlockingBounds(Vector2 center, float radius)
    {
        flockingBoundsCenter = center;
        flockingBoundsRadius = radius;
    }

    void Start()
    {
        if (targetFishData == null)
        {
            Debug.LogError("Boid�� targetFishData�� �Ҵ���� �ʾҽ��ϴ�! ���� �Ұ�.");
            Destroy(gameObject);
            return;
        }

        flockingBoundsCenter = transform.position;
        flockingBoundsRadius = targetFishData.scopeOfActivity;

        SpawnIndividualFish();
    }

    /// <summary>
    /// �� Boid ���� ���ο� ���� Fish���� ���Ƽ� ��ũ ���ø� ������� �����մϴ�.
    /// </summary>
    private void SpawnIndividualFish()
    {
        if (targetFishData.fishPrefab == null)
        {
            Debug.LogError($"FishData '{targetFishData.fishName}'�� fishPrefab�� �Ҵ���� �ʾҽ��ϴ�!");
            return;
        }

        // ���Ƽ� ��ũ ���ø��� ���� �ӽ����� �簢�� �������� ��ȯ
        Vector2 tempRectSize = new Vector2(flockingBoundsRadius * 2, flockingBoundsRadius * 2);
        Vector2 tempRectOffset = flockingBoundsCenter - new Vector2(flockingBoundsRadius, flockingBoundsRadius);

        // ���� Fish �� �ּ� �̰� �Ÿ��� FishData�� �����Ƿ� ������ �� ��� �Ǵ� �߰� �ʿ�
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
            // ���� ����Ʈ�� ���� ���� ���� ���� �ִ��� �ٽ� Ȯ��
            if (Vector2.Distance(point, flockingBoundsCenter) > flockingBoundsRadius)
            {
                continue;
            }

            if (spawnedCount >= targetFishData.fishUnitCount) break;

            Vector3 spawnPosition = new Vector3(point.x, point.y, 0f);

            // FishData�� ����� �������� ����Ͽ� ���� Fish �ν��Ͻ� ����
            GameObject fishObj = Instantiate(targetFishData.fishPrefab, spawnPosition, Quaternion.identity, this.transform);

            Fish individualFish = fishObj.GetComponent<Fish>();
            if (individualFish != null)
            {
                individualFish.fishData = targetFishData; // FishData �Ҵ�
                individualFish.parentBoid = this; // �ڱ� �ڽ� ����
                individualFish.SetFlockingBounds(flockingBoundsCenter, flockingBoundsRadius); // ���� Fish���� ���� ��� ���� ����
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

    // ����׸� ���� Gizmo (���� ��� �ð�ȭ - ����)
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && targetFishData != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, targetFishData.scopeOfActivity);
        }
    }
}

// PoissonDiskSampling2D ��ƿ��Ƽ�� ������ �����մϴ�.
// (���� ����)
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
