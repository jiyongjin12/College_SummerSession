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
    // LayerMask는 현재 Job에서 직접 사용되지 않으므로 제거하거나 다른 방식으로 활용합니다.
    // [SerializeField] private LayerMask obstacleLayer; 

    private NativeArray<BiomeBoundsData> biomeBounds;

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

        if (MapManager.Instance != null && MapManager.Instance.registeredBiomes != null)
        {
            biomeBounds = new NativeArray<BiomeBoundsData>(MapManager.Instance.registeredBiomes.Count, Allocator.Persistent);
            for (int i = 0; i < MapManager.Instance.registeredBiomes.Count; i++)
            {
                Biome biome = MapManager.Instance.registeredBiomes[i];
                if (biome != null)
                {
                    Vector3 biomeWorldCenter = MapManager.Instance.transform.position + biome.center;
                    biomeBounds[i] = new BiomeBoundsData
                    {
                        // Vector3를 float3로 변환할 때 X, Y, Z 모두 명시적으로 사용
                        minBounds = new float3(biomeWorldCenter.x - biome.size.x / 2f, biomeWorldCenter.y - biome.size.y / 2f, biomeWorldCenter.z - biome.size.z / 2f),
                        maxBounds = new float3(biomeWorldCenter.x + biome.size.x / 2f, biomeWorldCenter.y + biome.size.y / 2f, biomeWorldCenter.z + biome.size.z / 2f)
                    };
                }
            }
        }
        else
        {
            biomeBounds = new NativeArray<BiomeBoundsData>(0, Allocator.Persistent);
        }
    }

    private void DisposeNativeArrays()
    {
        if (fishInputs.IsCreated) fishInputs.Dispose();
        if (fishOutputs.IsCreated) fishOutputs.Dispose();
        if (biomeBounds.IsCreated) biomeBounds.Dispose();
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

            fishInputs[i] = new FishInputData
            {
                position = new float2(fish.transform.position.x, fish.transform.position.y), // X, Y만 추출하여 float2 생성
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

                flockingBoundsCenter = new float2(fish.GetFlockingBoundsCenter().x, fish.GetFlockingBoundsCenter().y), // X, Y만 추출하여 float2 생성
                flockingBoundsRadius = fish.GetFlockingBoundsRadius(),

                isActingOnPlayer = fish.IsActingOnPlayer || fish.IsDamagedReacting || fish.IsOnActionCooldown,
                isDie = fish.isDie
            };
        }

        FishSimulationJob simulationJob = new FishSimulationJob
        {
            fishInputs = fishInputs,
            fishOutputs = fishOutputs,
            deltaTime = Time.deltaTime,
            playerPos = new float2(playerTransform != null ? playerTransform.position.x : 0f, playerTransform != null ? playerTransform.position.y : 0f), // X, Y만 추출하여 float2 생성
            biomeBounds = biomeBounds,
            mapManagerWorldCenterY = MapManager.Instance.transform.position.y,
            mapManagerMapSizeY = MapManager.Instance.mapSize.y,
            mapManagerWorldMinX = MapManager.Instance.transform.position.x - MapManager.Instance.mapSize.x / 2f,
            mapManagerWorldMaxX = MapManager.Instance.transform.position.x + MapManager.Instance.mapSize.x / 2f
        };

        JobHandle handle = simulationJob.Schedule(allActiveFish.Count, 64);

        handle.Complete();

        for (int i = 0; i < allActiveFish.Count; i++)
        {
            Fish fish = allActiveFish[i];
            if (fish == null || fish.isDie) continue;

            FishOutputData output = fishOutputs[i];

            if (!fish.IsActingOnPlayer && !fish.IsDamagedReacting && !fish.IsOnActionCooldown)
            {
                fish.currentAcceleration = output.newAcceleration;
                fish.currentVelocity = output.newVelocity;
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

    public float2 flockingBoundsCenter;
    public float flockingBoundsRadius;

    public bool isActingOnPlayer;
    public bool isDie;
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
    [ReadOnly] public NativeArray<BiomeBoundsData> biomeBounds;

    public float mapManagerWorldCenterY;
    public float mapManagerMapSizeY;
    public float mapManagerWorldMinX;
    public float mapManagerWorldMaxX;


    public void Execute(int index)
    {
        FishInputData currentFish = fishInputs[index];

        if (currentFish.isDie || currentFish.isActingOnPlayer)
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
            if (i == index || fishInputs[i].isDie || fishInputs[i].isActingOnPlayer) continue;

            float dist = math.distance(currentPosition, fishInputs[i].position);
            if (dist < currentFish.flockNeighborhoodRadius)
            {
                neighboringFish.Add(fishInputs[i]);
            }
        }

        float2 alignmentForce = Alignment(currentVelocity, neighboringFish, currentFish.normalSpeed, currentFish.flockMaxForce);
        float2 cohesionForce = Cohesion(currentPosition, currentVelocity, neighboringFish, currentFish.normalSpeed, currentFish.flockMaxForce);
        float2 separationForce = Separation(currentPosition, currentVelocity, neighboringFish, currentFish.flockSeparationRadius, currentFish.flockMaxForce);

        acceleration += separationForce * currentFish.flockSeparationWeight;
        acceleration += cohesionForce * currentFish.flockCohesionWeight;
        acceleration += alignmentForce * currentFish.flockAlignmentWeight;

        neighboringFish.Dispose();

        float2 obstacleAvoidanceForce = AvoidObstacles(currentPosition, currentVelocity, currentFish.raycastLength, currentFish.normalSpeed, currentFish.flockMaxForce);
        acceleration += obstacleAvoidanceForce * currentFish.obstacleAvoidanceWeight;


        float2 circularBoundsForce = CircularBoundaryAvoidance(currentPosition, currentVelocity, currentFish.flockingBoundsCenter, currentFish.flockingBoundsRadius, currentFish.boundaryMargin, currentFish.normalSpeed, currentFish.flockMaxForce);
        acceleration += circularBoundsForce * currentFish.boundsAvoidanceWeight;

        float2 rectangleBoundsForce = RectangleBoundaryAvoidance(currentPosition, currentVelocity, currentFish.boundaryMargin, currentFish.normalSpeed, currentFish.flockMaxForce,
                                                                 mapManagerWorldMinX, mapManagerWorldMaxX,
                                                                 mapManagerWorldCenterY - mapManagerMapSizeY / 2f,
                                                                 mapManagerWorldCenterY + mapManagerMapSizeY / 2f);
        acceleration += rectangleBoundsForce * currentFish.boundsAvoidanceWeight;


        currentVelocity += acceleration * deltaTime;
        currentVelocity = LimitMagnitude(currentVelocity, currentFish.normalSpeed);


        fishOutputs[index] = new FishOutputData
        {
            newAcceleration = acceleration,
            newVelocity = currentVelocity
        };
    }

    private static float2 Steer(float2 desired, float2 currentVelocity, float flockMaxForce)
    {
        float2 steerForce = desired - currentVelocity;
        return LimitMagnitude(steerForce, flockMaxForce);
    }

    private static float2 LimitMagnitude(float2 vector, float max)
    {
        return math.lengthsq(vector) > max * max ? math.normalize(vector) * max : vector;
    }

    private static float2 Alignment(float2 currentVelocity, NativeList<FishInputData> neighboringFish, float normalSpeed, float flockMaxForce)
    {
        if (neighboringFish.Length == 0) return float2.zero;
        float2 averageVelocity = float2.zero;
        for (int i = 0; i < neighboringFish.Length; i++)
        {
            averageVelocity += neighboringFish[i].velocity;
        }
        averageVelocity += currentVelocity;
        averageVelocity /= (neighboringFish.Length + 1);
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
        centerOfMass /= neighboringFish.Length;
        return Steer(math.normalize(centerOfMass - currentPosition) * normalSpeed, currentVelocity, flockMaxForce);
    }

    private static float2 Separation(float2 currentPosition, float2 currentVelocity, NativeList<FishInputData> neighboringFish, float flockSeparationRadius, float flockMaxForce)
    {
        NativeList<FishInputData> closeFish = new NativeList<FishInputData>(Allocator.Temp);
        for (int i = 0; i < neighboringFish.Length; i++)
        {
            if (math.distance(currentPosition, neighboringFish[i].position) <= flockSeparationRadius)
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
            repulsionForce += math.normalize(diff) / math.max(0.001f, math.lengthsq(diff));
        }
        repulsionForce /= closeFish.Length;
        closeFish.Dispose();
        return Steer(math.normalize(repulsionForce) * flockMaxForce, currentVelocity, flockMaxForce);
    }

    private static float2 AvoidObstacles(float2 currentPosition, float2 currentVelocity, float raycastLength, float normalSpeed, float flockMaxForce)
    {
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
                                                     float mapWorldMinX, float mapWorldMaxX, float mapWorldMinY, float mapWorldMaxY)
    {
        float2 desiredDirection = float2.zero;
        bool outsideBoundary = false;

        if (currentPosition.x < mapWorldMinX + boundaryMargin)
        {
            desiredDirection += new float2(1, 0);
            outsideBoundary = true;
        }
        else if (currentPosition.x > mapWorldMaxX - boundaryMargin)
        {
            desiredDirection += new float2(-1, 0);
            outsideBoundary = true;
        }

        if (currentPosition.y < mapWorldMinY + boundaryMargin)
        {
            desiredDirection += new float2(0, 1);
            outsideBoundary = true;
        }
        else if (currentPosition.y > mapWorldMaxY - boundaryMargin)
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