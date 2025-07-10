using UnityEngine;

public class EscapeFish : Fish
{
    protected override void Update()
    {
        base.Update();

        // Fish.cs의 Update()에서 이미 모든 상태 (_isActingOnPlayer, _isOnActionCooldown, _isDamagedReacting)를
        // 처리하고 있으므로, 여기서는 추가적인 Update 로직이 거의 필요 없습니다.
        // 특정 EscapeFish만의 고유한 로직이 필요하다면 여기에 추가합니다.
    }

    protected override bool DetectPlayer()
    {
        // 변수명 변경: _isActingOnPlayer -> IsActingOnPlayer (프로퍼티)
        // _isOnActionCooldown -> IsOnActionCooldown (프로퍼티)
        // _isDamagedReacting -> IsDamagedReacting (프로퍼티)
        if (IsActingOnPlayer || IsOnActionCooldown || IsDamagedReacting) return false;

        // 변수명 변경: velocity -> currentVelocity
        Vector2 forward = currentVelocity.normalized;
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
                    if (hitCheck.collider != null && hitCheck.collider.transform != hit.transform)
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

    protected override void HandlePlayerDetection()
    {
        base.HandlePlayerDetection(); // Fish.cs의 공통 감지 로직 호출 (_isPlayerDetected = true, velocity = zero)
        // 변수명 변경: _isActingOnPlayer -> IsActingOnPlayer (프로퍼티)
        IsActingOnPlayer = true; // 감지 즉시 도망 행동 시작
        _currentActionTimer = fishData.chaseDuration; // 도망 시간 설정
        Debug.Log($"{gameObject.name}: Player Detected! Transitioning to Escape state.");
    }

    public override void TakeDamage(Transform damageDealer, float damage)
    {
        base.TakeDamage(damageDealer, damage);
        //Debug.Log($"{gameObject.name} (EscapeFish) received damage from {damageDealer.name}. Initiating escape!");
        ImmediateDetection(damageDealer);
    }

    protected override void HandleDamagedReaction()
    {
        // 변수명 변경: _isPlayerDetected -> _isPlayerDetected (protected 필드)
        // _isActingOnPlayer -> IsActingOnPlayer (프로퍼티)
        // _isDamagedReacting -> IsDamagedReacting (프로퍼티)
        _isPlayerDetected = true; // 피격 시에도 플레이어 인식
        IsActingOnPlayer = true; // 행동 시작
        _currentActionTimer = fishData.chaseDuration;
        IsDamagedReacting = false;

        HandlePlayerInteraction();
    }

    protected override void HandlePlayerInteraction()
    {
        if (_playerTransform == null)
        {
            ResetPlayerActionState();
            Debug.Log($"{gameObject.name} (EscapeFish): Player disappeared, returning to flocking.");
            return;
        }

        Vector2 directionFromPlayer = ((Vector2)transform.position - (Vector2)_playerTransform.position).normalized;
        Vector2 desiredVelocity = directionFromPlayer * fishData.normalSpeed * fishData.actionSpeedMultiplier;

        // Steer 메서드의 시그니처에 맞게 인자 추가
        // acceleration 변수명 변경: acceleration -> currentAcceleration
        currentAcceleration += Steer(desiredVelocity, currentVelocity, fishData.flockMaxForce);

        // Job System에서 처리되므로 ObstacleAvoidance() 및 RectangleBoundaryAvoidance() 직접 호출 제거
        // ObstacleAvoidance();
        // RectangleBoundaryAvoidance(); // Job System에서 처리

        Debug.DrawLine(transform.position, (Vector3)_playerTransform.position, Color.red);
    }
}
