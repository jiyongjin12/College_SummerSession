using UnityEngine;

public class NeutralFish : Fish
{
    private const float STARE_DURATION_TO_ATTACK = 4.0f;

    // NeutralFish만의 응시 관련 변수
    private bool _isStaringAtPlayer = false;
    private float _currentStareTimer = 0f;

    // HandlePlayerInteraction: 플레이어를 감지했을 때의 행동
    protected override void HandlePlayerInteraction()
    {
        if (_playerTransform == null || fishData == null)
        {
            ResetPlayerActionState();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);

        // 플레이어가 감지 범위를 벗어나면 상호작용 종료
        if (distanceToPlayer > fishData.playerDetectionRange * 1.1f)
        {
            ResetPlayerActionState();
            return;
        }

        // --- 응시 로직 ---
        if (!_isAttacking) // 이미 공격 중이 아니라면 응시 로직 수행
        {
            _isStaringAtPlayer = true;
            currentVelocity = Vector2.zero; // 멈춤
            currentAcceleration = Vector2.zero; // 가속도도 0

            // 플레이어 바라보기
            Vector2 directionToPlayer = (_playerTransform.position - transform.position).normalized;
            float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, fishData.rotationSpeed * Time.deltaTime);

            // 응시 타이머 증가
            _currentStareTimer += Time.deltaTime;

            // 4초 이상 응시했으면 공격 시작
            if (_currentStareTimer >= STARE_DURATION_TO_ATTACK)
            {
                Debug.Log($"{gameObject.name}이(가) 플레이어를 {STARE_DURATION_TO_ATTACK}초 이상 응시하여 공격을 시작합니다!");
                _isAttacking = true; // 공격 상태 플래그 설정
                _currentActionTimer = fishData.chaseDuration; // 공격 지속 시간 설정 (공격 지속 시간 이후 ResetPlayerActionState 호출)
                _currentStareTimer = 0f; // 응시 타이머 리셋
                _isStaringAtPlayer = false; // 응시 상태 해제

                // 플레이어에게 돌진하는 가속도 설정
                Vector2 desiredVelocity = directionToPlayer * fishData.normalSpeed * fishData.actionSpeedMultiplier;
                currentAcceleration = Steer(desiredVelocity, currentVelocity, fishData.flockMaxForce);
            }
        }

        // --- 공격 중 로직 ---
        if (_isAttacking)
        {
            // 공격 중에는 플레이어를 향해 계속 돌진
            Vector2 attackDirection = (_playerTransform.position - transform.position).normalized;
            Vector2 desiredVelocity = attackDirection * fishData.normalSpeed * fishData.actionSpeedMultiplier;
            currentAcceleration = Steer(desiredVelocity, currentVelocity, fishData.flockMaxForce);

            // _currentActionTimer는 Fish.Update()에서 이미 감소되고 있으므로 여기서 다시 감소시킬 필요 없음.
            // 하지만 공격이 시작될 때 _currentActionTimer를 설정했으므로, 그 타이머가 0이 되면 ResetPlayerActionState가 호출될 것임.
            // 여기서는 공격 로직만 정의하고, 종료는 Fish.Update()에 맡김.
        }
    }

    // ResetPlayerActionState 오버라이드: NeutralFish 고유 변수들도 초기화
    protected override void ResetPlayerActionState()
    {
        base.ResetPlayerActionState(); // 부모 클래스의 초기화 로직 호출
        _isStaringAtPlayer = false;
        _currentStareTimer = 0f;
    }


    // TakeDamage: 외부로부터 데미지를 받았을 때 호출되는 메서드
    public override void TakeDamage(Transform damageDealer, float damage)
    {
        base.TakeDamage(damageDealer, damage);

        if (!isDie)
        {
            ImmediateDetection(damageDealer);
        }
    }

    // HandleDamagedReaction: 피격 시 반응 로직 (중립 물고기는 피격 시 도망)
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
    }
}
