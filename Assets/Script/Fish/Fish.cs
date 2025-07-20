using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class Fish : MonoBehaviour
{
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

    [HideInInspector] public Vector2 _avoidanceDirection = Vector2.zero;
    [HideInInspector] public bool _isObstacleAhead = false;

    public int parentID = -1;

    [HideInInspector] public Vector2 boidSpawnAreaCenter;
    [HideInInspector] public float boidSpawnAreaRadius;

    [HideInInspector] public Vector2 biomeWorldMinBounds;
    [HideInInspector] public Vector2 biomeWorldMaxBounds;

    [SerializeField] protected SpriteRenderer spriteRenderer;

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
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"SpriteRenderer not found on {gameObject.name} or its children.", this);
            }
        }
    }

    private void Start()
    {
        currentHp = fishData.health;
    }

    protected virtual void Update()
    {
        HandleReDetectionCooldown();
        HandleAttackCooldown();

        if (isDie) return;

        CalculateAvoidanceDirection();

        if (IsDamagedReacting)
        {
            HandleDamagedReaction();
            UpdateVisualOrientation();
        }
        else if (IsActingOnPlayer)
        {
            HandlePlayerInteraction();
            _currentActionTimer -= Time.deltaTime;
            if (_currentActionTimer <= 0)
            {
                ResetPlayerActionState();
            }
            UpdateVisualOrientation();
        }
        else
        {
            if (!IsOnReDetectionCooldown)
            {
                _isPlayerDetected = DetectPlayer();
                if (_isPlayerDetected)
                {
                    HandlePlayerDetection();
                }
            }
            UpdateVisualOrientation();
        }

        UpdatePosition();
    }

    protected virtual void UpdateVisualOrientation()
    {
        // 1. 좌우 반전
        // 현재 속도 X 방향에 따라 스프라이트 반전함
        if (spriteRenderer != null)
        {
            if (currentVelocity.x > 0.01f) // 오른쪽으로 이동
            {
                spriteRenderer.flipX = true; // 오른쪽을 볼 때 flipX=true (기본 스프라이트가 왼쪽을 봄)
            }
            else if (currentVelocity.x < -0.01f) // 왼쪽으로 이동
            {
                spriteRenderer.flipX = false; // 왼쪽을 볼 때 flipX=false (기본 스프라이트가 왼쪽을 봄)
            }
        }

        // 2. 상하 기울기 (Z축 회전)
        // 위아래 움직임에 따라 스프라이트 기울임
        const float MAX_VERTICAL_ANGLE = 40f;

        // 속도가 너무 작으면 기울기 적용 안함
        if (currentVelocity.sqrMagnitude < 0.01f)
        {
            // 속도 없으면 정면으로 돌아옴
            transform.localEulerAngles = Vector3.Lerp(transform.localEulerAngles, new Vector3(0, 0, 0), fishData.rotationSpeed * Time.deltaTime);
            return;
        }

        // 목표 Z축 각도 계산: Y 속도에 비례함
        // currentVelocity.normalized.y 가 양수면 위, 음수면 아래
        // 수정: Y 속도가 양수일 때 양의 각도, 음수일 때 음의 각도 (스프라이트 기준)
        float targetAngleZ = currentVelocity.normalized.y * MAX_VERTICAL_ANGLE;

        // 스프라이트가 왼쪽(flipX=false)을 바라보고 있을 때 각도 방향 반전함
        if (spriteRenderer != null && spriteRenderer.flipX == false)
        {
            targetAngleZ = -targetAngleZ;
        }

        float currentAngleZ = transform.localEulerAngles.z;
        if (currentAngleZ > 180) currentAngleZ -= 360; // -180 ~ 180 범위로 변환

        float newAngleZ = Mathf.LerpAngle(currentAngleZ, targetAngleZ, fishData.rotationSpeed * Time.deltaTime);
        transform.localEulerAngles = new Vector3(0, 0, newAngleZ);
    }

    protected void UpdatePosition()
    {
        currentVelocity += currentAcceleration * Time.deltaTime; // Job에서 온 가속도 반영함
        float maxSpeed = fishData.normalSpeed;
        if (IsActingOnPlayer || IsDamagedReacting)
        {
            maxSpeed *= fishData.actionSpeedMultiplier;
        }
        currentVelocity = Vector2.ClampMagnitude(currentVelocity, maxSpeed);

        if (float.IsNaN(currentVelocity.x) || float.IsNaN(currentVelocity.y))
        {
            Debug.LogError($"Invalid velocity detected for {gameObject.name}! Resetting velocity to zero. Current velocity: {currentVelocity}");
            currentVelocity = Vector2.zero;
        }

        Vector3 newPosition = transform.position + (Vector3)currentVelocity * Time.deltaTime;
        newPosition.z = 0f;
        transform.position = newPosition;
    }

    protected void CalculateAvoidanceDirection()
    {
        _avoidanceDirection = Vector2.zero;
        _isObstacleAhead = false;

        int numRays = 7;
        float rayAngleIncrement = 30f;
        float totalAngleSpread = (numRays - 1) * rayAngleIncrement;
        float startAngle = -totalAngleSpread / 2f;

        // 장애물 감지 레이 기준 방향: 실제 이동 방향
        Vector2 raycastBaseDirection = currentVelocity.normalized;
        if (raycastBaseDirection.sqrMagnitude < 0.001f)
        {
            // 속도 0에 가까울 때, 스프라이트 바라보는 방향 기준으로 레이 발사함
            raycastBaseDirection = spriteRenderer.flipX ? Vector2.right : Vector2.left;
        }

        float bestAngle = 0f;
        float maxDistance = -1f;

        for (int i = 0; i < numRays; i++)
        {
            float currentRayAngle = startAngle + i * rayAngleIncrement;
            Quaternion rotation = Quaternion.AngleAxis(currentRayAngle, Vector3.forward);
            Vector2 rayDirection = rotation * raycastBaseDirection;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, fishData.raycastLength, obstacleLayer);

            if (hit.collider != null)
            {
                _isObstacleAhead = true;
                if (hit.distance > maxDistance)
                {
                    maxDistance = hit.distance;
                    bestAngle = currentRayAngle;
                }
            }
            else
            {
                if (fishData.raycastLength > maxDistance)
                {
                    maxDistance = fishData.raycastLength;
                    bestAngle = currentRayAngle;
                }
            }
        }

        if (_isObstacleAhead)
        {
            Quaternion bestRotation = Quaternion.AngleAxis(bestAngle, Vector3.forward);
            _avoidanceDirection = bestRotation * raycastBaseDirection;

            if (_avoidanceDirection.sqrMagnitude < 0.001f)
            {
                // 모든 방향이 막혔을 경우, X 방향에 수직인 방향으로 회피 시도함
                _avoidanceDirection = spriteRenderer.flipX ? new Vector2(0, -1) : new Vector2(0, 1);
            }
        }
    }

    protected virtual bool DetectPlayer()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, fishData.playerDetectionRange, playerLayer);

        // 현재 스프라이트가 바라보는 방향 (flipX에 따라 조정)
        Vector2 currentFacingDirection = spriteRenderer.flipX ? Vector2.right : Vector2.left;

        foreach (Collider2D playerCollider in hitColliders)
        {
            Vector2 directionToPlayer = (playerCollider.transform.position - transform.position).normalized;

            // 물고기 스프라이트 정면을 기준으로 각도 계산함
            float angleToPlayer = Vector2.Angle(currentFacingDirection, directionToPlayer);

            if (angleToPlayer <= fishData.fieldOfView / 2f)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, Vector2.Distance(transform.position, playerCollider.transform.position), obstacleLayer);

                if (hit.collider == null || hit.collider.transform == playerCollider.transform)
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

    // --- 시각화 ---
    protected virtual void OnDrawGizmosSelected()
    {
        if (fishData != null)
        {
            // 플레이어 감지 시야각 기즈모
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, fishData.playerDetectionRange);

            // 현재 물고기가 바라보는 방향 (flipX에 따라 조정)
            Vector3 currentFacingDirectionGizmo = spriteRenderer.flipX ? Vector3.right : Vector3.left;

            Gizmos.color = Color.blue;
            float halfFOV = fishData.fieldOfView / 2f;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.forward);
            Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.forward);

            // 시야각 라인 그림
            Vector3 leftRayDirection = leftRayRotation * currentFacingDirectionGizmo;
            Vector3 rightRayDirection = rightRayRotation * currentFacingDirectionGizmo;

            Gizmos.DrawLine(transform.position, transform.position + leftRayDirection * fishData.playerDetectionRange);
            Gizmos.DrawLine(transform.position, transform.position + rightRayDirection * fishData.playerDetectionRange);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, fishData.flockNeighborhoodRadius);

            // 다중 레이캐스트 시각화
            int numRays = 7;
            float rayAngleIncrement = 30f;
            float totalAngleSpread = (numRays - 1) * rayAngleIncrement;
            float startAngle = -totalAngleSpread / 2f;

            // 장애물 감지 레이의 기준 방향
            Vector2 raycastBaseDirectionGizmo = currentVelocity.normalized;
            if (raycastBaseDirectionGizmo.sqrMagnitude < 0.001f)
            {
                raycastBaseDirectionGizmo = spriteRenderer.flipX ? Vector2.right : Vector2.left;
            }

            for (int i = 0; i < numRays; i++)
            {
                float currentRayAngle = startAngle + i * rayAngleIncrement;
                Quaternion rotation = Quaternion.AngleAxis(currentRayAngle, Vector3.forward);
                Vector2 rayDirection = rotation * raycastBaseDirectionGizmo;

                RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, fishData.raycastLength, obstacleLayer);

                if (hit.collider != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, hit.point);
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)rayDirection * fishData.raycastLength);
                }
            }
            if (_isObstacleAhead)
            {
                Gizmos.color = Color.green;
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