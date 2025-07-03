using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Flocking_Test : MonoBehaviour
{
    // 필드 정리 및 변수명 개선
    public float maxSpeed = 3f; // 최대 속도
    public float maxForce = 0.01f; // 힘 적용의 최대치
    public float neighborhoodRadius = 1.2f; // 주변 이웃 탐색 반경
    public float separationRadius = 0.6f; // 분리 (충돌 회피)를 위한 가까운 이웃 반경 (neighborhoodRadius의 절반)

    [Range(0f, 2f)] public float separationWeight = 1.5f; // 분리 힘의 가중치
    [Range(0f, 2f)] public float cohesionWeight = 1f;    // 결집 힘의 가중치
    [Range(0f, 2f)] public float alignmentWeight = 1f;   // 정렬 힘의 가중치

    [Header("회피 설정")]
    public LayerMask obstacleLayer; // 장애물 레이어 (인스펙터에서 설정)
    public float obstacleAvoidanceWeight = 2f; // 장애물 회피 힘의 가중치
    public float raycastLength = 1.5f; // 장애물 감지 Raycast 길이
    public float rotationSpeed = 360f; // 초당 회전 각도

    [Header("경계 설정")]
    public float boundsAvoidanceWeight = 2f; // 경계 회피 힘의 가중치
    public float boundaryMargin = 3f; // 경계로부터 이만큼 떨어져 있을 때부터 회피 시작

    // 내부 사용 변수 (private)
    private Vector2 acceleration;
    private Vector2 velocity;
    private Vector2 flockingBoundsCenter;
    private Vector2 flockingBoundsSize;

    // 이 플로킹 에이전트가 움직일 경계 영역을 설정합니다.
    public void SetBounds(Vector2 center, Vector2 size)
    {
        flockingBoundsCenter = center;
        flockingBoundsSize = size;
    }

    private void Start()
    {
        // 시작 시 랜덤 방향으로 초기 속도 설정
        float angle = Random.Range(0, 2 * Mathf.PI);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * maxSpeed; // 초기 속도도 maxSpeed를 따르도록
        transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg); // Z축 회전만 적용
    }

    private void Update()
    {
        // 매 프레임 가속도 초기화
        acceleration = Vector2.zero;

        // 1. 주변 이웃 탐색
        var boidColliders = Physics2D.OverlapCircleAll(transform.position, neighborhoodRadius);
        var boids = boidColliders
                    .Select(o => o.GetComponent<Flocking_Test>())
                    .Where(b => b != null && b != this)
                    .ToList();

        // 2. 군집 규칙 적용
        Flock(boids);

        // 3. 장애물 회피 적용
        ObstacleAvoidance();

        // 4. 경계 회피 적용
        BoundaryAvoidance();

        // 5. 속도 및 위치 업데이트
        UpdateVelocity();
        UpdatePosition();

        // 6. 회전 업데이트 (바라보는 방향)
        UpdateRotation();
    }

    // 세 가지 군집 규칙 (정렬, 결집, 분리)을 적용하여 가속도를 계산합니다.
    private void Flock(IEnumerable<Flocking_Test> boids)
    {
        Vector2 alignmentForce = Alignment(boids);
        Vector2 cohesionForce = Cohesion(boids);
        Vector2 separationForce = Separation(boids);

        // 각 힘에 가중치를 곱하여 가속도에 합산
        acceleration += separationForce * separationWeight;
        acceleration += cohesionForce * cohesionWeight;
        acceleration += alignmentForce * alignmentWeight;
    }

    // 장애물과의 충돌을 회피하는 힘을 계산하여 가속도에 추가합니다.
    private void ObstacleAvoidance()
    {
        Vector2 currentForward = velocity.normalized;
        // 정면, 좌측 30도, 우측 30도 방향으로 Raycast 발사
        Vector2 leftRayDir = Quaternion.Euler(0, 0, 30) * currentForward;
        Vector2 rightRayDir = Quaternion.Euler(0, 0, -30) * currentForward;

        RaycastHit2D frontHit = Physics2D.CircleCast(transform.position, 0.2f, currentForward, raycastLength, obstacleLayer);
        RaycastHit2D leftHit = Physics2D.CircleCast(transform.position, 0.2f, leftRayDir, raycastLength, obstacleLayer);
        RaycastHit2D rightHit = Physics2D.CircleCast(transform.position, 0.2f, rightRayDir, raycastLength, obstacleLayer);

        // Gizmo (Scene 뷰에서 디버깅용으로 Raycast 시각화)
        Debug.DrawRay(transform.position, currentForward * raycastLength, frontHit.collider ? Color.red : Color.white);
        Debug.DrawRay(transform.position, leftRayDir * raycastLength, leftHit.collider ? Color.red : Color.green);
        Debug.DrawRay(transform.position, rightRayDir * raycastLength, rightHit.collider ? Color.red : Color.green);

        Vector2 steerForce = Vector2.zero;
        int hitCount = 0;

        // 충돌 감지 시 회피 방향 계산
        if (frontHit.collider)
        {
            hitCount++;
            // 정면 충돌 시 강하게 밀어내기 (충돌 지점으로부터 반대 방향)
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

        // 좌/우/전면 모두 감지되어 갇혔을 때, 랜덤하게 수직 방향으로 회피
        if (hitCount >= 2)
        {
            // 현재 진행 방향의 수직 방향 중 하나를 선택
            Vector2 perpendicularDir = new Vector2(-currentForward.y, currentForward.x).normalized;
            if (Random.value < 0.5f) perpendicularDir *= -1; // 반대 수직 방향도 고려
            steerForce = perpendicularDir; // 수직 방향으로 강하게 피함
        }

        if (hitCount > 0)
        {
            acceleration += Steer(steerForce.normalized * maxSpeed) * obstacleAvoidanceWeight;
        }
    }

    // 설정된 경계 영역 밖으로 나가지 않도록 회피하는 힘을 계산하여 가속도에 추가합니다.
    private void BoundaryAvoidance()
    {
        Vector2 minBounds = flockingBoundsCenter - flockingBoundsSize / 2f;
        Vector2 maxBounds = flockingBoundsCenter + flockingBoundsSize / 2f;
        Vector2 desiredVelocity = Vector2.zero;

        // X축 경계 체크
        if (transform.position.x < minBounds.x + boundaryMargin)
        {
            desiredVelocity.x = maxSpeed; // 오른쪽으로 이동
        }
        else if (transform.position.x > maxBounds.x - boundaryMargin)
        {
            desiredVelocity.x = -maxSpeed; // 왼쪽으로 이동
        }

        // Y축 경계 체크
        if (transform.position.y < minBounds.y + boundaryMargin)
        {
            desiredVelocity.y = maxSpeed; // 위쪽으로 이동
        }
        else if (transform.position.y > maxBounds.y - boundaryMargin)
        {
            desiredVelocity.y = -maxSpeed; // 아래쪽으로 이동
        }

        if (desiredVelocity != Vector2.zero)
        {
            Vector2 steerForce = Steer(desiredVelocity);

            // 경계에 가까울수록 힘을 강하게 적용
            float distanceToClosestBoundary = Mathf.Min(
                Mathf.Abs(transform.position.x - minBounds.x),
                Mathf.Abs(transform.position.x - maxBounds.x),
                Mathf.Abs(transform.position.y - minBounds.y),
                Mathf.Abs(transform.position.y - maxBounds.y)
            );
            float strength = 1f - Mathf.Clamp01(distanceToClosestBoundary / boundaryMargin);

            acceleration += steerForce * boundsAvoidanceWeight * strength;
        }
    }

    // 가속도를 속도에 적용하고, 속도 제한을 적용합니다.
    public void UpdateVelocity()
    {
        // 가속도를 시간과 프레임에 독립적으로 적용
        velocity += acceleration * Time.deltaTime;

        // 최소 속도 유지 (물고기가 멈추지 않도록)
        float minCurrentSpeed = maxSpeed * 0.4f;
        if (velocity.magnitude < minCurrentSpeed)
        {
            velocity = velocity.normalized * minCurrentSpeed;
        }

        // 최대 속도 제한
        velocity = LimitMagnitude(velocity, maxSpeed);
    }

    // 속도를 이용하여 오브젝트의 위치를 업데이트합니다.
    private void UpdatePosition()
    {
        // 2D 게임이므로 Z축은 0으로 유지
        Vector3 newPosition = transform.position + (Vector3)velocity * Time.deltaTime;
        newPosition.z = 0f;
        transform.position = newPosition;
    }

    // 오브젝트가 현재 속도 방향을 바라보도록 회전시킵니다.
    private void UpdateRotation()
    {
        if (velocity.sqrMagnitude < 0.001f) return; // 속도가 거의 없으면 회전하지 않음

        // 바라봐야 할 각도 계산 (Z축 기준)
        float targetAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

        // 현재 회전에서 목표 회전까지 자연스럽게 보간
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // 주변 이웃들의 평균 속도와 동일한 방향으로 움직이려는 힘 (정렬)을 계산합니다.
    private Vector2 Alignment(IEnumerable<Flocking_Test> boids)
    {
        if (!boids.Any()) return Vector2.zero;

        Vector2 averageVelocity = Vector2.zero;
        foreach (var b in boids)
        {
            averageVelocity += b.velocity;
        }
        averageVelocity /= boids.Count();

        return Steer(averageVelocity.normalized * maxSpeed);
    }

    // 주변 이웃들의 평균 위치(질량 중심)로 이동하려는 힘 (결집)을 계산합니다.
    private Vector2 Cohesion(IEnumerable<Flocking_Test> boids)
    {
        if (!boids.Any()) return Vector2.zero;

        Vector2 centerOfMass = Vector2.zero;
        foreach (var b in boids)
        {
            centerOfMass += (Vector2)b.transform.position; // Vector3를 Vector2로 캐스팅
        }
        centerOfMass /= boids.Count();

        return Steer((centerOfMass - (Vector2)transform.position).normalized * maxSpeed);
    }

    // 가까운 이웃들과 충돌하지 않도록 밀어내려는 힘 (분리)을 계산합니다.
    private Vector2 Separation(IEnumerable<Flocking_Test> boids)
    {
        // neighborhoodRadius 대신 separationRadius를 사용하여 더 가까운 이웃만 고려
        var closeBoids = boids.Where(b => Vector2.Distance(transform.position, b.transform.position) <= separationRadius).ToList();
        if (!closeBoids.Any()) return Vector2.zero;

        Vector2 repulsionForce = Vector2.zero;
        foreach (var b in closeBoids)
        {
            Vector2 diff = (Vector2)transform.position - (Vector2)b.transform.position;
            // 거리가 가까울수록 강하게 밀어내기 위해 거리의 제곱을 역수로 나눔
            repulsionForce += diff.normalized / Mathf.Max(0.001f, diff.magnitude * diff.magnitude);
        }
        repulsionForce /= closeBoids.Count;
        return Steer(repulsionForce.normalized * maxSpeed);
    }

    // 원하는 속도(desired)로 가기 위해 필요한 가속도(steering force)를 계산
    // 이 힘은 최대 힘(maxForce)으로 제한
    private Vector2 Steer(Vector2 desired)
    {
        Vector2 steerForce = desired - velocity;
        return LimitMagnitude(steerForce, maxForce);
    }

    // 벡터의 크기를 제한합니다.
    private Vector2 LimitMagnitude(Vector2 vector, float max)
    {
        return vector.sqrMagnitude > max * max ? vector.normalized * max : vector;
    }

    // --- Gizmos for Debugging ---
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            // 에디터에서만 경계 박스 그리기
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(flockingBoundsCenter, flockingBoundsSize);
        }

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, neighborhoodRadius); // 이웃 탐지 반경
        Gizmos.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
        Gizmos.DrawWireSphere(transform.position, separationRadius); // 분리 반경

        // 현재 속도
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)velocity);

        // 현재 가속도
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)acceleration * 10f); // 가속도는 작게 나타날 수 있으므로 10배 확대

#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            // 실제 Steering 벡터 시각화 (플레이 중)
            var boidColliders = Physics2D.OverlapCircleAll(transform.position, neighborhoodRadius);
            var boids = boidColliders.Select(o => o.GetComponent<Flocking_Test>()).Where(b => b != null && b != this).ToList();

            // 각 힘 계산
            Vector2 align = Alignment(boids);
            Vector2 coh = Cohesion(boids);
            Vector2 sep = Separation(boids);
            Vector2 obstacleSteer = Vector2.zero; // ObstacleAvoidance 내부에서 계산된 steerForce를 가져올 방법이 필요하거나, 여기에 다시 계산 로직을 넣어야 함.
            Vector2 boundarySteer = Vector2.zero; // BoundaryAvoidance 내부에서 계산된 steerForce를 가져올 방법이 필요하거나, 여기에 다시 계산 로직을 넣어야 함.


            Gizmos.color = Color.blue; // 정렬
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)align * alignmentWeight);

            Gizmos.color = Color.green; // 결집
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)coh * cohesionWeight);

            Gizmos.color = Color.red; // 분리
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)sep * separationWeight);

            // 장애물 회피 (ObstacleAvoidance에서 계산된 steerForce를 직접 얻을 수 있다면 좋음)
            // 임시로 레이캐스트 결과에 따라 대략적인 방향만 시각화
            Vector2 currentForward = velocity.normalized;
            RaycastHit2D frontHit = Physics2D.CircleCast(transform.position, 0.2f, currentForward, raycastLength, obstacleLayer);
            if (frontHit.collider)
            {
                Gizmos.color = Color.magenta; // 장애물 회피
                Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)(transform.position - (Vector3)frontHit.point).normalized * obstacleAvoidanceWeight);
            }
        }
#endif
    }
}