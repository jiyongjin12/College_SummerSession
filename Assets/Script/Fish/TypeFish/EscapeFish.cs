using UnityEngine;

public class EscapeFish : Fish
{
    // HandlePlayerInteraction: 플레이어를 감지했을 때의 행동 (무조건 도망)
    protected override void HandlePlayerInteraction()
    {
        if (_playerTransform == null || fishData == null)
        {
            ResetPlayerActionState(); // 플레이어 없으면 행동 초기화
            return;
        }

        // 플레이어로부터 멀어지는 방향으로 가속도 계산
        // fishData.actionSpeedMultiplier를 곱하여 평소보다 빠르게 도망가도록 유도
        Vector2 desiredVelocity = (transform.position - _playerTransform.position).normalized * fishData.normalSpeed * fishData.actionSpeedMultiplier;
        currentAcceleration = Steer(desiredVelocity, currentVelocity, fishData.flockMaxForce);
    }

    // TakeDamage: 외부로부터 데미지를 받았을 때 호출되는 메서드
    public override void TakeDamage(Transform damageDealer, float damage)
    {
        base.TakeDamage(damageDealer, damage); // 부모 클래스의 체력 감소 및 즉시 감지 호출

        // 물고기가 죽지 않았다면 피격 시 즉시 도망 상태로 전환
        // 도망 물고기는 피격 시에도 도망칩니다.
        if (!isDie)
        {
            ImmediateDetection(damageDealer);
        }
    }

    // HandleDamagedReaction: 피격 시 반응 로직 (도망 물고기는 피격 시에도 도망)
    protected override void HandleDamagedReaction()
    {
        // 도망 물고기는 피격 시에도 플레이어로부터 도망치므로,
        // 플레이어 상호작용 로직(HandlePlayerInteraction)을 그대로 재활용합니다.
        HandlePlayerInteraction();

        // 일정 거리 이상 도망가면 피격 반응 종료 (군집 행동으로 돌아감)
        if (_playerTransform != null && Vector2.Distance(transform.position, _playerTransform.position) > fishData.playerDetectionRange * 2f)
        {
            ResetPlayerActionState();
        }
    }
}
