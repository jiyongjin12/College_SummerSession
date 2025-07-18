using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class Fish : MonoBehaviour
{ // ó��
    public FishData fishData;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

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

    [HideInInspector] public RaycastHit2D _raycastHitData;
    public int parentID = -1;

    [HideInInspector] public Vector2 boidSpawnAreaCenter;
    [HideInInspector] public float boidSpawnAreaRadius;

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
        HandleReDetectionCooldown(); // ��Ž�� ��ٿ� Ÿ�̸Ӹ� ���⼭ ���ҽ�ŵ�ϴ�.
        HandleAttackCooldown();

        if (isDie) return;

        if (IsDamagedReacting)
        {
            HandleDamagedReaction();
            UpdateVelocity();
        }
        else if (IsActingOnPlayer) // �÷��̾�� ��ȣ�ۿ� �� (����, ����, ���� ��)
        {
            HandlePlayerInteraction();

            // Interaction�� ������ ResetPlayerActionState()�� ȣ��ǰ�,
            // �� �ȿ��� IsActingOnPlayer = false; �� IsOnReDetectionCooldown = true; �� �����˴ϴ�.
            _currentActionTimer -= Time.deltaTime;
            if (_currentActionTimer <= 0)
            {
                ResetPlayerActionState(); // ���⼭ ��Ž�� ��ٿ��� ���۵˴ϴ�.
            }
            UpdateVelocity();
        }
        else // �Ϲ����� ����/��Ȳ ���� (�÷��̾�� ��ȣ�ۿ� ���� �ƴ� ��)
        {
            if (!IsOnReDetectionCooldown) // ��Ž�� ��ٿ��� �ƴ� ���� �÷��̾ �����մϴ�.
            {
                _isPlayerDetected = DetectPlayer();
                if (_isPlayerDetected)
                {
                    HandlePlayerDetection();
                }
            }
            UpdateVelocity(); // ���� �ý��� ���ӵ��� FishSimulationManager���� ���Ǿ� �� velocity�� ������ �ݴϴ�.
        }

        UpdatePosition();
        PerformRaycastForObstacles();
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
            raycastDirection = transform.right;
        }
        _raycastHitData = Physics2D.Raycast(transform.position, raycastDirection, fishData.raycastLength, obstacleLayer);
    }

    protected virtual bool DetectPlayer()
    {
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, fishData.playerDetectionRange, playerLayer);

        if (playerCollider != null)
        {
            Vector2 directionToPlayer = (playerCollider.transform.position - transform.position).normalized;
            float angleToPlayer = Vector2.Angle(transform.right, directionToPlayer);

            if (angleToPlayer <= fishData.fieldOfView / 2f)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, fishData.playerDetectionRange, obstacleLayer);
                if (hit.collider == null)
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
        if (fishData.health <= 0) return;

        fishData.health -= damage;

        if (fishData.health <= 0 && !isDie)
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
        //currentVelocity = Vector2.zero;
        //currentAcceleration = Vector2.zero;
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
        //currentAcceleration = Vector2.zero;
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

    // --- ����� ����� �ð�ȭ ---
    protected virtual void OnDrawGizmosSelected()
    {
        if (fishData != null)
        {
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
            // ���� �κ�: Vector3 * Vector3 ��� Vector3 * float �� ����
            Gizmos.DrawLine(transform.position, transform.position + rightRayDirection * fishData.playerDetectionRange);         

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, fishData.flockNeighborhoodRadius);

            Vector2 raycastDirection = currentVelocity.normalized;
            if (raycastDirection.sqrMagnitude < 0.001f)
            {
                raycastDirection = transform.right;
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, (Vector3)raycastDirection * fishData.raycastLength);

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
