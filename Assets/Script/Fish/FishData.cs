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
    public List<FishHabitat> habitats; // 서식지 (예: 얕은 바다, 심해, 동굴)
    public float minDepth; // 최소 출현 수심
    public float maxDepth; // 최대 출현 수심
    //public List<TimeOfDay> activeTimes; // 활동 시간 (예: 낮, 밤, 항상) 이게 필요할려나? 걍 아침에 낚시 밤에 복귀 이런식으로 할거 같은디

    [Header("물고기 능력치")]
    public float health; // 물고기 체력
    public float speed; // 물고기 이동 속도
    public float acceleration; // 물고기 가속도
    public float attackPower; // 공격력 (공격하는 물고기)
    //public float experienceYield; // 획득 경험치 - 이게 필요할지는 잘 모르겠다 && 아마 넣은다면 도감의 숙련도? 그쪽으로 빠질듯 아직은 사용 x
    //public float evasionRate; // 플레이어 회피율 (도망치는 빈도?) 필요없을려나

    [Header("행동 패턴")]
    public FishBehaviorType behaviorType; // 행동 타입 (도망침, 공격, 중립)
    public float detectionRange; // 플레이어 감지 범위
    public float escapeSpeedMultiplier; // 도망칠 때 속도 배율
    public float maxChaseDuration; // 최대 추격 시간 (공격형 물고기) || 추적거리로 변경가능 

    public float scopeOfActivity; // 활동범위 - maxChaseDuration을 대신해도 상관없을듯
    public int herdCount; // 무리의 수

    [Header("자원 및 보상")]
    public int baseValue; // 기본 판매 가격
    //public int minQuantity; // 최소 획득 수량 || 필요할려나?
    //public int maxQuantity; // 최대 획득 수량 || 필요할려나?
    //public List<ItemDrop> possibleDrops; // 획득 가능한 아이템 목록 (살점, 비늘 등)

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
    ShallowSea,
    DeepSea,
    Cave,
    Wreckage,
    CoralReef
}

//public enum TimeOfDay
//{
//    Day,
//    Night,
//    Always
//}

public enum FishBehaviorType
{
    Flee, // 도망침
    Aggressive, // 공격적
    Neutral // 중립적
}
