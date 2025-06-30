using System.Collections.Generic;
using UnityEngine;

//[System.Serializable]
[CreateAssetMenu(menuName = "Fish/FishData")]
public class FishData : ScriptableObject
{
    public string fishName; // ����� �̸� (��: ��ġ, ��¡��, �ظ�)
    public int fishID; // ID
    public GameObject fishPrefab; // ����� ���� 

    [Header("���� ����")]
    public FishType fishType; // ����� ���� (��: �Ϲ�, ���, ����, ����) && ������ (��: ���� ���� ����)���� �ٲ㵵 ������ 
    public List<FishHabitat> habitats; // ������ (��: ���� �ٴ�, ����, ����)
    public float minDepth; // �ּ� ���� ����
    public float maxDepth; // �ִ� ���� ����
    //public List<TimeOfDay> activeTimes; // Ȱ�� �ð� (��: ��, ��, �׻�) �̰� �ʿ��ҷ���? �� ��ħ�� ���� �㿡 ���� �̷������� �Ұ� ������

    [Header("����� �ɷ�ġ")]
    public float health; // ����� ü��
    public float speed; // ����� �̵� �ӵ�
    public float acceleration; // ����� ���ӵ�
    public float attackPower; // ���ݷ� (�����ϴ� �����)
    //public float experienceYield; // ȹ�� ����ġ - �̰� �ʿ������� �� �𸣰ڴ� && �Ƹ� �����ٸ� ������ ���õ�? �������� ������ ������ ��� x
    //public float evasionRate; // �÷��̾� ȸ���� (����ġ�� ��?) �ʿ��������

    [Header("�ൿ ����")]
    public FishBehaviorType behaviorType; // �ൿ Ÿ�� (����ħ, ����, �߸�)
    public float detectionRange; // �÷��̾� ���� ����
    public float escapeSpeedMultiplier; // ����ĥ �� �ӵ� ����
    public float maxChaseDuration; // �ִ� �߰� �ð� (������ �����) || �����Ÿ��� ���氡�� 

    public float scopeOfActivity; // Ȱ������ - maxChaseDuration�� ����ص� ���������
    public int herdCount; // ������ ��

    [Header("�ڿ� �� ����")]
    public int baseValue; // �⺻ �Ǹ� ����
    //public int minQuantity; // �ּ� ȹ�� ���� || �ʿ��ҷ���?
    //public int maxQuantity; // �ִ� ȹ�� ���� || �ʿ��ҷ���?
    //public List<ItemDrop> possibleDrops; // ȹ�� ������ ������ ��� (����, ��� ��)

    [Header("���� ����")]
    [TextArea(3, 5)]
    public string description; // ����� ���� (������ ǥ��)
    public Sprite fishIcon; // ������ ǥ�õ� ����� ������
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
    Flee, // ����ħ
    Aggressive, // ������
    Neutral // �߸���
}
