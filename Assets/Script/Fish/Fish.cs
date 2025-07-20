using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public abstract class Fish : MonoBehaviour
{ // ó��
    public FishData fishData;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    private float currentHp;

    [HideInInspector] public Vector2 currentVelocity;
    [HideInInspector] public Vector2 currentAcceleration;
    [HideInInspector] public bool isDie = false;

    public bool IsActingOnPlayer { get; protected set; }
    public bool IsOnReDetectionCooldown { get; protected set; }
    public bool IsDamagedReacting { get; protected set; }

    protected Transform _playerTransform;
    protected bool _isPlayerDetected = false;
    protected float _currentActionTimer;
    protected float _currentReDetectionCooldownTimer;
    protected float _currentAttackCooldownTimer;
    protected bool _isAttacking = false;

    protected float _currentNeutralEngagementTimer;

    // RaycastHit2D _raycastHitData는 이제 Job으로 전달되지 않고, 다중 레이캐스트 결과로 대체됩니다.
    // [HideInInspector] public RaycastHit2D _raycastHitData; // 제거 또는 용도 변경

    // === 추가/변경: 장애물 회피를 위한 다중 레이캐스트 정보 ===
    [HideInInspector] public Vector2 _avoidanceDirection = Vector2.zero; // 회피해야 할 방향
    [HideInInspector] public bool _isObstacleAhead = false; // 전방에 장애물이 있는지 여부

    public int parentID = -1;

    [HideInInspector] public Vector2 boidSpawnAreaCenter;
    [HideInInspector] public float boidSpawnAreaRadius;

    [HideInInspector] public Vector2 biomeWorldMinBounds;
    [HideInInspector] public Vector2 biomeWorldMaxBounds;

    private SpriteRenderer spriteRenderer;


    protected virtual void OnEnable()
    {
        if (FishSimulationManager.Instance != null)
        {
            FishSimulationManager.Instance.RegisterFish(this);
        }
    }

    protected virtual void OnDisable()
    {
        if (FishSimulationManager.Instance != null)
        {
            FishSimulationManager.Instance.UnregisterFish(this);
        }
    }

    protected virtual void Awake()
    {
        currentVelocity = Vector2.zero;
        currentAcceleration = Vector2.zero;
    }

    private void Start()
    {
        currentHp = fishData.health;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void Update()
    {
        HandleReDetectionCooldown();
        HandleAttackCooldown();

        if (isDie) return;

        // === 변경: 항상 장애물 회피를 위한 레이캐스트를 수행합니다. ===
        // 기존 PerformRaycastForObstacles()는 제거하고, CalculateAvoidanceDirection()으로 대체합니다.
        // 이 계산 결과(_avoidanceDirection, _isObstacleAhead)는 Job으로 전달됩니다.
        CalculateAvoidanceDirection();

        if (IsDamagedReacting)
        {
            HandleDamagedReaction();
            UpdateVelocity(); // Fish.UpdateVelocity()에서 회전 로직을 처리합니다.
        }
        else if (IsActingOnPlayer)
        {
            HandlePlayerInteraction();
            _currentActionTimer -= Time.deltaTime;
            if (_currentActionTimer <= 0)
            {
                ResetPlayerActionState();
            }
            UpdateVelocity(); // Fish.UpdateVelocity()에서 회전 로직을 처리합니다.
        }
        else // 일반적인 군집/순찰 상태 (플레이어와 상호작용 중이 아님)
        {
            if (!IsOnReDetectionCooldown)
            {
                _isPlayerDetected = DetectPlayer();
                if (_isPlayerDetected)
                {
                    HandlePlayerDetection();
                }
            }
            // === 변경: Job으로부터 받은 currentAcceleration을 사용하여 속도를 업데이트합니다.
            // 회전은 UpdateVelocity()에서 _avoidanceDirection 또는 currentVelocity를 기반으로 합니다.
            UpdateVelocity();
        }

        UpdatePosition();
    }

    protected virtual void UpdateVelocity()
    {
        currentVelocity += currentAcceleration * Time.deltaTime;

        float maxSpeed = fishData.normalSpeed;
        if (IsActingOnPlayer || IsDamagedReacting)
        {
            maxSpeed *= fishData.actionSpeedMultiplier;
        }
        currentVelocity = Vector2.ClampMagnitude(currentVelocity, maxSpeed);

        // === 변경: 물고기 스프라이트 방향 및 기울기 제어 ===

        // 1. 좌우 반전 (핵심)
        // 현재 속도의 X 방향을 기반으로 스프라이트를 반전시킵니다.
        // 이는 물고기가 왼쪽으로 가면 스프라이트가 왼쪽을 보게 하고, 오른쪽으로 가면 오른쪽을 보게 합니다.
        if (currentVelocity.x > 0.01f) // 오른쪽으로 이동 (약간의 오차 범위 허용)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = true; // 기본 방향
        }
        else if (currentVelocity.x < -0.01f) // 왼쪽으로 이동
        {
            if (spriteRenderer != null) spriteRenderer.flipX = false; // 좌우 반전
        }
        // 만약 X 속도가 0에 가깝다면, 마지막 방향을 유지합니다 (옵션).
        // 이 예제에서는 현재 X 속도가 있다면 무조건 반전합니다.

        // 2. 상하 기울기 (Z축 회전)
        // 위아래로 움직이는 정도에 따라 스프라이트를 기울입니다.
        // 제한된 각도(예: 40도)를 넘지 않도록 합니다.
        const float MAX_VERTICAL_ANGLE = 40f; // 상하 최대 기울기 각도

        // 현재 속도의 Y 방향에 따라 목표 각도 계산
        // currentVelocity.normalized.y는 -1에서 1 사이의 값을 가집니다.
        // 이 값을 -MAX_VERTICAL_ANGLE에서 MAX_VERTICAL_ANGLE 사이의 각도로 매핑합니다.
        float targetAngleZ = -currentVelocity.normalized.y * MAX_VERTICAL_ANGLE;

        // 만약 스프라이트가 왼쪽을 바라보고 있다면, Y축 기울기의 방향을 반전시켜야 합니다.
        // (예: 왼쪽으로 이동 중 위로 가면 Z축 회전은 양수, 오른쪽으로 이동 중 위로 가면 Z축 회전은 음수)
        if (spriteRenderer != null && spriteRenderer.flipX) // 왼쪽을 바라보고 있을 때
        {
            targetAngleZ = -targetAngleZ; // Y축 기울기 각도 반전
        }

        // 현재 Z축 회전을 목표 Z축 회전으로 부드럽게 보간합니다.
        // rotationSpeed는 이제 기울기 회전 속도를 제어합니다.
        float currentAngleZ = transform.localEulerAngles.z;
        if (currentAngleZ > 180) currentAngleZ -= 360; // -180 ~ 180 범위로 변환

        float newAngleZ = Mathf.LerpAngle(currentAngleZ, targetAngleZ, fishData.rotationSpeed * Time.deltaTime);
        transform.localEulerAngles = new Vector3(0, 0, newAngleZ);

        // 참고: 물고기의 Z축 회전(기울기)은 여전히 transform.rotation을 사용합니다.
        // 다만, 이 회전은 오로지 스프라이트가 위아래로 기울어지는 "시각적인" 효과를 위한 것이며,
        // 물고기의 실제 "정면"은 항상 X축(좌우)에 고정됩니다.

        // 중요: CalculateAvoidanceDirection()에서 사용하는 forwardDirection은 이제 transform.right가 아니라
        // currentVelocity.normalized (또는 기본 X축 방향)를 사용해야 합니다.
        // (이미 이전 수정에서 그렇게 변경했습니다.)
    }

    protected void UpdatePosition()
    {
        if (float.IsNaN(currentVelocity.x) || float.IsNaN(currentVelocity.y))
        {
            Debug.LogError($"Invalid velocity detected for {gameObject.name}! Resetting velocity to zero. Current velocity: {currentVelocity}");
            currentVelocity = Vector2.zero;
        }

        Vector3 newPosition = transform.position + (Vector3)currentVelocity * Time.deltaTime;
        newPosition.z = 0f;
        transform.position = newPosition;
    }

    // === 추가: 다중 레이캐스트를 이용한 장애물 회피 방향 계산 ===
    protected void CalculateAvoidanceDirection()
    {
        _avoidanceDirection = Vector2.zero;
        _isObstacleAhead = false;

        // 회피를 위한 레이캐스트 개수 및 각도 설정 (FishData에 추가하는 것이 더 유연합니다)
        int numRays = 7; // 중앙, 좌우 3개씩
        float rayAngleIncrement = 30f; // 각 레이 사이의 각도 (중앙에서 좌우로 벌어지는 각도)
        float totalAngleSpread = (numRays - 1) * rayAngleIncrement; // 전체 각도 범위
        float startAngle = -totalAngleSpread / 2f; // 시작 각도

        // 현재 물고기의 진행 방향 (currentVelocity가 0일 경우 transform.right 사용)
        Vector2 forwardDirection = currentVelocity.normalized;
        if (forwardDirection.sqrMagnitude < 0.001f)
        {
            forwardDirection = transform.right;
        }

        float bestAngle = 0f; // 가장 좋은 회피 방향 각도 (relative to forwardDirection)
        float maxDistance = -1f; // 가장 먼 거리를 가진 레이 (안전한 방향)

        for (int i = 0; i < numRays; i++)
        {
            float currentRayAngle = startAngle + i * rayAngleIncrement;
            Quaternion rotation = Quaternion.AngleAxis(currentRayAngle, Vector3.forward);
            Vector2 rayDirection = rotation * forwardDirection;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, fishData.raycastLength, obstacleLayer);

            if (hit.collider != null)
            {
                _isObstacleAhead = true; // 하나라도 장애물에 부딪히면 플래그 설정
                // 이 레이에 장애물이 있음. 안전하지 않은 방향.
                // 일단 히트된 경우, 이 방향은 회피 대상이 됩니다.
                // 여기서는 가장 안전한(가장 먼) 방향을 찾습니다.
                if (hit.distance > maxDistance)
                {
                    maxDistance = hit.distance;
                    bestAngle = currentRayAngle; // 이 각도를 잠정적으로 가장 좋은 각도로 설정
                }
            }
            else // 장애물이 없는 방향
            {
                // 장애물이 없는 방향은 언제나 더 안전한 방향으로 간주
                if (fishData.raycastLength > maxDistance)
                {
                    maxDistance = fishData.raycastLength; // 최대 길이까지 도달했으므로 가장 안전
                    bestAngle = currentRayAngle;
                }
            }
        }

        if (_isObstacleAhead)
        {
            // 가장 안전한 방향(장애물이 없거나 가장 멀리 있는)을 찾아 그 방향으로 유도합니다.
            Quaternion bestRotation = Quaternion.AngleAxis(bestAngle, Vector3.forward);
            _avoidanceDirection = bestRotation * forwardDirection;

            // 만약 모든 방향이 막혀있다면, 현재 이동 방향의 법선 방향 (오른쪽 또는 왼쪽)으로 돌게 합니다.
            if (_avoidanceDirection.sqrMagnitude < 0.001f)
            {
                _avoidanceDirection = Quaternion.AngleAxis(90f, Vector3.forward) * forwardDirection; // 기본적으로 오른쪽으로 회피
            }
        }
        // _isObstacleAhead가 false이면 _avoidanceDirection은 Vector2.zero로 유지됩니다.
    }

    // 기존 PerformRaycastForObstacles()는 제거합니다.
    // protected void PerformRaycastForObstacles()
    // {
    //     Vector2 raycastDirection = currentVelocity.normalized;
    //     if (raycastDirection.sqrMagnitude < 0.001f)
    //     {
    //         raycastDirection = transform.right;
    //     }
    //     _raycastHitData = Physics2D.Raycast(transform.position, raycastDirection, fishData.raycastLength, obstacleLayer);
    // }

    protected virtual bool DetectPlayer()
    {
        // === 변경: OverlapCircleAll 또는 Physics2D.RaycastAll을 활용하여 시야각 내 플레이어 감지 ===
        // 기존 OverlapCircle은 모든 방향을 감지하므로 제거합니다.
        // 대신, 시야각(FOV)에 맞는 충돌체를 직접 찾습니다.
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, fishData.playerDetectionRange, playerLayer);

        foreach (Collider2D playerCollider in hitColliders)
        {
            Vector2 directionToPlayer = (playerCollider.transform.position - transform.position).normalized;
            float angleToPlayer = Vector2.Angle(transform.right, directionToPlayer); // transform.right가 물고기의 정면

            if (angleToPlayer <= fishData.fieldOfView / 2f)
            {
                // 플레이어와 물고기 사이에 장애물이 없는지 확인 (레이캐스트는 플레이어 방향으로 유지)
                RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, Vector2.Distance(transform.position, playerCollider.transform.position), obstacleLayer);

                // Debug.DrawRay(transform.position, directionToPlayer * Vector2.Distance(transform.position, playerCollider.transform.position), Color.red); // 디버그용

                if (hit.collider == null || hit.collider.transform == playerCollider.transform) // 직접 플레이어와 충돌했거나 장애물이 없음
                {
                    _playerTransform = playerCollider.transform;
                    return true;
                }
            }
        }
        _playerTransform = null;
        return false;
    }

    protected virtual void HandlePlayerDetection()
    {
        _isPlayerDetected = true;
        IsActingOnPlayer = true;
        _currentActionTimer = fishData.chaseDuration;
    }

    protected abstract void HandlePlayerInteraction();

    public virtual void TakeDamage(Transform damageDealer, float damage)
    {
        Debug.Log("Damage");
        if (currentHp <= 0) return;

        currentHp -= damage;

        if (currentHp <= 0 && !isDie)
        {
            isDie = true;
            Debug.Log($"{gameObject.name} is dead.");
        }

        if (!isDie)
        {
            ImmediateDetection(damageDealer);
        }
    }

    protected virtual void ImmediateDetection(Transform damageDealer)
    {
        ResetPlayerActionState();
        IsDamagedReacting = true;
        _playerTransform = damageDealer;
    }

    protected abstract void HandleDamagedReaction();

    protected virtual void ResetPlayerActionState()
    {
        _isPlayerDetected = false;
        IsActingOnPlayer = false;
        IsDamagedReacting = false;
        _playerTransform = null;
        _currentActionTimer = 0f;
        _isAttacking = false;
        IsOnReDetectionCooldown = true;
        _currentReDetectionCooldownTimer = fishData.chaseCooldown;
        _currentNeutralEngagementTimer = 0f;
    }

    protected void HandleReDetectionCooldown()
    {
        if (IsOnReDetectionCooldown)
        {
            _currentReDetectionCooldownTimer -= Time.deltaTime;
            if (_currentReDetectionCooldownTimer <= 0)
            {
                IsOnReDetectionCooldown = false;
                _currentReDetectionCooldownTimer = 0f;
            }
        }
    }

    protected void HandleAttackCooldown()
    {
        if (_currentAttackCooldownTimer > 0)
        {
            _currentAttackCooldownTimer -= Time.deltaTime;
            if (_currentAttackCooldownTimer < 0)
            {
                _currentAttackCooldownTimer = 0;
            }
        }
    }

    public Vector2 GetFlockingBoundsCenter() { return transform.position; }
    public float GetFlockingBoundsRadius() { return fishData.flockNeighborhoodRadius * 2f; }

    // Steer 함수는 그대로 사용
    protected Vector2 Steer(Vector2 desired, Vector2 current, float maxForce)
    {
        Vector2 steerForce = desired - current;
        return LimitMagnitude(steerForce, maxForce);
    }

    // LimitMagnitude 함수는 그대로 사용
    protected Vector2 LimitMagnitude(Vector2 vector, float max)
    {
        if (vector.sqrMagnitude > max * max)
        {
            return vector.normalized * max;
        }
        return vector;
    }

    // --- 시각화 ---
    protected virtual void OnDrawGizmosSelected()
    {
        if (fishData != null)
        {
            // 기존 플레이어 감지 범위 및 시야각 기즈모
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, fishData.playerDetectionRange);

            Gizmos.color = Color.blue;
            Vector3 fovDirection = transform.right;
            float halfFOV = fishData.fieldOfView / 2f;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.forward);
            Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.forward);
            Vector3 leftRayDirection = leftRayRotation * fovDirection;
            Vector3 rightRayDirection = rightRayRotation * fovDirection;

            Gizmos.DrawLine(transform.position, transform.position + leftRayDirection * fishData.playerDetectionRange);
            Gizmos.DrawLine(transform.position, transform.position + rightRayDirection * fishData.playerDetectionRange);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, fishData.flockNeighborhoodRadius);

            // === 변경: 다중 레이캐스트 시각화 ===
            int numRays = 7;
            float rayAngleIncrement = 30f;
            float totalAngleSpread = (numRays - 1) * rayAngleIncrement;
            float startAngle = -totalAngleSpread / 2f;

            Vector2 forwardDirection = currentVelocity.normalized;
            if (forwardDirection.sqrMagnitude < 0.001f)
            {
                forwardDirection = transform.right;
            }

            for (int i = 0; i < numRays; i++)
            {
                float currentRayAngle = startAngle + i * rayAngleIncrement;
                Quaternion rotation = Quaternion.AngleAxis(currentRayAngle, Vector3.forward);
                Vector2 rayDirection = rotation * forwardDirection;

                RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, fishData.raycastLength, obstacleLayer);

                if (hit.collider != null)
                {
                    Gizmos.color = Color.red; // 장애물 발견 시 빨간색
                    Gizmos.DrawLine(transform.position, hit.point);
                }
                else
                {
                    Gizmos.color = Color.yellow; // 장애물 없음 시 노란색
                    Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)rayDirection * fishData.raycastLength);
                }
            }
            // === 추가: 계산된 회피 방향 시각화 ===
            if (_isObstacleAhead)
            {
                Gizmos.color = Color.green; // 회피 방향은 초록색
                Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)_avoidanceDirection.normalized * fishData.raycastLength * 1.2f);
            }

            Gizmos.color = Color.Lerp(Color.yellow, Color.red, 0.5f);
            Gizmos.DrawWireSphere(boidSpawnAreaCenter, boidSpawnAreaRadius);

            Gizmos.color = Color.green;
            Vector2 biomeCenter = (biomeWorldMinBounds + biomeWorldMaxBounds) / 2f;
            Vector2 biomeSize = biomeWorldMaxBounds - biomeWorldMinBounds;
            Gizmos.DrawWireCube(biomeCenter, biomeSize);
        }
    }

    protected virtual void OnDrawGizmos()
    {
        if (_isPlayerDetected && _playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _playerTransform.position);
        }
    }
}
