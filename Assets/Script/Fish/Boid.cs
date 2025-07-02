using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    [Header("군집 설정")]
    public FishData targetFishData;

    // TODO: 여기에 실제 Boid(군집) 로직을 추가합니다.
    // - 이 Boid 군집 내 개별 물고기들의 움직임, 정렬, 응집, 분리 등을 처리합니다.
    // - targetFishData의 speed, behaviorType 등을 참조하여 동작합니다.

    void Start()
    {
        if (targetFishData != null)
        {
            // 예: targetFishData.fishPrefab을 사용하여 실제 물고기 모델 생성
            // GameObject fishModel = Instantiate(targetFishData.fishPrefab, transform);
            // fishModel.transform.localPosition = Vector3.zero;

            // Boid의 초기 속도 설정 등
        }
        else
        {
            Debug.LogWarning("Boid has no targetFishData assigned!");
        }
    }

    // 여기에 Boid 알고리즘 (Cohesion, Alignment, Separation) 로직을 구현합니다.
    // 예를 들어:
    // private void Update()
    // {
    //     Vector3 cohesion = CalculateCohesion();
    //     Vector3 alignment = CalculateAlignment();
    //     Vector3 separation = CalculateSeparation();

    //     Vector3 finalDirection = (cohesion + alignment + separation).normalized;
    //     transform.position += finalDirection * targetFishData.speed * Time.deltaTime;
    // }

}
