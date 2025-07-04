using UnityEngine;

public class TestBiomeObject : MonoBehaviour
{
    // 체크 주기를 설정 (예: 1초마다 체크)
    public float checkInterval = 1f;
    private float timer;

    void Start()
    {
        // 게임 시작 시 한 번 바로 체크
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
        // 현재 오브젝트의 월드 위치를 가져옵니다.
        Vector3 currentPosition = transform.position;

        // MapManager를 통해 현재 위치의 바이옴 데이터를 가져옵니다.
        Biome currentBiome = MapManager.Instance.GetBiomeAtPosition(currentPosition);

        // 아마도 수심   
        float currentDepth = MapManager.Instance.GetDepthFromYPosition(currentPosition.y);

        if (currentBiome != null)
        {
            // 바이옴 이름과 서식지 타입을 디버그 로그로 출력
            Debug.Log($" 현재 위치: {currentPosition}");
            Debug.Log($" 수심: {currentDepth:F1}m");
            Debug.Log($" 바이옴: '{currentBiome.biomeName}' (타입: {currentBiome.habitatType}) ");
        }
        else
        {
            // 맵 범위를 벗어난 경우
            Debug.LogWarning($"[{gameObject.name}] 현재 위치: {currentPosition}, WA 맵을 뚤었어");
        }
    }

}
