using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;
using Unity.Burst;

public class FishSimulationManager : MonoBehaviour
{
    public static FishSimulationManager Instance { get; private set; }

    private List<Fish> allActiveFish = new List<Fish>();

    private NativeArray<FishInputData> fishInputs;
    private NativeArray<FishOutputData> fishOutputs;

    [SerializeField] private Transform playerTransform;

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
    }

    private void DisposeNativeArrays()
    {
        if (fishInputs.IsCreated) fishInputs.Dispose();
        if (fishOutputs.IsCreated) fishOutputs.Dispose();
    }

    void LateUpdate()
    {
        if (allActiveFish.Count == 0 || !fishInputs.IsCreated || fishInputs.Length == 0) return;

        for (int i = 0; i < allActiveFish.Count; i++)
        {
            Fish fish = allActiveFish[i];
            if (fish == null) continue; // null 체크 추가

            FishData fd = fish.fishData;
            if (fd == null) continue; // null 체크 추가

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

                isActingOnPlayer = fish.IsActingOnPlayer,
                isDamagedReacting = fish.IsDamagedReacting,
                isOnActionCooldown = fish.IsOnReDetectionCooldown,
                isDie = fish.isDie,

                // === 변경: 장애물 회피 정보를 _avoidanceDirection 및 _isObstacleAhead로 전달 ===
                hasObstacleHit = fish._isObstacleAhead, // 전방에 장애물 여부
                obstacleAvoidanceDirection = fish._avoidanceDirection, // Fish에서 계산된 회피 방향
                // distanceToObstacle, obstacleHitNormal, obstacleHitPoint는 이제 Job에서 직접 사용되지 않음

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

            if (!fish.IsActingOnPlayer && !fish.IsDamagedReacting && !fish.IsOnReDetectionCooldown)
            {
                fish.currentAcceleration = output.newAcceleration;
            }
        }
    }
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
    public float raycastLength; // 여전히 FishData에서 가져오므로 유지

    public float boundaryMargin;
    public float boundsAvoidanceWeight;

    public float2 flockingBoundsCenter;
    public float flockingBoundsRadius;

    public bool isActingOnPlayer;
    public bool isDie;
    public bool isDamagedReacting;
    public bool isOnActionCooldown;

    // === 변경: 다중 레이캐스트 결과로 얻은 회피 방향 및 플래그 ===
    public bool hasObstacleHit; // _isObstacleAhead
    public float2 obstacleAvoidanceDirection; // _avoidanceDirection (정규화된 회피 방향)

    public int parentID;

    public float2 boidActivityCenter;
    public float boidActivityRadius;

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
    public float2 playerPos;

    public void Execute(int index)
    {
        FishInputData currentFish = fishInputs[index];

        if (currentFish.isDie || currentFish.isActingOnPlayer || currentFish.isDamagedReacting || currentFish.isOnActionCooldown)
        {
            fishOutputs[index] = new FishOutputData
            {
                newAcceleration = float2.zero,
                newVelocity = currentFish.velocity
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
            if (fishInputs[i].parentID != currentFish.parentID) continue;

            float dist = math.distance(currentPosition, fishInputs[i].position);
            if (dist < currentFish.flockNeighborhoodRadius)
            {
                neighboringFish.Add(fishInputs[i]);
            }
        }

        float2 alignmentForce = Alignment(currentVelocity, neighboringFish, currentFish.normalSpeed, currentFish.flockMaxForce);
        float2 cohesionForce = Cohesion(currentPosition, currentVelocity, neighboringFish, currentFish.normalSpeed, currentFish.flockMaxForce);
        float2 separationForce = Separation(currentPosition, currentVelocity, neighboringFish, currentFish.flockSeparationRadius, currentFish.flockMaxForce);

        if (math.isfinite(alignmentForce.x) && math.isfinite(alignmentForce.y))
            acceleration += alignmentForce * currentFish.flockAlignmentWeight;
        if (math.isfinite(cohesionForce.x) && math.isfinite(cohesionForce.y))
            acceleration += cohesionForce * currentFish.flockCohesionWeight;
        if (math.isfinite(separationForce.x) && math.isfinite(separationForce.y))
            acceleration += separationForce * currentFish.flockSeparationWeight;

        neighboringFish.Dispose();

        // === 변경: Fish에서 미리 계산된 회피 방향을 사용 ===
        float2 obstacleAvoidanceForce = AvoidObstacles(currentFish.hasObstacleHit, currentFish.obstacleAvoidanceDirection, currentFish.normalSpeed, currentFish.flockMaxForce);
        if (math.isfinite(obstacleAvoidanceForce.x) && math.isfinite(obstacleAvoidanceForce.y))
            acceleration += obstacleAvoidanceForce * currentFish.obstacleAvoidanceWeight;

        float2 boidBoundsForce = CircularBoundaryAvoidance(currentPosition, currentVelocity, currentFish.boidActivityCenter, currentFish.boidActivityRadius, currentFish.boundaryMargin, currentFish.normalSpeed, currentFish.flockMaxForce);
        if (math.isfinite(boidBoundsForce.x) && math.isfinite(boidBoundsForce.y))
            acceleration += boidBoundsForce * currentFish.boundsAvoidanceWeight;

        float2 biomeBoundsForce = RectangleBoundaryAvoidance(currentPosition, currentVelocity, currentFish.boundaryMargin, currentFish.normalSpeed, currentFish.flockMaxForce, currentFish.biomeWorldMinBounds.x, currentFish.biomeWorldMaxBounds.x, currentFish.biomeWorldMinBounds.y, currentFish.biomeWorldMaxBounds.y);
        if (math.isfinite(biomeBoundsForce.x) && math.isfinite(biomeBoundsForce.y))
            acceleration += biomeBoundsForce * currentFish.boundsAvoidanceWeight;

        if (!math.isfinite(acceleration.x) || !math.isfinite(acceleration.y))
        {
            acceleration = float2.zero;
        }

        fishOutputs[index] = new FishOutputData
        {
            newAcceleration = acceleration,
            newVelocity = currentVelocity
        };
    }

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
                repulsionForce += new float2(1f, 0f);
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

    // === 변경: Fish에서 계산된 회피 방향을 사용하는 AvoidObstacles ===
    private static float2 AvoidObstacles(bool hasObstacleHit, float2 obstacleAvoidanceDirection, float normalSpeed, float flockMaxForce)
    {
        if (hasObstacleHit && math.lengthsq(obstacleAvoidanceDirection) > 0.0001f)
        {
            // Fish에서 이미 가장 안전한 회피 방향을 계산했으므로, 그 방향으로 조향력을 적용합니다.
            // 회피 강도 조절은 더 이상 필요 없을 수 있지만, 필요하다면 FishData에 추가할 수 있습니다.
            return Steer(obstacleAvoidanceDirection * normalSpeed, float2.zero, flockMaxForce);
        }
        return float2.zero;
    }

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

    private static float2 RectangleBoundaryAvoidance(float2 currentPosition, float2 currentVelocity, float boundaryMargin, float normalSpeed, float flockMaxForce,
                                                     float biomeMinX, float biomeMaxX, float biomeMinY, float biomeMaxY)
    {
        float2 desiredDirection = float2.zero;
        bool outsideBoundary = false;

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