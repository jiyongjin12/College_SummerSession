using UnityEngine;

public class NeutralFish : Fish
{
    private const float STARE_DURATION_TO_ATTACK = 4.0f;

    private bool _isStaringAtPlayer = false;
    private float _currentStareTimer = 0f;

    protected override void Update()
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

    protected override void HandlePlayerInteraction()
    {
        if (_playerTransform == null || fishData == null)
        {
            ResetPlayerActionState();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer > fishData.playerDetectionRange * 1.1f)
        {
            ResetPlayerActionState();
            return;
        }

        // 응시 로직
        if (!_isAttacking) // 이미 공격 중이 아니면 응시 로직 수행함
        {
            _isStaringAtPlayer = true;
            currentVelocity = Vector2.zero; // 멈춤
            currentAcceleration = Vector2.zero; // 가속도 0

            // 플레이어 바라보기 (transform.rotation 사용함)
            Vector2 directionToPlayer = (_playerTransform.position - transform.position).normalized;
            float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, fishData.rotationSpeed * Time.deltaTime);

            // 스프라이트 flipX는 방향에 맞게 설정함
            if (spriteRenderer != null)
            {
                // 수정: 플레이어 방향에 따라 flipX 설정함 (오른쪽이면 true)
                spriteRenderer.flipX = directionToPlayer.x > 0;
            }
            // 상하 기울기는 0으로 고정함
            transform.localEulerAngles = new Vector3(0, 0, transform.localEulerAngles.z);

            // 응시 타이머 증가함
            _currentStareTimer += Time.deltaTime;

            // 4초 이상 응시하면 공격 시작함
            if (_currentStareTimer >= STARE_DURATION_TO_ATTACK)
            {
                Debug.Log($"{gameObject.name}이(가) 플레이어를 {STARE_DURATION_TO_ATTACK}초 이상 응시하여 공격을 시작합니다!");
                _isAttacking = true;
                _currentActionTimer = fishData.chaseDuration;
                _currentStareTimer = 0f;
                _isStaringAtPlayer = false;

                // 플레이어에게 돌진하는 가속도 설정함
                Vector2 desiredVelocity = directionToPlayer * fishData.normalSpeed * fishData.actionSpeedMultiplier;
                currentAcceleration = Steer(desiredVelocity, currentVelocity, fishData.flockMaxForce);
            }
        }

        // 공격 중 로직
        if (_isAttacking)
        {
            // 공격 중에는 플레이어를 향해 계속 돌진함
            Vector2 attackDirection = (_playerTransform.position - transform.position).normalized;
            Vector2 desiredVelocity = attackDirection * fishData.normalSpeed * fishData.actionSpeedMultiplier;
            currentAcceleration = Steer(desiredVelocity, currentVelocity, fishData.flockMaxForce);

            // 공격 중에는 스프라이트 반전 및 상하 기울기 로직 적용함
            UpdateVisualOrientation();
        }
    }

    protected override void ResetPlayerActionState()
    {
        base.ResetPlayerActionState();
        _isStaringAtPlayer = false;
        _currentStareTimer = 0f;
        // 상태 초기화 시 Z축 회전도 기본값으로 돌려놓음
        transform.localEulerAngles = new Vector3(0, 0, 0);
    }

    public override void TakeDamage(Transform damageDealer, float damage)
    {
        base.TakeDamage(damageDealer, damage);

        if (!isDie)
        {
            ImmediateDetection(damageDealer);
        }
    }

    protected override void HandleDamagedReaction()
    {
        if (_playerTransform == null)
        {
            ResetPlayerActionState();
            return;
        }

        Vector2 desiredVelocity = (transform.position - _playerTransform.position).normalized * fishData.normalSpeed * fishData.actionSpeedMultiplier;
        currentAcceleration = Steer(desiredVelocity, currentVelocity, fishData.flockMaxForce);

        if (Vector2.Distance(transform.position, _playerTransform.position) > fishData.playerDetectionRange * 2f)
        {
            ResetPlayerActionState();
        }
        UpdateVisualOrientation(); // 도망 중에는 일반 시각적 처리함
    }
}