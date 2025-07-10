using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class Fish : MonoBehaviour
{ // 처리
  // 처리
    public FishData fishData;
    public Boid parentBoid;
    public Biome currentBiome;
    public bool isDie;
    //임시 HP
    [SerializeField] public float curHP;

    protected Transform _playerTransform;
    // protected 멤버 대신 public 프로퍼티 추가
    protected bool _isPlayerDetected = false;
    protected bool _isAttacking = false;

    // FishSimulationManager에서 접근하기 위한 public 프로퍼티
    public bool IsActingOnPlayer { get; protected set; } = false;
    public bool IsOnActionCooldown { get; protected set; } = false;
    public bool IsDamagedReacting { get; protected set; } = false;

    public float _currentActionTimer = 0f;
    private float _currentActionCooldownTimer = 0f;
    protected float _currentAttackCooldownTimer = 0f;

    public bool FlockingSystemONOFF_Test = false;

    // Job 시스템으로 계산될 가속도와 속도
    [HideInInspector] public Vector2 currentAcceleration;
    [HideInInspector] public Vector2 currentVelocity;

    public LayerMask obstacleLayer;
    public LayerMask playerLayer;

    // 군집 경계는 Job Input으로 전달됩니다.
    private Vector2 _flockingBoundsCenter;
    private float _flockingBoundsRadius;

    public void SetFlockingBounds(Vector2 center, float radius)
    {
        _flockingBoundsCenter = center;
        _flockingBoundsRadius = radius;
    }

    // FishSimulationManager가 접근할 수 있도록 Get 메서드 추가
    public Vector2 GetFlockingBoundsCenter() => _flockingBoundsCenter;
    public float GetFlockingBoundsRadius() => _flockingBoundsRadius;


    protected virtual void Awake()
    {
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
        curHP = fishData.health;

        float initialSpeed = fishData != null ? fishData.normalSpeed : 3f;
        float angle = UnityEngine.Random.Range(0, 2 * Mathf.PI); // UnityEngine.Random 명시
        currentVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * initialSpeed;
        transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
    }

    protected virtual void Update()
    {
        currentAcceleration = Vector2.zero;

        if (fishData == null || isDie) return;

        if (_currentAttackCooldownTimer > 0)
        {
            _currentAttackCooldownTimer -= Time.deltaTime;
        }

        // 플레이어와의 상호작용 로직은 여전히 개별 Fish 스크립트에서 메인 스레드에서 처리됩니다.
        if (IsDamagedReacting)
        {
            HandleDamagedReaction();
        }
        else if (IsActingOnPlayer)
        {
            _currentActionTimer -= Time.deltaTime;

            if (_currentActionTimer <= 0f)
            {
                ResetPlayerActionState();
            }
            else
            {
                HandlePlayerInteraction();
            }
        }
        else if (IsOnActionCooldown)
        {
            _currentActionCooldownTimer -= Time.deltaTime;

            if (_currentActionCooldownTimer <= 0f)
            {
                IsOnActionCooldown = false;
            }
        }
        else // 기본 상태: 플레이어 감지 시도 및 군집 행동 (Job 시스템이 군집 행동을 담당)
        {
            bool playerFound = DetectPlayer();
            if (playerFound)
            {
                HandlePlayerDetection();
            }
        }

        UpdateVelocity();
        UpdatePosition();
        UpdateRotation();
    }

    protected virtual bool DetectPlayer() { return false; }

    protected virtual void HandlePlayerDetection()
    {
        _isPlayerDetected = true;
        currentVelocity = Vector2.zero;
    }

    public virtual void TakeDamage(Transform damageDealer, float damage)
    {
        Debug.Log($"DAMAGE : {damage}!!!");
        curHP -= damage;
        if (curHP <= 0) { Debug.Log("die"); isDie = true; }
    }

    protected virtual void ImmediateDetection(Transform damageDealer)
    {
        if (!IsActingOnPlayer && !IsOnActionCooldown && damageDealer.CompareTag("Player"))
        {
            IsDamagedReacting = true; // 프로퍼티 사용
            _playerTransform = damageDealer;
            currentVelocity = Vector2.zero;
        }
    }

    protected abstract void HandleDamagedReaction();

    protected abstract void HandlePlayerInteraction();

    protected void ResetPlayerActionState()
    {
        IsActingOnPlayer = false; // 프로퍼티 사용
        _isPlayerDetected = false;
        _isAttacking = false;
        _playerTransform = null;

        IsOnActionCooldown = true; // 프로퍼티 사용
        _currentActionCooldownTimer = fishData.chaseCooldown;

        currentVelocity = Vector2.zero;
    }

    protected virtual void NeutralWatchPlayerRotation() { }

    protected void UpdateVelocity()
    {
        currentVelocity += currentAcceleration * Time.deltaTime;

        float currentMaxSpeed = fishData.normalSpeed;
        if (IsActingOnPlayer)
        {
            currentMaxSpeed *= fishData.actionSpeedMultiplier;
        }

        currentVelocity = LimitMagnitude(currentVelocity, currentMaxSpeed);
    }

    protected void UpdatePosition()
    {
        Vector3 newPosition = transform.position + (Vector3)currentVelocity * Time.deltaTime;
        newPosition.z = 0f;
        transform.position = newPosition;
    }

    protected void UpdateRotation()
    {
        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;

            float currentRotationSpeed = fishData.rotationSpeed;
            if (IsActingOnPlayer)
            {
                currentRotationSpeed *= fishData.actionSpeedMultiplier;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, targetAngle), Time.deltaTime * currentRotationSpeed);
        }
    }

    protected Vector2 Steer(Vector2 desired, Vector2 currentVel, float maxForce)
    {
        Vector2 steerForce = desired - currentVel;
        return LimitMagnitude(steerForce, maxForce);
    }

    protected Vector2 LimitMagnitude(Vector2 vector, float max)
    {
        return vector.sqrMagnitude > max * max ? vector.normalized * max : vector;
    }

    protected virtual void OnDestroy()
    {
        if (FishSimulationManager.Instance != null)
        {
            FishSimulationManager.Instance.UnregisterFish(this);
        }
    }

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

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, currentNeighborhoodRadius);
        Gizmos.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
        Gizmos.DrawWireSphere(transform.position, currentSeparationRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)currentVelocity);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)currentAcceleration * 10f);

        if (fishData != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, fishData.playerDetectionRange);
            Vector2 forwardDir = currentVelocity.normalized;
            if (forwardDir.sqrMagnitude < 0.001f) forwardDir = transform.right;

            float detectionAngleHalf = 60f;
            Vector3 leftLimit = Quaternion.Euler(0, 0, detectionAngleHalf) * forwardDir * fishData.playerDetectionRange;
            Vector3 rightLimit = Quaternion.Euler(0, 0, -detectionAngleHalf) * forwardDir * fishData.playerDetectionRange;

            Gizmos.DrawLine(transform.position, transform.position + leftLimit);
            Gizmos.DrawLine(transform.position, transform.position + rightLimit);
        }
    }
}
