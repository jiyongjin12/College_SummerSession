using UnityEngine;

public class AttackFish : Fish
{ // 특수 처리

    protected override void Update()
    {
        base.Update();

        // AttackFish는 플레이어 감지 시 추격 행동을 시작합니다.
        // Fish.cs의 Update()에서 이미 DetectPlayer()를 호출하고 HandlePlayerDetection()을 처리합니다.
        // 여기서 추가적인 DetectPlayer() 호출은 필요 없습니다.

        // AttackFish는 _isActingOnPlayer 상태에서 HandlePlayerInteraction()을 통해 추적/공격하므로
        // _isPlayerDetected (주시 상태)와 겹칠 필요가 없습니다.
    }

    // Fish.cs의 DetectPlayer()를 오버라이드하여 플레이어 감지 로직 구현
    protected override bool DetectPlayer()
    {
        // 이미 플레이어와 상호작용 중이거나 쿨다운 중, 피격 반응 중이라면 다시 감지할 필요 없음
        if (_isActingOnPlayer || _isOnActionCooldown || _isDamagedReacting) return false;

        Vector2 forward = velocity.normalized;
        if (forward.sqrMagnitude < 0.001f) forward = transform.right;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, fishData.playerDetectionRange, playerLayer);

        foreach (Collider2D hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                Vector2 directionToPlayer = (hit.transform.position - transform.position).normalized;
                float angleToPlayer = Vector2.Angle(forward, directionToPlayer);
                float detectionAngleHalf = 60f;

                if (angleToPlayer <= detectionAngleHalf)
                {
                    RaycastHit2D hitCheck = Physics2D.Raycast(transform.position, directionToPlayer, fishData.playerDetectionRange, obstacleLayer);
                    if (hitCheck.collider != null)
                    {
                        continue;
                    }

                    _playerTransform = hit.transform; // 플레이어 트랜스폼 저장
                    return true; // 플레이어 감지 성공
                }
            }
        }
        _playerTransform = null;
        return false;
    }

    // 플레이어가 감지되었을 때 공통 처리 후 AttackFish만의 추가 행동 초기화
    protected override void HandlePlayerDetection()
    {
        base.HandlePlayerDetection(); // Fish.cs의 공통 감지 로직 호출 (_isPlayerDetected = true, velocity = zero)
        _isActingOnPlayer = true; // 감지 즉시 추격 행동 시작
        _currentActionTimer = fishData.chaseDuration; // 추격 시간 설정
        //Debug.Log($"{gameObject.name}: Player Detected! Transitioning to Chase state.");
    }

    public override void TakeDamage(Transform damageDealer, float damage)
    {
        base.TakeDamage(damageDealer, damage);
        //Debug.Log($"{gameObject.name} (AttackFish) received damage from {damageDealer.name}. Initiating counter-attack!");
        ImmediateDetection(damageDealer);
    }

    protected override void HandleDamagedReaction()
    {
        _isPlayerDetected = true; // 피격 시에도 플레이어 인식
        _isActingOnPlayer = true; // 행동 시작
        _currentActionTimer = fishData.chaseDuration;
        _isDamagedReacting = false;

        HandlePlayerInteraction();
    }

    protected override void HandlePlayerInteraction()
    {
        if (_playerTransform == null)
        {
            ResetPlayerActionState();
            //Debug.Log($"{gameObject.name} (AttackFish): Player disappeared, returning to flocking.");
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= fishData.attackRange)
        {
            velocity = Vector2.zero;
            _isAttacking = true;

            if (_currentAttackCooldownTimer <= 0)
            {
                Attack();
                _currentAttackCooldownTimer = fishData.attackCooldown;
            }
        }
        else
        {
            _isAttacking = false;
            Vector2 directionToPlayer = ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized;
            Vector2 desiredVelocity = directionToPlayer * fishData.normalSpeed * fishData.actionSpeedMultiplier;
            acceleration += Steer(desiredVelocity) * fishData.boundsAvoidanceWeight;
            ObstacleAvoidance();
        }

        Debug.DrawLine(transform.position, (Vector3)_playerTransform.position, Color.green);
    }

    private void Attack()
    {
        Debug.Log($"{gameObject.name} (AttackFish) attacks Player for {fishData.attackDamage} damage!");
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (fishData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, fishData.attackRange);
        }
    }
}
