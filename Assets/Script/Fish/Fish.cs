using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class Fish : MonoBehaviour
{ // 처리
    public FishData fishData;
    public Boid parentBoid;

    public Biome currentBiome; // 소환된 바이옴 정보

    public bool FlockingSystemONOFF_Test = false;

    // 내부 사용 변수 (FishData에서 가져올 값이 아님)
    private Vector2 acceleration;
    private Vector2 velocity;

    public LayerMask obstacleLayer;

    // Boid로부터 받은 원형 경계 정보 (FishData에 고정된 값이 아님)
    private Vector2 _flockingBoundsCenter;
    private float _flockingBoundsRadius;

    // 이 Fish 개체가 움직일 원형 경계 영역을 설정합니다. (Boid에서 호출)
    public void SetFlockingBounds(Vector2 center, float radius)
    {
        _flockingBoundsCenter = center;
        _flockingBoundsRadius = radius;
    }

    protected virtual void Start()
    {
        // fishData가 없으면 기본값 사용 또는 오류 처리 (필요시)
        float currentMaxSpeed = 0f;

        if (fishData != null)
            currentMaxSpeed = fishData.speed;
        else
            Debug.Log("데이터 확인");

        // 시작 시 랜덤 방향으로 초기 속도 설정
        float angle = Random.Range(0, 2 * Mathf.PI);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * currentMaxSpeed;
        transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
    }

    public void Update()
    {
        // 매 프레임 가속도 초기화
        acceleration = Vector2.zero;

        // 1. 주변 이웃 탐색 (자신과 같은 Boid 아래에 있는 Fish들만 대상으로 함)
        var fishColliders = Physics2D.OverlapCircleAll(transform.position, fishData.flockNeighborhoodRadius); // FishData 값 직접 사용
        var neighboringFish = fishColliders.Select(o => o.GetComponent<Fish>()).Where(f => f != null && f != this && f.transform.parent == this.transform.parent).ToList(); // 같은 자식만 무리로 함
        //var neighboringFish = fishColliders.Select(o => o.GetComponent<Fish>()).Where(f => f != null && f != this && f.fishData.name == this.fishData.name).ToList(); // 이름이 같은 물고기들만 무리로 함

        if (FlockingSystemONOFF_Test)
            Debug.Log("플레이어 발견 후 이동 처리");
        else
        {
            // 2. 군집 규칙 적용
            Flock(neighboringFish);

            // 3. 장애물 회피 적용
            ObstacleAvoidance();

            // 4. 경계 회피 적용 (원형 경계)
            CircularBoundaryAvoidance();

            // 추가. 바이옴 경계 회피 적용 (사각경계)
            RectangleBoundaryAvoidance(); 

            // 5. 속도 및 위치 업데이트
            UpdateVelocity();
            UpdatePosition();

            // 6. 회전 업데이트 (바라보는 방향)
            UpdateRotation();
        }

    }

    // 세 가지 군집 규칙 (정렬, 결집, 분리)을 적용하여 가속도를 계산
    private void Flock(IEnumerable<Fish> fishAgents)
    {
        Vector2 alignmentForce = Alignment(fishAgents);
        Vector2 cohesionForce = Cohesion(fishAgents);
        Vector2 separationForce = Separation(fishAgents);

        // FishData 값 직접 사용
        acceleration += separationForce * fishData.flockSeparationWeight;
        acceleration += cohesionForce * fishData.flockCohesionWeight;
        acceleration += alignmentForce * fishData.flockAlignmentWeight;
    }

    // 장애물과의 충돌을 회피하는 힘을 계산하여 가속도에 추가합니다.
    private void ObstacleAvoidance()
    {
        Vector2 currentForward = velocity.normalized;
        Vector2 leftRayDir = Quaternion.Euler(0, 0, 30) * currentForward;
        Vector2 rightRayDir = Quaternion.Euler(0, 0, -30) * currentForward;

        // FishData 값 직접 사용
        RaycastHit2D frontHit = Physics2D.CircleCast(transform.position, 0.2f, currentForward, fishData.raycastLength, obstacleLayer);
        RaycastHit2D leftHit = Physics2D.CircleCast(transform.position, 0.2f, leftRayDir, fishData.raycastLength, obstacleLayer);
        RaycastHit2D rightHit = Physics2D.CircleCast(transform.position, 0.2f, rightRayDir, fishData.raycastLength, obstacleLayer);

        // 디버그 그리기 (여기도 FishData 값 직접 사용)
        Debug.DrawRay(transform.position, currentForward * fishData.raycastLength, frontHit.collider ? Color.red : Color.white);
        Debug.DrawRay(transform.position, leftRayDir * fishData.raycastLength, leftHit.collider ? Color.red : Color.green);
        Debug.DrawRay(transform.position, rightRayDir * fishData.raycastLength, rightHit.collider ? Color.red : Color.green);

        Vector2 steerForce = Vector2.zero;
        int hitCount = 0;

        if (frontHit.collider)
        {
            hitCount++;
            steerForce += (Vector2)(transform.position - (Vector3)frontHit.point).normalized * 2f;
        }
        if (leftHit.collider)
        {
            hitCount++;
            steerForce += (Vector2)(transform.position - (Vector3)leftHit.point).normalized;
        }
        if (rightHit.collider)
        {
            hitCount++;
            steerForce += (Vector2)(transform.position - (Vector3)rightHit.point).normalized;
        }

        if (hitCount >= 2)
        {
            Vector2 perpendicularDir = new Vector2(-currentForward.y, currentForward.x).normalized;
            if (Random.value < 0.5f) perpendicularDir *= -1;
            steerForce = perpendicularDir;
        }

        if (hitCount > 0)
        {
            // FishData 값 직접 사용
            acceleration += Steer(steerForce.normalized * fishData.speed) * fishData.obstacleAvoidanceWeight;
        }
    }


    // 설정된 원형 경계 밖으로 나가지 않도록 회피하는 힘을 계산하여 가속도에 추가
    private void CircularBoundaryAvoidance()
    {
        if (_flockingBoundsRadius <= 0) return;
        if (fishData == null) return; // fishData가 없으면 실행하지 않음

        float distanceFromCenter = Vector2.Distance(transform.position, _flockingBoundsCenter);

        // FishData 값 직접 사용
        if (distanceFromCenter >= _flockingBoundsRadius - fishData.boundaryMargin)
        {

            Vector2 desiredDirection = (_flockingBoundsCenter - (Vector2)transform.position).normalized;
            // FishData 값 직접 사용
            Vector2 steerForce = Steer(desiredDirection * fishData.speed);

            // 경계에 가까울수록 힘을 강하게 적용 (FishData 값 직접 사용)
            float strength = Mathf.Clamp01((distanceFromCenter - (_flockingBoundsRadius - fishData.boundaryMargin)) / fishData.boundaryMargin);

            // FishData 값 직접 사용
            acceleration += steerForce * fishData.boundsAvoidanceWeight * strength;
        }
    }


    // 바이옴 범위 나가지 않도록 회피
    private void RectangleBoundaryAvoidance()
    {
        if (currentBiome == null || MapManager.Instance == null) return;

        Vector3 currentPos = transform.position;

        // Biome의 center를 MapManager의 위치를 기준으로 월드 좌표로 변환
        Vector3 biomeWorldCenter = MapManager.Instance.transform.position + currentBiome.center;
        Vector3 biomeSize = currentBiome.size; 

        Vector3 minBounds = biomeWorldCenter - biomeSize / 2f;
        Vector3 maxBounds = biomeWorldCenter + biomeSize / 2f;

        Vector2 desiredDirection = Vector2.zero;
        bool outsideBoundary = false;

        // X 경계 밖에 있는지 확인
        if (currentPos.x < minBounds.x)
        {
            desiredDirection += Vector2.right; // 오른쪽으로 이동 유도
            outsideBoundary = true;
        }
        else if (currentPos.x > maxBounds.x)
        {
            desiredDirection += Vector2.left; // 왼쪽으로 이동 유도
            outsideBoundary = true;
        }

        // Y 경계 밖에 있는지 확인
        if (currentPos.y < minBounds.y)
        {
            desiredDirection += Vector2.up; // 위로 이동 유도
            outsideBoundary = true;
        }
        else if (currentPos.y > maxBounds.y)
        {
            desiredDirection += Vector2.down; // 아래로 이동 유도
            outsideBoundary = true;
        }

        if (outsideBoundary)
        {
            desiredDirection.Normalize(); // 방향 정규화
            Vector2 steerForce = Steer(desiredDirection * fishData.speed);

            // 경계 밖에 있을 때, 경계 내부의 가장 가까운 지점 계산
            float closestX = Mathf.Clamp(currentPos.x, minBounds.x, maxBounds.x);
            float closestY = Mathf.Clamp(currentPos.y, minBounds.y, maxBounds.y);
            Vector2 closestPointInBiome = new Vector2(closestX, closestY);

            // 경계 밖으로 벗어난 거리 계산
            float distOutsideBoundary = Vector2.Distance(currentPos, closestPointInBiome);

            // 벗어난 거리에 비례하여 0에서 1 사이 값으로 정규화
            float strength = Mathf.Clamp01(distOutsideBoundary / fishData.boundaryMargin);
            strength = Mathf.Max(strength, 0.1f); // 최소 힘을 보장, 너무 느리게 돌아오지 않도록 함

            acceleration += steerForce * fishData.boundsAvoidanceWeight * strength;
        }
    }

    // 가속도를 속도에 적용하고, 속도 제한을 적용
    protected void UpdateVelocity()
    {
        velocity += acceleration * Time.deltaTime;

        // FishData 값 직접 사용
        float minCurrentSpeed = fishData.speed * 0.4f;
        if (velocity.magnitude < minCurrentSpeed)
        {
            velocity = velocity.normalized * minCurrentSpeed;
        }

        // FishData 값 직접 사용
        velocity = LimitMagnitude(velocity, fishData.speed);
    }

    // 속도를 이용하여 오브젝트의 위치를 업데이트합니다.
    protected void UpdatePosition()
    {
        Vector3 newPosition = transform.position + (Vector3)velocity * Time.deltaTime;
        newPosition.z = 0f;
        transform.position = newPosition;
    }

    // 오브젝트가 현재 속도 방향을 바라보도록 회전
    protected void UpdateRotation()
    {
        if (velocity.sqrMagnitude < 0.001f) return;
        if (fishData == null) return; // fishData가 없으면 실행하지 않음

        float targetAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

        // FishData 값 직접 사용
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, fishData.rotationSpeed * Time.deltaTime);
    }

    // 주변 이웃들의 평균 속도와 동일한 방향으로 움직이려는 힘 (정렬)
    private Vector2 Alignment(IEnumerable<Fish> fishAgents)
    {
        if (!fishAgents.Any()) return Vector2.zero;

        Vector2 averageVelocity = Vector2.zero;
        foreach (var f in fishAgents)
        {
            averageVelocity += f.velocity;
        }
        averageVelocity /= fishAgents.Count();

        // FishData 값 직접 사용
        return Steer(averageVelocity.normalized * fishData.speed);
    }

    // 주변 이웃들의 평균 위치(질량 중심)로 이동하려는 힘 (결집)을 계산합니다.
    private Vector2 Cohesion(IEnumerable<Fish> fishAgents)
    {
        if (!fishAgents.Any()) return Vector2.zero;

        Vector2 centerOfMass = Vector2.zero;
        foreach (var f in fishAgents)
        {
            centerOfMass += (Vector2)f.transform.position;
        }
        centerOfMass /= fishAgents.Count();

        // FishData 값 직접 사용
        return Steer((centerOfMass - (Vector2)transform.position).normalized * fishData.speed);
    }

    // 가까운 이웃들과 충돌하지 않도록 밀어내려는 힘 (분리)
    private Vector2 Separation(IEnumerable<Fish> fishAgents)
    {
        // FishData 값 직접 사용
        var closeFish = fishAgents.Where(f => Vector2.Distance(transform.position, f.transform.position) <= fishData.flockSeparationRadius).ToList();
        if (!closeFish.Any()) return Vector2.zero;

        Vector2 repulsionForce = Vector2.zero;
        foreach (var f in closeFish)
        {
            Vector2 diff = (Vector2)transform.position - (Vector2)f.transform.position;
            repulsionForce += diff.normalized / Mathf.Max(0.001f, diff.magnitude * diff.magnitude);
        }
        repulsionForce /= closeFish.Count;
        // FishData 값 직접 사용
        return Steer(repulsionForce.normalized * fishData.speed);
    }

    // 원하는 속도(desired)로 가기 위해 필요한 가속도(steering force)를 계산
    // 이 힘은 최대 힘(maxForce)으로 제한
    private Vector2 Steer(Vector2 desired)
    {
        // FishData 값 직접 사용
        Vector2 steerForce = desired - velocity;
        return LimitMagnitude(steerForce, fishData.flockMaxForce); // maxForce도 fishData에서 가져옴
    }

    // 벡터 크기를 제한합니다.
    private Vector2 LimitMagnitude(Vector2 vector, float max)
    {
        return vector.sqrMagnitude > max * max ? vector.normalized * max : vector;
    }

    // --- Gizmos for Debugging ---
    protected virtual void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && fishData != null) // fishData가 있을 때만 그리기
        {
            Gizmos.color = Color.magenta;
            // Boid로부터 받은 원형 경계를 그립니다.
            Gizmos.DrawWireSphere(_flockingBoundsCenter, _flockingBoundsRadius);
            // FishData 값 직접 사용
            Gizmos.DrawWireSphere(_flockingBoundsCenter, _flockingBoundsRadius - fishData.boundaryMargin);
        }

        // FishData가 없으면 기본값으로 그리기 (선택 사항)
        float currentNeighborhoodRadius = (fishData != null) ? fishData.flockNeighborhoodRadius : 1.2f;
        float currentSeparationRadius = (fishData != null) ? fishData.flockSeparationRadius : 0.6f;
        float currentRaycastLength = (fishData != null) ? fishData.raycastLength : 1.5f;

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, currentNeighborhoodRadius);
        Gizmos.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
        Gizmos.DrawWireSphere(transform.position, currentSeparationRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)velocity);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)acceleration * 10f);

#if UNITY_EDITOR
        if (Application.isPlaying && fishData != null)
        {
            var fishColliders = Physics2D.OverlapCircleAll(transform.position, fishData.flockNeighborhoodRadius);
            var neighboringFish = fishColliders.Select(o => o.GetComponent<Fish>()).Where(f => f != null && f != this && f.transform.parent == this.transform.parent).ToList();

            // FishData 값 직접 사용
            Vector2 align = Alignment(neighboringFish);
            Vector2 coh = Cohesion(neighboringFish);
            Vector2 sep = Separation(neighboringFish);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)align * fishData.flockAlignmentWeight);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)coh * fishData.flockCohesionWeight);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)sep * fishData.flockSeparationWeight);

            Vector2 currentForward = velocity.normalized;
            RaycastHit2D frontHit = Physics2D.CircleCast(transform.position, 0.2f, currentForward, fishData.raycastLength, obstacleLayer);
            if (frontHit.collider)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)(transform.position - (Vector3)frontHit.point).normalized * fishData.obstacleAvoidanceWeight);
            }
        }
#endif
    }
}
