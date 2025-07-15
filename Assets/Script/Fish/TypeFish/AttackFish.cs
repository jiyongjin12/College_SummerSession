using UnityEngine;

public class AttackFish : Fish
{ // 특수 처리
    // HandlePlayerInteraction: 플레이어를 감지했을 때의 행동 (추격 및 공격)
    protected override void HandlePlayerInteraction()
    {
        // 플레이어 트랜스폼이 없거나 FishData가 없으면 행동 초기화
        if (_playerTransform == null || fishData == null)
        {
            ResetPlayerActionState();
            return;
        }

        // 플레이어를 향하는 방향 계산
        Vector2 directionToPlayer = (_playerTransform.position - transform.position).normalized;

        // 플레이어를 향해 돌진하는 가속도 계산
        // fishData.actionSpeedMultiplier를 곱하여 평소보다 빠르게 이동하도록 유도
        Vector2 desiredVelocity = directionToPlayer * fishData.normalSpeed * fishData.actionSpeedMultiplier;
        currentAcceleration = Steer(desiredVelocity, currentVelocity, fishData.flockMaxForce);

        // 플레이어와 거리가 공격 범위 이내이고, 공격 쿨다운이 끝났으면 공격 실행
        if (Vector2.Distance(transform.position, _playerTransform.position) <= fishData.attackRange && _currentAttackCooldownTimer <= 0)
        {
            PerformAttack(); // 공격 메서드 호출
            _currentAttackCooldownTimer = fishData.attackCooldown; // 공격 쿨다운 시작
        }
    }

    // PerformAttack: 실제 공격 로직을 처리하는 프라이빗 메서드
    private void PerformAttack()
    {
        // TODO: 여기에 플레이어에게 데미지를 주는 실제 로직을 구현합니다.
        // 예: PlayerHealthManager.Instance.TakeDamage(fishData.damage);
        Debug.Log($"{gameObject.name} attacks Player for {fishData.attackDamage} damage!");
        // 필요하다면 공격 애니메이션, 사운드, 파티클 이펙트 등을 재생할 수 있습니다.
    }

    // TakeDamage: 외부로부터 데미지를 받았을 때 호출되는 메서드
    public override void TakeDamage(Transform damageDealer, float damage)
    {
        base.TakeDamage(damageDealer, damage); // 부모 클래스의 체력 감소 및 즉시 감지 호출

        // 물고기가 죽지 않았고, 이미 피격 반응 중이 아니면 즉시 피격 반응 상태로 전환
        // 공격 물고기는 피격 시에도 플레이어에게 반격하는 경향을 보입니다.
        if (!isDie && !IsDamagedReacting)
        {
            ImmediateDetection(damageDealer);
        }
    }

    // HandleDamagedReaction: 피격 시 반응 로직 (공격 물고기는 피격 시 반격)
    protected override void HandleDamagedReaction()
    {
        // 공격 물고기는 피격 시에도 플레이어를 추적하고 공격하므로,
        // 플레이어 상호작용 로직(HandlePlayerInteraction)을 그대로 재활용합니다.
        HandlePlayerInteraction();
    }
}
