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
    public List<FishHabitat> habitats; // ������ (�븻, ����, ����, ��ȣ��)
    public float minDepth; // �ּ� ���� ����
    public float maxDepth; // �ִ� ���� ����

    [Header("����� �ɷ�ġ")]
    public float health; // ����� ü��
    public float speed; // ����� �̵� �ӵ�
    public float escapeSpeedMultiplier; // ����ĥ �� �ӵ� ����
    public float attackPower; // ���ݷ� (�����ϴ� �����)

    [Header("�ൿ ����")]
    public FishBehaviorType behaviorType; // �ൿ Ÿ�� (����ħ, ����, �߸�)  *
     [Tooltip("�÷��̾� ���� ����")]
    public float detectionRange; // �÷��̾� ���� ����

    [Header("����")]
    public bool useBoids; // ���� �ý��� ��� ����
    
     [Tooltip("Ȱ�� ����")]
    public float scopeOfActivity; // Ȱ������ 
    public int fishUnitCount; // ������ ��  *?


    [Header("�ڿ� �� ����")]
    public int baseValue; // �⺻ �Ǹ� ����

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
    Normal,
    Cave,
    Wreckage,
    CoralReef
}

public enum FishBehaviorType
{
    Flee, // ����ħ
    Aggressive, // ������
    Neutral // �߸���
}
