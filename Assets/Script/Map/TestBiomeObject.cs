using UnityEngine;

public class TestBiomeObject : MonoBehaviour
{
    // üũ �ֱ⸦ ���� (��: 1�ʸ��� üũ)
    public float checkInterval = 1f;
    private float timer;

    void Start()
    {
        // ���� ���� �� �� �� �ٷ� üũ
        CheckCurrentBiome();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            CheckCurrentBiome();
            timer = 0f;
        }
    }

    void CheckCurrentBiome()
    {
        // ���� ������Ʈ�� ���� ��ġ�� �����ɴϴ�.
        Vector3 currentPosition = transform.position;

        // MapManager�� ���� ���� ��ġ�� ���̿� �����͸� �����ɴϴ�.
        Biome currentBiome = MapManager.Instance.GetBiomeAtPosition(currentPosition);

        // �Ƹ��� ����   
        float currentDepth = MapManager.Instance.GetDepthFromYPosition(currentPosition.y);

        if (currentBiome != null)
        {
            // ���̿� �̸��� ������ Ÿ���� ����� �α׷� ���
            Debug.Log($" ���� ��ġ: {currentPosition}");
            Debug.Log($" ����: {currentDepth:F1}m");
            Debug.Log($" ���̿�: '{currentBiome.biomeName}' (Ÿ��: {currentBiome.habitatType}) ");
        }
        else
        {
            // �� ������ ��� ���
            Debug.LogWarning($"[{gameObject.name}] ���� ��ġ: {currentPosition}, WA ���� �Ծ���");
        }
    }

}
