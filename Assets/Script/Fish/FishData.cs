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
    public FishType fishType; // 물고기 종류 (예: 일반, 희귀, 전설, 보스) && 사이즈 (예: 소형 중형 대형)으로 바꿔도 될지도 
    public List<FishHabitat> habitats; // 서식지 (노말, 동굴, 잔해, 산호초)
    public float minDepth; // 최소 출현 수심
    public float maxDepth; // 최대 출현 수심

    [Header("물고기 능력치")]
    public float health; // 물고기 체력
    public float speed; // 물고기 이동 속도
    public float escapeSpeedMultiplier; // 도망칠 때 속도 배율
    public float attackPower; // 공격력 (공격하는 물고기)

    [Header("행동 패턴")]
    public FishBehaviorType behaviorType; // 행동 타입 (도망침, 공격, 중립)  *
     [Tooltip("플레이어 감지 범위")]
    public float detectionRange; // 플레이어 감지 범위

    [Header("군집")]
    public bool useBoids; // 군집 시스템 사용 여부
    
     [Tooltip("활동 범위")]
    public float scopeOfActivity; // 활동범위 
    public int fishUnitCount; // 무리의 수  *?


    [Header("자원 및 보상")]
    public int baseValue; // 기본 판매 가격

    [Header("도감 정보")]
    [TextArea(3, 5)]
    public string description; // 물고기 설명 (도감에 표시)
    public Sprite fishIcon; // 도감에 표시될 물고기 아이콘
}

public enum FishType
{
    Common,
    Uncommon,
    Rare,
    Legendary,
    Boss
}

public enum FishHabitat
{
    Normal,
    Cave,
    Wreckage,
    CoralReef
}

public enum FishBehaviorType
{
    Flee, // 도망침
    Aggressive, // 공격적
    Neutral // 중립적
}
