using UnityEngine;

public class Flocking_Spawn_Test : MonoBehaviour
{
    public GameObject fishPrefab; // Flocking_Test 스크립트가 부착된 물고기 프리팹

    [Range(1, 700)] // 최소 1마리는 스폰하도록 범위 조정
    public int numberToSpawn = 100; // 스폰할 물고기의 개수 (변수명 변경)

    public Vector2 spawnAreaSize = new Vector2(10, 10); // 물고기가 스폰될 사각형 영역의 크기

    private void Start()
    {
        for (int i = 0; i < numberToSpawn; i++) // 변수명 변경
        {
            // 지정된 스폰 영역 내에서 랜덤 위치 생성
            Vector2 randomPos = new Vector2(
                Random.Range(transform.position.x - spawnAreaSize.x / 2, transform.position.x + spawnAreaSize.x / 2),
                Random.Range(transform.position.y - spawnAreaSize.y / 2, transform.position.y + spawnAreaSize.y / 2)
            );
            // Z축을 0으로 고정하여 인스턴스화
            Vector3 spawnPosition3D = new Vector3(randomPos.x, randomPos.y, 0f);

            var obj = Instantiate(fishPrefab, spawnPosition3D, Quaternion.identity);

            // Flocking_Test 컴포넌트 가져오기
            Flocking_Test flockingAgent = obj.GetComponent<Flocking_Test>();
            if (flockingAgent != null)
            {
                // 생성된 에이전트에게 경계 정보 전달
                flockingAgent.SetBounds(transform.position, spawnAreaSize);
            }
            else
            {
                Debug.LogWarning($"Spawned object {obj.name} does not have a Flocking_Test component!");
            }
        }
    }

    // Scene 뷰에서 스폰 영역을 시각화합니다.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        // transform.position을 중심으로 spawnAreaSize 크기의 와이어 큐브 그리기
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0.01f)); // Z축을 얇게
    }
}
