using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class Fish : MonoBehaviour
{ // 처리
    public FishData fishData;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    // Job 시스템에 필요한 변수들 (public으로 변경하여 FishSimulationManager에서 접근 가능하게 함)
    [HideInInspector] public Vector2 currentVelocity;
    [HideInInspector] public Vector2 currentAcceleration;
    [HideInInspector] public bool isDie = false;
    public bool IsActingOnPlayer { get; protected set; } // 플레이어에게 반응 중인지 (추격/도망/공격)
    public bool IsOnActionCooldown { get; protected set; } // 행동 쿨다운 중인지
    public bool IsDamagedReacting { get; protected set; } // 피격 반응 중인지

    protected Transform _playerTransform;
    protected bool _isPlayerDetected = false;
    protected float _currentActionTimer;
    protected float _currentActionCooldownTimer;
    protected float _currentAttackCooldownTimer; // 공격 쿨다운 타이머
    protected bool _isAttacking = false; // 공격 중인지 여부

    [HideInInspector] public RaycastHit2D _raycastHitData;
    public int parentID = -1; // Boid의 인스턴스 ID를 할당하여 같은 Boid 소속임을 나타냄

    // ===== 추가: 물고기가 속한 Boid의 스폰 영역 (활동 경계) 정보를 저장 =====
    // Job으로 전달하기 위해 public으로 노출
    [HideInInspector] public Vector2 boidSpawnAreaCenter;
    [HideInInspector] public float boidSpawnAreaRadius;

    // ===== 추가: 물고기가 속한 바이옴의 정보를 저장 =====
    // Job으로 전달하기 위해 public으로 노출
    [HideInInspector] public Vector2 biomeWorldMinBounds;
    [HideInInspector] public Vector2 biomeWorldMaxBounds;


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

    protected virtual void Update()
    {
        HandleActionCooldown();
        HandleAttackCooldown();

        if (isDie) return;

        // ===== 플레이어 관련 로직 우선 처리 =====
        if (IsDamagedReacting)
        {
            HandleDamagedReaction();
            UpdateVelocity(); // 피격 반응 중에도 velocity/acceleration은 업데이트되어야 함
        }
        else if (IsOnActionCooldown)
        {
            // 쿨다운 중에는 멈춤
            currentVelocity = Vector2.zero;
            currentAcceleration = Vector2.zero;
            _playerTransform = null;
        }
        else if (IsActingOnPlayer)
        {
            HandlePlayerInteraction();
            _currentActionTimer -= Time.deltaTime;
            if (_currentActionTimer <= 0)
            {
                ResetPlayerActionState();
            }
            UpdateVelocity(); // 플레이어와 상호작용 중에도 velocity/acceleration은 업데이트되어야 함
        }
        else
        {
            // 플레이어와 상호작용 중이 아닐 때만 플레이어 감지 시도 및 Job 결과 반영
            _isPlayerDetected = DetectPlayer();
            if (_isPlayerDetected)
            {
                HandlePlayerDetection(); // 이 안에서 IsActingOnPlayer 등을 설정할 것
            }
            // Job 시스템에서 계산된 currentAcceleration과 currentVelocity를 UpdateVelocity/UpdatePosition에 활용
            UpdateVelocity();
        }

        UpdatePosition();
        PerformRaycastForObstacles(); // Job으로 넘길 데이터를 미리 계산
    }

    protected virtual void UpdateVelocity()
    {
        // currentAcceleration은 Job 또는 개별 Fish의 HandlePlayerInteraction에서 설정됨
        currentVelocity += currentAcceleration * Time.deltaTime;

        // 플레이어 행동 중일 때는 FishData의 actionSpeedMultiplier 적용
        float maxSpeed = fishData.normalSpeed;
        if (IsActingOnPlayer || IsDamagedReacting)
        {
            maxSpeed *= fishData.actionSpeedMultiplier;
        }
        currentVelocity = Vector2.ClampMagnitude(currentVelocity, maxSpeed); // 여기서 직접 속도 크기 제한

        // 회전 로직
        if (currentVelocity.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, fishData.rotationSpeed * Time.deltaTime);
        }
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

    protected void PerformRaycastForObstacles()
    {
        Vector2 raycastDirection = currentVelocity.normalized;
        if (raycastDirection.sqrMagnitude < 0.001f)
        {
            raycastDirection = transform.right; // 멈춰있을 경우 기본적으로 오른쪽으로 쏜다
        }
        _raycastHitData = Physics2D.Raycast(transform.position, raycastDirection, fishData.raycastLength, obstacleLayer);
    }


    protected virtual bool DetectPlayer()
    {
        // ... (이전 DetectPlayer 로직과 동일) ...
        // AttackFish, NeutralFish, EscapeFish에서 오버라이드 됨
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, fishData.playerDetectionRange, playerLayer);
        if (playerCollider != null)
        {
            _playerTransform = playerCollider.transform;
            return true;
        }
        _playerTransform = null;
        return false;
    }

    protected virtual void HandlePlayerDetection()
    {
        _isPlayerDetected = true;
        IsActingOnPlayer = true; // 플레이어 감지 시 행동 시작
        _currentActionTimer = fishData.chaseDuration; // 행동 지속 시간 설정
        currentVelocity = Vector2.zero; // 잠시 멈춰서 반응 시작
        currentAcceleration = Vector2.zero; // 가속도도 0으로 초기화
    }

    protected virtual void HandlePlayerInteraction()
    {
        // AttackFish, NeutralFish, EscapeFish에서 오버라이드 됨
        // 이 함수 내에서 currentAcceleration을 직접 계산하여 할당해야 합니다.
        // 예시:
        // if (_playerTransform != null) {
        //     Vector2 desiredVelocity = (_playerTransform.position - transform.position).normalized * fishData.normalSpeed * fishData.actionSpeedMultiplier;
        //     currentAcceleration = Steer(desiredVelocity, currentVelocity, fishData.flockMaxForce);
        // }
    }

    public virtual void TakeDamage(Transform damageDealer, float damage)
    {
        if (fishData.health <= 0) return;

        fishData.health -= damage;

        if (fishData.health <= 0 && !isDie)
        {
            isDie = true;
            Debug.Log($"{gameObject.name} is dead.");
            // 사망 처리 (애니메이션, 비활성화 등)
        }
        ImmediateDetection(damageDealer); // 데미지 입으면 즉시 플레이어 감지 로직 활성화
    }

    protected virtual void ImmediateDetection(Transform damageDealer)
    {
        ResetPlayerActionState(); // 기존 행동 초기화
        IsDamagedReacting = true; // 피격 반응 상태로 진입
        _playerTransform = damageDealer; // 데미지를 준 오브젝트 (플레이어)를 추적 대상으로 설정
        currentVelocity = Vector2.zero; // 잠시 멈춰서 반응 시작
        currentAcceleration = Vector2.zero; // 가속도도 0으로 초기화
    }

    protected virtual void HandleDamagedReaction()
    {
        // 자식 클래스에서 오버라이드하여 구체적인 피격 반응 구현
        // 예를 들어, EscapeFish는 도망치고, AttackFish는 반격 등
        // 이 함수 내에서도 currentAcceleration을 직접 계산하여 할당해야 합니다.
    }

    protected virtual void ResetPlayerActionState()
    {
        _isPlayerDetected = false;
        IsActingOnPlayer = false;
        IsDamagedReacting = false;
        _playerTransform = null;
        _currentActionTimer = 0f;
        _isAttacking = false;
        IsOnActionCooldown = true; // 행동 종료 후 쿨다운 시작
        _currentActionCooldownTimer = fishData.chaseCooldown;
        currentVelocity = Vector2.zero; // 쿨다운 시작 시 속도 초기화
        currentAcceleration = Vector2.zero; // 쿨다운 시작 시 가속도 초기화
    }

    protected void HandleActionCooldown()
    {
        if (IsOnActionCooldown)
        {
            _currentActionCooldownTimer -= Time.deltaTime;
            if (_currentActionCooldownTimer <= 0)
            {
                IsOnActionCooldown = false;
                _currentActionCooldownTimer = 0f;
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

    // 물고기 개별의 군집 경계 중심 (자기 자신의 위치)
    public Vector2 GetFlockingBoundsCenter() { return transform.position; }
    // 물고기 개별의 군집 경계 반경 (FishData에서 가져옴)
    public float GetFlockingBoundsRadius() { return fishData.flockNeighborhoodRadius * 2f; } // 예를 들어 군집 반경의 2배

    protected Vector2 Steer(Vector2 desired, Vector2 current, float maxForce)
    {
        Vector2 steerForce = desired - current;
        return LimitMagnitude(steerForce, maxForce);
    }

    protected Vector2 LimitMagnitude(Vector2 vector, float max)
    {
        if (vector.sqrMagnitude > max * max)
        {
            return vector.normalized * max;
        }
        return vector;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (fishData != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, fishData.playerDetectionRange);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, fishData.flockNeighborhoodRadius);

            // Raycast 방향 시각화 (노란색)
            Vector2 raycastDirection = currentVelocity.normalized;
            if (raycastDirection.sqrMagnitude < 0.001f)
            {
                raycastDirection = transform.right;
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, (Vector3)raycastDirection * fishData.raycastLength);

            // Boid 스폰 영역 경계 시각화 (주황색)
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, 0.5f); // 주황색
            Gizmos.DrawWireSphere(boidSpawnAreaCenter, boidSpawnAreaRadius);

            // 바이옴 경계 시각화 (녹색)
            Gizmos.color = Color.green;
            Vector2 biomeCenter = (biomeWorldMinBounds + biomeWorldMaxBounds) / 2f;
            Vector2 biomeSize = biomeWorldMaxBounds - biomeWorldMinBounds;
            Gizmos.DrawWireCube(biomeCenter, biomeSize);
        }
    }
}
