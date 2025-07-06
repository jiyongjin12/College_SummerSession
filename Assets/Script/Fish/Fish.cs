using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class Fish : MonoBehaviour
{ // 처리
    public FishData fishData;
    public Boid parentBoid;
    public Biome currentBiome;

    protected Transform _playerTransform;
    protected bool _isPlayerDetected = false;
    protected bool _isActingOnPlayer = false;
    protected bool _isOnActionCooldown = false;
    protected bool _isDamagedReacting = false;
    protected bool _isAttacking = false;

    public float _currentActionTimer = 0f;
    private float _currentActionCooldownTimer = 0f;
    protected float _currentAttackCooldownTimer = 0f;

    public bool FlockingSystemONOFF_Test = false;

    protected Vector2 acceleration;
    protected Vector2 velocity;

    public LayerMask obstacleLayer;
    public LayerMask playerLayer;

    private Vector2 _flockingBoundsCenter;
    private float _flockingBoundsRadius;

    public void SetFlockingBounds(Vector2 center, float radius)
    {
        _flockingBoundsCenter = center;
        _flockingBoundsRadius = radius;
    }

    protected virtual void Awake()
    {
        if (fishData == null)
        {
            Debug.LogWarning($"Fish on {gameObject.name} does not have FishData assigned! Flocking behavior may not work correctly.");
        }

        if (obstacleLayer.value == 0)
        {
            obstacleLayer = LayerMask.GetMask("Wall");
            if (obstacleLayer.value == 0)
            {
                Debug.LogWarning($"Layer 'Wall' not found for obstacleLayer on {gameObject.name}. Please set it manually in Inspector or create 'Wall' layer.");
            }
        }
        if (playerLayer.value == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
            if (playerLayer.value == 0)
            {
                Debug.LogWarning($"Layer 'Player' not found for playerLayer on {gameObject.name}. Please set it manually in Inspector or create 'Player' layer.");
            }
        }
    }

    protected virtual void Start()
    {
        float currentMaxSpeed = 0f;

        if (fishData != null)
            currentMaxSpeed = fishData.normalSpeed;
        else
        {
            Debug.LogError("FishData가 할당되지 않았습니다. 기본 속도를 3f로 설정합니다.");
            currentMaxSpeed = 3f;
        }

        float angle = Random.Range(0, 2 * Mathf.PI);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * currentMaxSpeed;
        transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
    }

    protected virtual void Update()
    {
        acceleration = Vector2.zero;

        if (fishData == null) return;

        if (_currentAttackCooldownTimer > 0)
        {
            _currentAttackCooldownTimer -= Time.deltaTime;
        }

        if (FlockingSystemONOFF_Test)
        {
            HandlePlayerInteraction();
        }
        else if (_isDamagedReacting)
        {
            HandleDamagedReaction();
        }
        else if (_isActingOnPlayer)
        {
            _currentActionTimer -= Time.deltaTime;

            if (_currentActionTimer <= 0f)
            {
                Debug.Log($"{gameObject.name}: Action time expired. Entering cooldown.");
                ResetPlayerActionState();
            }
            else
            {
                HandlePlayerInteraction();
            }
        }
        else if (_isOnActionCooldown)
        {
            _currentActionCooldownTimer -= Time.deltaTime;

            if (_currentActionCooldownTimer <= 0f)
            {
                Debug.Log($"{gameObject.name}: Cooldown finished. Ready to detect player again.");
                _isOnActionCooldown = false;
            }

            PerformFlockingAndBoundaryChecks(); // 군집 행동
        }
        else
        {
            // 플레이어 감지 시도 (각 자식 클래스에서 오버라이드하여 감지)
            bool playerFound = DetectPlayer();
            if (playerFound)
            {
                HandlePlayerDetection(); // 플레이어 감지 시 공통 처리
            }
            else if (_isPlayerDetected && !_isActingOnPlayer) // 이전에 감지되었는데 지금은 사라진 경우 (NeutralFish용)
            {
                // NeutralFish에서 이 상태를 ResetNeutralWatchState()로 처리
                // Attack/EscapeFish는 _isActingOnPlayer 상태에서 _playerTransform == null로 처리됨
            }

            // 플레이어에게 행동 중이 아닐 때만 군집 행동
            if (!_isActingOnPlayer && !_isPlayerDetected) // _isPlayerDetected는 NeutralFish의 주시 상태를 의미
            {
                PerformFlockingAndBoundaryChecks();
            }
        }

        UpdateVelocity();
        UpdatePosition();
        UpdateRotation();
    }

    /// <summary>
    /// 플레이어 감지 로직. 각 물고기 타입이 오버라이드하여 플레이어 감지 여부를 반환합니다.
    /// 감지 시 _playerTransform에 플레이어 참조를 저장해야 합니다.
    /// </summary>
    /// <returns>플레이어가 시야 내에 감지되면 true, 아니면 false.</returns>
    protected virtual bool DetectPlayer() { return false; }

    /// <summary>
    /// 플레이어가 감지되었을 때 공통적으로 수행하는 초기화 및 상태 전환 로직.
    /// </summary>
    protected virtual void HandlePlayerDetection()
    {
        _isPlayerDetected = true;       // 플레이어 감지 플래그 켜기
        velocity = Vector2.zero;        // 움직임 완전 정지
        Debug.Log($"{gameObject.name}: Player Detected! Initializing reaction.");
    }

    /// <summary>
    /// 플레이어에게 데미지를 받았을 때 호출되는 메서드.
    /// </summary>
    public virtual void TakeDamage(Transform damageDealer)
    {
        Debug.Log($"{gameObject.name} took damage from {damageDealer.name}. Default Fish reaction.");
    }

    /// <summary>
    /// 피격 시 반응을 처리하는 추상 메서드. 자식 클래스에서 오버라이드.
    /// </summary>
    protected abstract void HandleDamagedReaction();

    /// <summary>
    /// 플레이어가 감지되었거나 피격 반응 시 호출되는 추상 메서드.
    /// </summary>
    protected abstract void HandlePlayerInteraction();

    /// <summary>
    /// 플레이어와 상호작용 (추격/도망/공격) 상태를 초기화하고 쿨다운 상태로 전환합니다.
    /// </summary>
    protected void ResetPlayerActionState()
    {
        _isActingOnPlayer = false;
        _isPlayerDetected = false;
        _isAttacking = false;
        _playerTransform = null;

        _isOnActionCooldown = true;
        _currentActionCooldownTimer = fishData.chaseCooldown;

        velocity = Vector2.zero; // 상태 초기화 후 잠시 멈춤
    }

    // NeutralFish에서 더 이상 사용되지 않습니다.
    protected virtual void NeutralWatchPlayerRotation() { }

    /// <summary>
    /// 군집 행동 및 경계 검사를 수행하는 도우미 메서드.
    /// </summary>
    protected void PerformFlockingAndBoundaryChecks()
    {
        var fishColliders = Physics2D.OverlapCircleAll(transform.position, fishData.flockNeighborhoodRadius);
        var neighboringFish = fishColliders.Select(o => o.GetComponent<Fish>())
                                .Where(f => f != null && f != this && f.transform.parent == this.transform.parent)
                                .ToList();

        Flock(neighboringFish);
        ObstacleAvoidance();
        CircularBoundaryAvoidance();
        RectangleBoundaryAvoidance();
    }





    // --- 기존의 군집, 회피, 이동 관련 메서드들 (protected로 변경) ---
    protected void Flock(IEnumerable<Fish> fishAgents)
    {
        Vector2 alignmentForce = Alignment(fishAgents);
        Vector2 cohesionForce = Cohesion(fishAgents);
        Vector2 separationForce = Separation(fishAgents);

        acceleration += separationForce * fishData.flockSeparationWeight;
        acceleration += cohesionForce * fishData.flockCohesionWeight;
        acceleration += alignmentForce * fishData.flockAlignmentWeight;
    }

    protected void ObstacleAvoidance()
    {
        Vector2 currentForward = velocity.normalized;
        Vector2 leftRayDir = Quaternion.Euler(0, 0, 30) * currentForward;
        Vector2 rightRayDir = Quaternion.Euler(0, 0, -30) * currentForward;

        RaycastHit2D frontHit = Physics2D.CircleCast(transform.position, 0.2f, currentForward, fishData.raycastLength, obstacleLayer);
        RaycastHit2D leftHit = Physics2D.CircleCast(transform.position, 0.2f, leftRayDir, fishData.raycastLength, obstacleLayer);
        RaycastHit2D rightHit = Physics2D.CircleCast(transform.position, 0.2f, rightRayDir, fishData.raycastLength, obstacleLayer);

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
            acceleration += Steer(steerForce.normalized * fishData.normalSpeed) * fishData.obstacleAvoidanceWeight;
        }
    }

    protected void CircularBoundaryAvoidance()
    {
        if (_flockingBoundsRadius <= 0) return;
        if (fishData == null) return;

        float distanceFromCenter = Vector2.Distance(transform.position, _flockingBoundsCenter);

        if (distanceFromCenter >= _flockingBoundsRadius - fishData.boundaryMargin)
        {
            Vector2 desiredDirection = (_flockingBoundsCenter - (Vector2)transform.position).normalized;
            Vector2 steerForce = Steer(desiredDirection * fishData.normalSpeed);

            float strength = Mathf.Clamp01((distanceFromCenter - (_flockingBoundsRadius - fishData.boundaryMargin)) / fishData.boundaryMargin);
            acceleration += steerForce * fishData.boundsAvoidanceWeight * strength;
        }
    }

    protected void RectangleBoundaryAvoidance()
    {
        if (currentBiome == null || MapManager.Instance == null) return;

        Vector3 currentPos = transform.position;
        Vector3 biomeWorldCenter = MapManager.Instance.transform.position + currentBiome.center;
        Vector3 biomeSize = currentBiome.size;

        Vector3 minBounds = biomeWorldCenter - biomeSize / 2f;
        Vector3 maxBounds = biomeWorldCenter + biomeSize / 2f;

        Vector2 desiredDirection = Vector2.zero;
        bool outsideBoundary = false;

        if (currentPos.x < minBounds.x)
        {
            desiredDirection += Vector2.right;
            outsideBoundary = true;
        }
        else if (currentPos.x > maxBounds.x)
        {
            desiredDirection += Vector2.left;
            outsideBoundary = true;
        }

        if (currentPos.y < minBounds.y)
        {
            desiredDirection += Vector2.up;
            outsideBoundary = true;
        }
        else if (currentPos.y > maxBounds.y)
        {
            desiredDirection += Vector2.down;
            outsideBoundary = true;
        }

        if (outsideBoundary)
        {
            desiredDirection.Normalize();
            Vector2 steerForce = Steer(desiredDirection * fishData.normalSpeed);

            float closestX = Mathf.Clamp(currentPos.x, minBounds.x, maxBounds.x);
            float closestY = Mathf.Clamp(currentPos.y, minBounds.y, maxBounds.y);
            Vector2 closestPointInBiome = new Vector2(closestX, closestY);

            float distOutsideBoundary = Vector2.Distance(currentPos, closestPointInBiome);
            float strength = Mathf.Clamp01(distOutsideBoundary / fishData.boundaryMargin);
            strength = Mathf.Max(strength, 0.1f);

            acceleration += steerForce * fishData.boundsAvoidanceWeight * strength;
        }
    }

    protected void UpdateVelocity()
    {
        velocity += acceleration * Time.deltaTime;

        float minCurrentSpeed = fishData.normalSpeed * 0.4f;
        if (velocity.magnitude < minCurrentSpeed)
        {
            velocity = velocity.normalized * minCurrentSpeed;
        }

        velocity = LimitMagnitude(velocity, fishData.normalSpeed);
    }

    protected void UpdatePosition()
    {
        Vector3 newPosition = transform.position + (Vector3)velocity * Time.deltaTime;
        newPosition.z = 0f;
        transform.position = newPosition;
    }

    protected void UpdateRotation()
    {
        if (velocity.sqrMagnitude < 0.001f || fishData == null) return;

        float targetAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, fishData.rotationSpeed * Time.deltaTime);
    }

    protected Vector2 Alignment(IEnumerable<Fish> fishAgents)
    {
        if (!fishAgents.Any()) return Vector2.zero;
        Vector2 averageVelocity = Vector2.zero;
        foreach (var f in fishAgents) { averageVelocity += f.velocity; }
        averageVelocity /= fishAgents.Count();
        return Steer(averageVelocity.normalized * fishData.normalSpeed);
    }

    protected Vector2 Cohesion(IEnumerable<Fish> fishAgents)
    {
        if (!fishAgents.Any()) return Vector2.zero;
        Vector2 centerOfMass = Vector2.zero;
        foreach (var f in fishAgents) { centerOfMass += (Vector2)f.transform.position; }
        centerOfMass /= fishAgents.Count();
        return Steer((centerOfMass - (Vector2)transform.position).normalized * fishData.normalSpeed);
    }

    protected Vector2 Separation(IEnumerable<Fish> fishAgents)
    {
        var closeFish = fishAgents.Where(f => Vector2.Distance(transform.position, f.transform.position) <= fishData.flockSeparationRadius).ToList();
        if (!closeFish.Any()) return Vector2.zero;
        Vector2 repulsionForce = Vector2.zero;
        foreach (var f in closeFish)
        {
            Vector2 diff = (Vector2)transform.position - (Vector2)f.transform.position;
            repulsionForce += diff.normalized / Mathf.Max(0.001f, diff.magnitude * diff.magnitude);
        }
        repulsionForce /= closeFish.Count;
        return Steer(repulsionForce.normalized * fishData.normalSpeed);
    }

    protected Vector2 Steer(Vector2 desired)
    {
        Vector2 steerForce = desired - velocity;
        return LimitMagnitude(steerForce, fishData.flockMaxForce);
    }

    protected Vector2 LimitMagnitude(Vector2 vector, float max)
    {
        return vector.sqrMagnitude > max * max ? vector.normalized * max : vector;
    }

    // --- Gizmos for Debugging ---
    protected virtual void OnDrawGizmosSelected()
    {
        if (fishData == null) return;

        if (Application.isPlaying && parentBoid != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_flockingBoundsCenter, _flockingBoundsRadius);
            Gizmos.DrawWireSphere(_flockingBoundsCenter, _flockingBoundsRadius - fishData.boundaryMargin);
        }

        if (Application.isPlaying && currentBiome != null && MapManager.Instance != null)
        {
            Gizmos.color = currentBiome.GetGizmoColor();
            Vector3 biomeWorldCenter = MapManager.Instance.transform.position + currentBiome.center;
            Gizmos.DrawWireCube(biomeWorldCenter, currentBiome.size);
        }

        float currentNeighborhoodRadius = fishData.flockNeighborhoodRadius;
        float currentSeparationRadius = fishData.flockSeparationRadius;
        float currentRaycastLength = fishData.raycastLength;

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, currentNeighborhoodRadius);
        Gizmos.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
        Gizmos.DrawWireSphere(transform.position, currentSeparationRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)velocity);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)acceleration * 10f);

        if (fishData != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, fishData.playerDetectionRange);
            Vector2 forwardDir = velocity.normalized;
            if (forwardDir.sqrMagnitude < 0.001f) forwardDir = transform.right;

            float detectionAngleHalf = 60f;
            Vector3 leftLimit = Quaternion.Euler(0, 0, detectionAngleHalf) * forwardDir * fishData.playerDetectionRange;
            Vector3 rightLimit = Quaternion.Euler(0, 0, -detectionAngleHalf) * forwardDir * fishData.playerDetectionRange;

            Gizmos.DrawLine(transform.position, transform.position + leftLimit);
            Gizmos.DrawLine(transform.position, transform.position + rightLimit);
        }

#if UNITY_EDITOR
        if (Application.isPlaying && fishData != null)
        {
            var fishColliders = Physics2D.OverlapCircleAll(transform.position, fishData.flockNeighborhoodRadius);
            var neighboringFish = fishColliders.Select(o => o.GetComponent<Fish>()).Where(f => f != null && f != this && f.transform.parent == this.transform.parent).ToList();

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
