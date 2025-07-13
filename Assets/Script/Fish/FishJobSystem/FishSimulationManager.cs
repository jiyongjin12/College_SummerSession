using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;
using Unity.Burst; // BurstCompile 오류 해결을 위해 추가

public class FishSimulationManager : MonoBehaviour
{
    public static FishSimulationManager Instance { get; private set; }

    private List<Fish> allActiveFish = new List<Fish>();

    private NativeArray<FishInputData> fishInputs;
    private NativeArray<FishOutputData> fishOutputs;

    [SerializeField] private Transform playerTransform;

    // BiomeBoundsData는 이제 MapManager에서 직접 가져와 Job으로 넘기지 않고,
    // 각 Fish 인스턴스에 할당된 biomeWorldMinBounds/MaxBounds를 사용합니다.
    // private NativeArray<BiomeBoundsData> biomeBounds;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        UpdateFishListAndNativeArrays();
    }

    void OnDisable()
    {
        DisposeNativeArrays();
    }

    public void RegisterFish(Fish fish)
    {
        if (!allActiveFish.Contains(fish))
        {
            allActiveFish.Add(fish);
            UpdateFishListAndNativeArrays();
        }
    }

    public void UnregisterFish(Fish fish)
    {
        if (allActiveFish.Contains(fish))
        {
            allActiveFish.Remove(fish);
            UpdateFishListAndNativeArrays();
        }
    }

    private void UpdateFishListAndNativeArrays()
    {
        DisposeNativeArrays();

        if (allActiveFish.Count == 0) return;

        fishInputs = new NativeArray<FishInputData>(allActiveFish.Count, Allocator.Persistent);
        fishOutputs = new NativeArray<FishOutputData>(allActiveFish.Count, Allocator.Persistent);

        // BiomeBoundsData NativeArray는 이제 필요 없습니다.
        // if (biomeBounds.IsCreated) biomeBounds.Dispose();
        // if (MapManager.Instance != null && MapManager.Instance.registeredBiomes != null)
        // {
        //     biomeBounds = new NativeArray<BiomeBoundsData>(MapManager.Instance.registeredBiomes.Count, Allocator.Persistent);
        //     for (int i = 0; i < MapManager.Instance.registeredBiomes.Count; i++)
        //     {
        //         Biome biome = MapManager.Instance.registeredBiomes[i];
        //         if (biome != null)
        //         {
        //             Vector3 biomeWorldCenter = MapManager.Instance.transform.position + biome.center;
        //             biomeBounds[i] = new BiomeBoundsData
        //             {
        //                 minBounds = new float3(biomeWorldCenter.x - biome.size.x / 2f, biomeWorldCenter.y - biome.size.y / 2f, biomeWorldCenter.z - biome.size.z / 2f),
        //                 maxBounds = new float3(biomeWorldCenter.x + biomeWorldCenter.x / 2f, biomeWorldCenter.y + biomeWorldCenter.y / 2f, biomeWorldCenter.z + biomeWorldCenter.z / 2f)
        //             };
        //         }
        //     }
        // }
        // else
        // {
        //     biomeBounds = new NativeArray<BiomeBoundsData>(0, Allocator.Persistent);
        // }
    }

    private void DisposeNativeArrays()
    {
        if (fishInputs.IsCreated) fishInputs.Dispose();
        if (fishOutputs.IsCreated) fishOutputs.Dispose();
        // if (biomeBounds.IsCreated) biomeBounds.Dispose(); // 이제 필요 없음
    }

    void LateUpdate()
    {
        if (allActiveFish.Count == 0 || !fishInputs.IsCreated || fishInputs.Length == 0) return;

        for (int i = 0; i < allActiveFish.Count; i++)
        {
            Fish fish = allActiveFish[i];
            if (fish == null) continue;

            FishData fd = fish.fishData;
            if (fd == null) continue;

            if (float.IsNaN(fish.currentVelocity.x) || float.IsNaN(fish.currentVelocity.y))
            {
                Debug.LogError($"Fish {fish.name} has NaN velocity before Job. Resetting.");
                fish.currentVelocity = Vector2.zero;
            }
            if (float.IsNaN(fish.currentAcceleration.x) || float.IsNaN(fish.currentAcceleration.y))
            {
                Debug.LogError($"Fish {fish.name} has NaN acceleration before Job. Resetting.");
                fish.currentAcceleration = Vector2.zero;
            }

            fishInputs[i] = new FishInputData
            {
                position = new float2(fish.transform.position.x, fish.transform.position.y),
                velocity = (float2)fish.currentVelocity,

                normalSpeed = fd.normalSpeed,
                flockMaxForce = fd.flockMaxForce,
                flockNeighborhoodRadius = fd.flockNeighborhoodRadius,
                flockSeparationRadius = fd.flockSeparationRadius,
                flockSeparationWeight = fd.flockSeparationWeight,
                flockCohesionWeight = fd.flockCohesionWeight,
                flockAlignmentWeight = fd.flockAlignmentWeight,

                obstacleAvoidanceWeight = fd.obstacleAvoidanceWeight,
                raycastLength = fd.raycastLength,

                boundaryMargin = fd.boundaryMargin,
                boundsAvoidanceWeight = fd.boundsAvoidanceWeight,

                flockingBoundsCenter = new float2(fish.GetFlockingBoundsCenter().x, fish.GetFlockingBoundsCenter().y),
                flockingBoundsRadius = fish.GetFlockingBoundsRadius(),

                // ===== 변경 부분 1: fish.IsOnActionCooldown -> fish.IsOnReDetectionCooldown =====
                isActingOnPlayer = fish.IsActingOnPlayer,
                isDamagedReacting = fish.IsDamagedReacting,
                isOnActionCooldown = fish.IsOnReDetectionCooldown, // <--- 여기!
                isDie = fish.isDie,

                hasObstacleHit = fish._raycastHitData.collider != null,
                obstacleHitNormal = fish._raycastHitData.collider != null ? new float2(fish._raycastHitData.normal.x, fish._raycastHitData.normal.y) : float2.zero,
                obstacleHitPoint = fish._raycastHitData.collider != null ? new float2(fish._raycastHitData.point.x, fish._raycastHitData.point.y) : float2.zero,
                distanceToObstacle = fish._raycastHitData.collider != null ? fish._raycastHitData.distance : fish.fishData.raycastLength,
                parentID = fish.parentID,

                boidActivityCenter = fish.boidSpawnAreaCenter,
                boidActivityRadius = fish.boidSpawnAreaRadius,

                biomeWorldMinBounds = fish.biomeWorldMinBounds,
                biomeWorldMaxBounds = fish.biomeWorldMaxBounds
            };
        }

        FishSimulationJob simulationJob = new FishSimulationJob
        {
            fishInputs = fishInputs,
            fishOutputs = fishOutputs,
            deltaTime = Time.deltaTime,
            playerPos = new float2(playerTransform != null ? playerTransform.position.x : 0f, playerTransform != null ? playerTransform.position.y : 0f),
        };

        JobHandle handle = simulationJob.Schedule(allActiveFish.Count, 64);

        handle.Complete();

        for (int i = 0; i < allActiveFish.Count; i++)
        {
            Fish fish = allActiveFish[i];
            if (fish == null || fish.isDie) continue;

            FishOutputData output = fishOutputs[i];

            // ===== 변경 부분 2: !fish.IsOnActionCooldown -> !fish.IsOnReDetectionCooldown =====
            if (!fish.IsActingOnPlayer && !fish.IsDamagedReacting && !fish.IsOnReDetectionCooldown) // <--- 여기!
            {
                fish.currentAcceleration = output.newAcceleration;
            }
        }
    }
}

public struct BiomeBoundsData
{
    public float3 minBounds;
    public float3 maxBounds;
}

public struct FishInputData
{
    public float2 position;
    public float2 velocity;

    public float normalSpeed;
    public float flockMaxForce;
    public float flockNeighborhoodRadius;
    public float flockSeparationRadius;
    public float flockSeparationWeight;
    public float flockCohesionWeight;
    public float flockAlignmentWeight;

    public float obstacleAvoidanceWeight;
    public float raycastLength;

    public float boundaryMargin;
    public float boundsAvoidanceWeight;

    public float2 flockingBoundsCenter; // 개별 물고기의 군집 경계 중심
    public float flockingBoundsRadius;   // 개별 물고기의 군집 경계 반경

    public bool isActingOnPlayer;
    public bool isDie;
    public bool isDamagedReacting; // 추가: 피격 반응 중인지
    public bool isOnActionCooldown; // 추가: 쿨다운 중인지

    // Raycast 결과
    public bool hasObstacleHit;
    public float2 obstacleHitNormal;
    public float2 obstacleHitPoint;
    public float distanceToObstacle;

    public int parentID;

    // ===== 추가: Boid의 활동 경계 정보 =====
    public float2 boidActivityCenter;
    public float boidActivityRadius;

    // ===== 추가: 바이옴 경계 정보 =====
    public float2 biomeWorldMinBounds;
    public float2 biomeWorldMaxBounds;
}

public struct FishOutputData
{
    public float2 newAcceleration;
    public float2 newVelocity;
}

[BurstCompile]
public struct FishSimulationJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<FishInputData> fishInputs;
    [WriteOnly] public NativeArray<FishOutputData> fishOutputs;

    public float deltaTime;
    public float2 playerPos; // 현재 사용하지 않지만, Job 내에서 플레이어 관련 로직이 있다면 사용 가능
    // biomeBounds NativeArray는 더 이상 필요 없음
    // MapManager의 전체 맵 경계도 더 이상 Job에 직접 전달할 필요 없음

    public void Execute(int index)
    {
        FishInputData currentFish = fishInputs[index];

        // ===== 변경: 플레이어와 상호작용 중이거나, 피격 반응 중이거나, 쿨다운 중이거나, 죽었으면 군집/경계 계산 스킵 =====
        if (currentFish.isDie || currentFish.isActingOnPlayer || currentFish.isDamagedReacting || currentFish.isOnActionCooldown)
        {
            fishOutputs[index] = new FishOutputData
            {
                newAcceleration = float2.zero, // Job에서는 가속도를 0으로 설정하여 MonoBehaviour 로직이 우선하도록 함
                newVelocity = currentFish.velocity // 기존 속도 유지 (MonoBehaviour에서 업데이트될 것임)
            };
            return;
        }

        float2 acceleration = float2.zero;
        float2 currentVelocity = currentFish.velocity;
        float2 currentPosition = currentFish.position;

        NativeList<FishInputData> neighboringFish = new NativeList<FishInputData>(Allocator.Temp);
        for (int i = 0; i < fishInputs.Length; i++)
        {
            if (i == index || fishInputs[i].isDie || fishInputs[i].isActingOnPlayer || fishInputs[i].isDamagedReacting || fishInputs[i].isOnActionCooldown) continue;
            if (fishInputs[i].parentID != currentFish.parentID) continue; // 같은 부모 ID를 가진 물고기만 군집에 포함

            float dist = math.distance(currentPosition, fishInputs[i].position);
            if (dist < currentFish.flockNeighborhoodRadius)
            {
                neighboringFish.Add(fishInputs[i]);
            }
        }

        // 군집 행동 (정렬, 결합, 분리)
        float2 alignmentForce = Alignment(currentVelocity, neighboringFish, currentFish.normalSpeed, currentFish.flockMaxForce);
        float2 cohesionForce = Cohesion(currentPosition, currentVelocity, neighboringFish, currentFish.normalSpeed, currentFish.flockMaxForce);
        float2 separationForce = Separation(currentPosition, currentVelocity, neighboringFish, currentFish.flockSeparationRadius, currentFish.flockMaxForce);

        if (math.isfinite(alignmentForce.x) && math.isfinite(alignmentForce.y))
            acceleration += alignmentForce * currentFish.flockAlignmentWeight;
        // else Debug.LogError($"Alignment force is NaN/Infinity for fish {index}"); // 이제 Job에서는 Debug.Log를 피하는 것이 좋습니다.

        if (math.isfinite(cohesionForce.x) && math.isfinite(cohesionForce.y))
            acceleration += cohesionForce * currentFish.flockCohesionWeight;
        // else Debug.LogError($"Cohesion force is NaN/Infinity for fish {index}");

        if (math.isfinite(separationForce.x) && math.isfinite(separationForce.y))
            acceleration += separationForce * currentFish.flockSeparationWeight;
        // else Debug.LogError($"Separation force is NaN/Infinity for fish {index}");

        neighboringFish.Dispose();

        // 장애물 회피
        float2 obstacleAvoidanceForce = AvoidObstacles(currentFish.hasObstacleHit, currentFish.obstacleHitNormal, currentFish.distanceToObstacle, currentFish.raycastLength, currentFish.normalSpeed, currentFish.flockMaxForce);
        if (math.isfinite(obstacleAvoidanceForce.x) && math.isfinite(obstacleAvoidanceForce.y))
            acceleration += obstacleAvoidanceForce * currentFish.obstacleAvoidanceWeight;
        // else Debug.LogError($"Obstacle avoidance force is NaN/Infinity for fish {index}");

        // ===== 추가: Boid 활동 경계 회피 =====
        float2 boidBoundsForce = CircularBoundaryAvoidance(currentPosition, currentVelocity, currentFish.boidActivityCenter, currentFish.boidActivityRadius, currentFish.boundaryMargin, currentFish.normalSpeed, currentFish.flockMaxForce);
        if (math.isfinite(boidBoundsForce.x) && math.isfinite(boidBoundsForce.y))
            acceleration += boidBoundsForce * currentFish.boundsAvoidanceWeight;
        // else Debug.LogError($"Boid bounds force is NaN/Infinity for fish {index}");

        // ===== 추가: 바이옴 경계 회피 =====
        float2 biomeBoundsForce = RectangleBoundaryAvoidance(currentPosition, currentVelocity, currentFish.boundaryMargin, currentFish.normalSpeed, currentFish.flockMaxForce, currentFish.biomeWorldMinBounds.x, currentFish.biomeWorldMaxBounds.x, currentFish.biomeWorldMinBounds.y, currentFish.biomeWorldMaxBounds.y);
        if (math.isfinite(biomeBoundsForce.x) && math.isfinite(biomeBoundsForce.y))
            acceleration += biomeBoundsForce * currentFish.boundsAvoidanceWeight;
        // else Debug.LogError($"Biome bounds force is NaN/Infinity for fish {index}");


        // 최종 가속도 유효성 검사 (NaN/Infinity 방지)
        if (!math.isfinite(acceleration.x) || !math.isfinite(acceleration.y))
        {
            acceleration = float2.zero;
        }

        // Job에서는 가속도만 계산하여 반환하고, 속도 업데이트는 MonoBehaviour에서 책임지도록 변경
        // 이렇게 하면 MonoBehaviour의 플레이어 관련 로직이 속도에 더 직접적인 영향을 줄 수 있습니다.
        // newVelocity는 Job 내에서 계산되지만, 실제 Fish의 currentVelocity에 바로 적용되지 않습니다.
        // Fish.UpdateVelocity()에서 이 acceleration을 사용하여 currentVelocity를 업데이트합니다.
        // float2 newVelocity = currentVelocity + acceleration * deltaTime; // 이 계산은 Job 내에서 더 이상 직접 Fish.currentVelocity에 반영되지 않습니다.
        // newVelocity = LimitMagnitude(newVelocity, currentFish.normalSpeed);

        fishOutputs[index] = new FishOutputData
        {
            newAcceleration = acceleration,
            newVelocity = currentVelocity // Job에서는 currentVelocity를 직접 변경하지 않으므로 기존 값을 반환
        };
    }

    // 기존 헬퍼 함수들은 변경 없음 (이미 NaN 방지 로직 포함)
    private static float2 LimitMagnitude(float2 vector, float max)
    {
        float sqrMag = math.lengthsq(vector);
        if (sqrMag > max * max)
        {
            if (sqrMag < 0.0001f)
            {
                return float2.zero;
            }
            return math.normalize(vector) * max;
        }
        return vector;
    }

    private static float2 Steer(float2 desired, float2 currentVelocity, float flockMaxForce)
    {
        float2 steerForce = desired - currentVelocity;
        return LimitMagnitude(steerForce, flockMaxForce);
    }

    private static float2 Alignment(float2 currentVelocity, NativeList<FishInputData> neighboringFish, float normalSpeed, float flockMaxForce)
    {
        if (neighboringFish.Length == 0) return float2.zero;
        float2 averageVelocity = float2.zero;
        for (int i = 0; i < neighboringFish.Length; i++)
        {
            averageVelocity += neighboringFish[i].velocity;
        }
        averageVelocity /= (float)(neighboringFish.Length);

        if (math.lengthsq(averageVelocity) < 0.0001f)
        {
            return float2.zero;
        }
        return Steer(math.normalize(averageVelocity) * normalSpeed, currentVelocity, flockMaxForce);
    }

    private static float2 Cohesion(float2 currentPosition, float2 currentVelocity, NativeList<FishInputData> neighboringFish, float normalSpeed, float flockMaxForce)
    {
        if (neighboringFish.Length == 0) return float2.zero;
        float2 centerOfMass = float2.zero;
        for (int i = 0; i < neighboringFish.Length; i++)
        {
            centerOfMass += neighboringFish[i].position;
        }
        centerOfMass /= (float)neighboringFish.Length;

        float2 directionToCM = centerOfMass - currentPosition;
        if (math.lengthsq(directionToCM) < 0.0001f)
        {
            return float2.zero;
        }
        return Steer(math.normalize(directionToCM) * normalSpeed, currentVelocity, flockMaxForce);
    }

    private static float2 Separation(float2 currentPosition, float2 currentVelocity, NativeList<FishInputData> neighboringFish, float flockSeparationRadius, float flockMaxForce)
    {
        NativeList<FishInputData> closeFish = new NativeList<FishInputData>(Allocator.Temp);
        for (int i = 0; i < neighboringFish.Length; i++)
        {
            float dist = math.distance(currentPosition, neighboringFish[i].position);
            if (dist > 0 && dist <= flockSeparationRadius)
            {
                closeFish.Add(neighboringFish[i]);
            }
        }

        if (closeFish.Length == 0)
        {
            closeFish.Dispose();
            return float2.zero;
        }

        float2 repulsionForce = float2.zero;
        for (int i = 0; i < closeFish.Length; i++)
        {
            float2 diff = currentPosition - closeFish[i].position;
            float distSq = math.lengthsq(diff);
            if (distSq < 0.0001f)
            {
                repulsionForce += new float2(1f, 0f); // Fallback to a fixed direction
                continue;
            }
            repulsionForce += math.normalize(diff) / distSq;
        }
        repulsionForce /= closeFish.Length;
        closeFish.Dispose();

        if (math.lengthsq(repulsionForce) < 0.0001f)
        {
            return float2.zero;
        }
        return Steer(math.normalize(repulsionForce) * flockMaxForce, currentVelocity, flockMaxForce);
    }

    private static float2 AvoidObstacles(bool hasObstacleHit, float2 obstacleNormal, float distanceToObstacle, float raycastLength, float normalSpeed, float flockMaxForce)
    {
        if (hasObstacleHit)
        {
            float effectiveDistance = math.max(0.001f, distanceToObstacle);
            float avoidanceStrength = 1f - (effectiveDistance / raycastLength);
            avoidanceStrength = math.clamp(avoidanceStrength, 0f, 1f);

            float2 desiredDirection = obstacleNormal;
            return Steer(desiredDirection * normalSpeed * avoidanceStrength, float2.zero, flockMaxForce);
        }
        return float2.zero;
    }

    // ===== 추가: Boid 활동 경계 회피 (CircularBoundaryAvoidance 재활용) =====
    private static float2 CircularBoundaryAvoidance(float2 currentPosition, float2 currentVelocity, float2 center, float radius, float margin, float normalSpeed, float flockMaxForce)
    {
        if (radius <= 0) return float2.zero;

        float distanceFromCenter = math.distance(currentPosition, center);

        if (distanceFromCenter >= radius - margin)
        {
            float2 desiredDirection = math.normalize(center - currentPosition);
            float2 steerForce = Steer(desiredDirection * normalSpeed, currentVelocity, flockMaxForce);

            float strength = math.clamp((distanceFromCenter - (radius - margin)) / margin, 0f, 1f);
            return steerForce * strength;
        }
        return float2.zero;
    }

    // ===== 추가: 바이옴 경계 회피 (RectangleBoundaryAvoidance 재활용) =====
    private static float2 RectangleBoundaryAvoidance(float2 currentPosition, float2 currentVelocity, float boundaryMargin, float normalSpeed, float flockMaxForce,
                                                     float biomeMinX, float biomeMaxX, float biomeMinY, float biomeMaxY)
    {
        float2 desiredDirection = float2.zero;
        bool outsideBoundary = false;

        // X축 경계
        if (currentPosition.x < biomeMinX + boundaryMargin)
        {
            desiredDirection += new float2(1, 0);
            outsideBoundary = true;
        }
        else if (currentPosition.x > biomeMaxX - boundaryMargin)
        {
            desiredDirection += new float2(-1, 0);
            outsideBoundary = true;
        }

        // Y축 경계
        if (currentPosition.y < biomeMinY + boundaryMargin)
        {
            desiredDirection += new float2(0, 1);
            outsideBoundary = true;
        }
        else if (currentPosition.y > biomeMaxY - boundaryMargin)
        {
            desiredDirection += new float2(0, -1);
            outsideBoundary = true;
        }

        if (outsideBoundary)
        {
            desiredDirection = math.normalize(desiredDirection);
            return Steer(desiredDirection * normalSpeed, currentVelocity, flockMaxForce);
        }
        return float2.zero;
    }
}