using UnityEngine;

public class NeutralFish : Fish
{
    private bool _hasAttackedOnce = false; // 피격 시 한 번의 공격 시도를 기록하는 플래그
    private bool _isWatchingPlayer = false; // 플레이어를 주시 중인 상태 (감지 후 공격 받기 전)

    protected override void Update()
    {
        base.Update();

        // 피격 반응 중이 아니거나, 다른 플레이어 행동(_isActingOnPlayer) 중이 아닐 때
        // 그리고 쿨다운 중이 아닐 때만 주시 로직을 실행합니다.
        if (!_isDamagedReacting && !_isActingOnPlayer && !_isOnActionCooldown)
        {
            if (_isPlayerDetected) // 이미 플레이어가 감지되어 주시 중인 상태
            {
                // 플레이어가 아직 시야 내에 있는지 재확인
                bool playerStillInSight = DetectPlayer(); // DetectPlayer()가 _playerTransform을 업데이트
                if (playerStillInSight)
                {
                    LookAtPlayer(); // 계속 플레이어를 바라봅니다.
                }
                else
                {
                    Debug.Log($"{gameObject.name}: Player out of sight during watch. Returning to flocking.");
                    ResetNeutralWatchState(); // 주시 상태 종료 및 원래 상태로 복귀
                }
            }
            // else: DetectPlayer()는 Fish.cs의 Update에서 호출되고, 감지되면 HandlePlayerDetection()이 호출되어
            //      _isPlayerDetected가 true로 설정됩니다.
        }
    }

    /// <summary>
    /// NeutralFish 전용 플레이어 감지 로직.
    /// </summary>
    protected override bool DetectPlayer()
    {
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
        _playerTransform = null; // 플레이어가 시야에 없으면 null로 설정
        return false;
    }

    /// <summary>
    /// 플레이어가 감지되었을 때 공통 처리 후 NeutralFish만의 추가 행동 초기화 (주시 상태로 진입).
    /// </summary>
    protected override void HandlePlayerDetection()
    {
        base.HandlePlayerDetection(); // Fish.cs의 공통 감지 로직 호출 (_isPlayerDetected = true, velocity = zero)
        _isWatchingPlayer = true; // 주시 상태 시작
        Debug.Log($"{gameObject.name}: Player Detected! Transitioning to Watch state.");
    }

    /// <summary>
    /// 플레이어를 바라보는 로직.
    /// </summary>
    private void LookAtPlayer()
    {
        if (_playerTransform != null)
        {
            Vector2 directionToPlayer = ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized;
            float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, fishData.rotationSpeed * Time.deltaTime);

            velocity = Vector2.zero; // ★ 주시 중에는 움직이지 않음 (다시 한 번 강조) ★

            Debug.DrawLine(transform.position, (Vector3)_playerTransform.position, Color.yellow);
        }
    }

    /// <summary>
    /// 중립 물고기의 주시 상태를 종료하고 일반 상태로 복귀합니다.
    /// </summary>
    private void ResetNeutralWatchState()
    {
        _isPlayerDetected = false; // 더 이상 감지 상태 아님
        _isWatchingPlayer = false; // 주시 상태 종료
        _playerTransform = null;
        // velocity는 이미 0이지만, 다음 프레임부터 UpdateVelocity에서 다시 군집 속도 계산 시작
    }

    public override void TakeDamage(Transform damageDealer)
    {
        // 피격 시에는 다른 상태(주시 포함)보다 우선적으로 반응
        if (!_isActingOnPlayer && !_isOnActionCooldown && damageDealer.CompareTag("Player"))
        {
            Debug.Log($"{gameObject.name} (NeutralFish) received damage from {damageDealer.name}. Initiating counter-attack!");
            _isDamagedReacting = true; // 피격 반응 상태로 진입
            _playerTransform = damageDealer; // 데미지를 준 오브젝트 (플레이어)
            _hasAttackedOnce = false; // 공격 플래그 초기화
            _isWatchingPlayer = false; // ★ 주시 상태 종료 (피격 시 공격 모드로 전환) ★
            _isPlayerDetected = false; // 피격 시에는 주시 상태가 아니므로 감지 플래그도 해제
            velocity = Vector2.zero; // 잠시 멈춰서 반응 시작
        }
    }

    protected override void HandleDamagedReaction()
    {
        // 피격 시에는 무조건 플레이어를 추격/공격하는 행동으로 전환
        _isPlayerDetected = true; // 데미지를 받았으니 플레이어의 존재를 '인식'했다고 가정 (감지 아님)
        _isActingOnPlayer = true;
        _currentActionTimer = fishData.chaseDuration; // 피격 시에도 추격 시간 적용
        _isDamagedReacting = false; // 반응 처리 후 플래그 해제 (이제 _isActingOnPlayer가 관리)

        HandlePlayerInteraction(); // 즉시 공격 행동 실행
    }

    protected override void HandlePlayerInteraction()
    {
        // 플레이어 트랜스폼이 유효한 경우에만 실행
        if (_playerTransform == null)
        {
            ResetPlayerActionState();
            Debug.Log($"{gameObject.name} (NeutralFish): Player disappeared during counter-attack, returning to flocking.");
            return;
        }

        // 중립 물고기는 피격 시 단 한 번만 공격을 시도합니다.
        if (!_hasAttackedOnce)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);

            if (distanceToPlayer <= fishData.attackRange)
            {
                velocity = Vector2.zero;
                _isAttacking = true;

                if (_currentAttackCooldownTimer <= 0)
                {
                    Attack();
                    _currentAttackCooldownTimer = fishData.attackCooldown;
                    _hasAttackedOnce = true;

                    ResetPlayerActionState();
                    Debug.Log($"{gameObject.name} (NeutralFish): Attacked once. Entering cooldown.");
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
        }
        else
        {
            // 이미 공격 시도했으면 더 이상 추격하지 않고, ResetPlayerActionState()에 의해 쿨다운으로 진입
        }

        Debug.DrawLine(transform.position, (Vector3)_playerTransform.position, Color.yellow);
    }

    private void Attack()
    {
        Debug.Log($"{gameObject.name} (NeutralFish) attacks Player for {fishData.attackDamage} damage!");
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (fishData != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, fishData.attackRange);
        }
    }
}
