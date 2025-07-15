using System.Collections.Generic;
using UnityEngine;

//[System.Serializable]
[CreateAssetMenu(menuName = "Fish/FishData")]
public class FishData : ScriptableObject
{
    public string fishName; // 물고기 이름 (예: 참치, 오징어, 해마)
    public int fishID; // ID
    public GameObject fishPrefab; // 물고기 외형

    [Header("생태 정보")]
    public FishType fishType; // 사이즈 (예: 소형 중형 대형)
    public List<FishHabitat> habitats; // 서식지 (노말, 동굴, 잔해, 산호초)
    public float minDepth; // 최소 출현 수심
    public float maxDepth; // 최대 출현 수심

    [Header("물고기 능력치")]
    public float health; // 물고기 체력
    public float normalSpeed; // 물고기 이동 속도

    [Header("행동 패턴")]
    public FishBehaviorType behaviorType; // 행동 타입 (도망침, 공격, 중립)  *
    [Tooltip("플레이어 감지 범위")]
    public float playerDetectionRange; // 플레이어 감지 범위
    [Range(0, 360)] public float fieldOfView = 120f; // 플레이어 감지 시야각 (부채꼴)

    [Header("개별 Fish 군집 행동 파라미터")]
    [Tooltip("활동 범위")]
    public float scopeOfActivity = 30; // 활동범위 
    public int fishUnitCount; // 무리의 수  *?

    public float flockMaxForce = 1f; // 각 Fish가 힘을 적용할 최대치
    public float flockNeighborhoodRadius = 3f; // 이웃 탐색 범위
    public float flockSeparationRadius = 1.3f; // 이웃 응집 거리

    [Range(0f, 20f)] public float flockSeparationWeight = 2f; // 분리 힘의 가중치
    [Range(0f, 20f)] public float flockCohesionWeight = 1f;    // 결집 힘의 가중치
    [Range(0f, 20f)] public float flockAlignmentWeight = 1f;   // 정렬 힘의 가중치

    [Tooltip("장애물 회피 레이어 마스크 (각 Fish에서 설정)")]
    public float obstacleAvoidanceWeight = 20f; // 장애물 회피 힘의 가중치
    public float raycastLength = 1f; // 장애물 감지 Raycast 길이
    public float rotationSpeed = 360f; // 초당 회전 각도

    [Tooltip("경계 회피 마진 (각 Fish에서 설정)")]
    public float boundaryMargin = 2.5f; // 경계로부터 이만큼 떨어져 있을 때부터 회피 시작
    public float boundsAvoidanceWeight = 5f; // 경계 회피 힘의 가중치

    [Header("공격/도망/중립 파라미터")]
    public float actionSpeedMultiplier = 1.5f; // 행동 시 속도 가중치 (추격/도망/공격 이동 속도 배율)

     [Tooltip("추격 및 도망치는 시간")]
    public float chaseDuration = 5f; // 추격/도망 지속 시간
     [Tooltip("추격 및 도망 대기 시간")]
    public float chaseCooldown = 3f; // 추격 대기 시간

    [Header("공격형 물고기 전용")]
    public float attackRange = 2f; // 공격 범위 
    public float attackDamage; // 공격력 (공격하는 물고기)
    public float attackCooldown = 1f; // 공격 속도 


    [Header("자원 및 보상")]
    public float baseValue; // 기본 판매 가격

    [Header("도감 정보")]
    [TextArea(3, 5)]
    public string description; // 물고기 설명 (도감에 표시)
    public Sprite fishIcon; // 도감에 표시될 물고기 아이콘
}

public enum FishType
{
    Small,
    Medium,
    Large
}

public enum FishHabitat
{
    Normal,
    Cave,
    Wreckage,
    CoralReef,
    DontSpawnt
}

public enum FishBehaviorType
{
    Flee, // 도망침
    Aggressive, // 공격적
    Neutral // 중립적
}
