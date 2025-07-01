using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapManager : MonoBehaviour
{
    [Header("맵 전체 크기 (월드 좌표 기준)")]
    public Vector3 mapSize = new Vector3(100f, 185f, 30f);

    [Header("맵에 배치할 바이옴 데이터 (MapManager 로컬 기준)")]
    public List<Biome> registeredBiomes;

    private Biome _normalBiomeInfo;

    // [SerializeField] private List<FishData> allFishData; 

    void Awake()
    {
        // 맵 매니저의 위치를 맵의 수직 중앙에 설정 (Y=0이 수면이 되도록)
        // 맵의 상단(Y=0)이 수면이고, 하단(Y=-mapSize.y)이 바닥이 됩니다.
        this.transform.position = new Vector3(0, -(mapSize.y / 2f), 0);

        InitializeMapBiomes();
    }


    // 맵 바이옴 시스템을 초기화합니다.
    void InitializeMapBiomes()
    {
        // _normalBiomeInfo를 내부적으로 초기화
        _normalBiomeInfo = ScriptableObject.CreateInstance<Biome>();
        _normalBiomeInfo.biomeName = "Normal Zone (Auto-Generated)";
        _normalBiomeInfo.habitatType = FishHabitat.Normal;
        _normalBiomeInfo.center = Vector3.zero; // Normal 바이옴은 MapManager 로컬 0,0,0을 기준으로 함
        _normalBiomeInfo.size = mapSize; // 논리적으로 맵 전체 크기 (실제 범위는 GetBiomeAtPosition에서 결정)
        _normalBiomeInfo.colorString = "0.2,0.7,0.2,0.5"; // 노말 바이옴 기본 색상 (녹색)

        // registeredBiomes 리스트에서 null 항목 제거
        if (registeredBiomes != null)
        {
            registeredBiomes.RemoveAll(item => item == null);
        }
        else
        {
            registeredBiomes = new List<Biome>();
        }
    }


    // 주어진 월드 좌표에 해당하는 BiomeData를 반환합니다.
    // 특정 바이옴에 속하지 않으면 자동으로 Normal BiomeData를 반환합니다.
    public Biome GetBiomeAtPosition(Vector3 worldPosition)
    {
        // 맵 매니저의 로컬 좌표계로 변환
        Vector3 localPosition = worldPosition - this.transform.position;

        // 맵 전체 범위를 벗어나면 null 반환 (맵의 로컬 좌표 기준)
        if (localPosition.x < -mapSize.x / 2f || localPosition.x > mapSize.x / 2f ||
            localPosition.y < -mapSize.y / 2f || localPosition.y > mapSize.y / 2f ||
            localPosition.z < -mapSize.z / 2f || localPosition.z > mapSize.z / 2f)
        {
            return null;
        }

        // 등록된 특정 바이옴에 속하는지 확인 (로컬 좌표 사용)
        foreach (Biome biome in registeredBiomes)
        {
            if (biome != null && biome.Contains(localPosition))
            {
                return biome;
            }
        }

        // 어떤 특정 바이옴에도 속하지 않으면 Normal 바이옴으로 간주
        return _normalBiomeInfo;
    }

    // 맵 범위와 바이옴을 Scene 뷰에 시각화
    void OnDrawGizmos()
    {
        // 에디터에서만 실행 중일 때 또는 게임 실행 중일 때 그리기
        if (this.enabled)
        {
            // 맵 전체 범위 그리기 (MapManager의 transform.position을 중심으로)
            Gizmos.color = new Color(0, 0.5f, 1f, 0.2f); // 하늘색 투명
            Gizmos.DrawCube(transform.position, mapSize);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, mapSize);

            // 각 바이옴 범위 그리기 (MapManager의 transform.position을 기준으로 오프셋)
            if (registeredBiomes != null)
            {
                foreach (Biome biome in registeredBiomes)
                {
                    if (biome == null) continue;

                    Color gizmoColor = biome.GetGizmoColor();
                    Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f); // 투명도 적용
                    // biome.center는 MapManager 로컬 좌표이므로, transform.position을 더해 월드 좌표로 변환
                    Gizmos.DrawCube(biome.center + transform.position, biome.size);
                    Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f); // 와이어 프레임은 불투명하게
                    Gizmos.DrawWireCube(biome.center + transform.position, biome.size);
                }
            }
        }
    }

    public float GetDepthFromYPosition(float yPosition)
    {
        // Y값이 음수일수록 깊어지므로 -를 붙여 양수 수심으로 변환
        return -yPosition;
    }

    // 물고기 소환 로직 (?)
    public void SpawnFishAtLocation(Vector3 spawnPosition, List<FishData> allAvailableFish)
    {
        Biome targetBiome = GetBiomeAtPosition(spawnPosition);
        if (targetBiome == null)
        {
            Debug.LogWarning($"Spawn location {spawnPosition} is outside map bounds. Cannot spawn fish.");
            return;
        }

        float currentDepth = GetDepthFromYPosition(spawnPosition.y);

        List<FishData> possibleFishToSpawn = new List<FishData>();

        foreach (FishData fish in allAvailableFish)
        {
            // 물고기 데이터의 서식지 (Habitat)가 현재 바이옴의 HabitatType과 일치하는지 확인
            bool habitatMatches = fish.habitats.Contains(targetBiome.habitatType);

            // 물고기 데이터의 최소/최대 수심 범위가 현재 수심과 일치하는지 확인
            // 물고기 Data의 minDepth, maxDepth는 양수 수심 값으로 가정
            bool depthMatches = currentDepth >= fish.minDepth && currentDepth <= fish.maxDepth;

            if (habitatMatches && depthMatches)
            {
                possibleFishToSpawn.Add(fish);
            }
        }

        if (possibleFishToSpawn.Count > 0)
        {
            FishData selectedFish = possibleFishToSpawn[Random.Range(0, possibleFishToSpawn.Count)];
            // 실제 물고기 프리팹을 인스턴스화하고 FishData를 할당하는 로직은 여기에 추가
            Debug.Log($"Spawned {selectedFish.fishName} (Habitat: {selectedFish.habitats[0]}, Depth: {currentDepth:F1}) in {targetBiome.biomeName}");
            // GameObject newFishGO = Instantiate(selectedFish.fishPrefab, spawnPosition, Quaternion.identity);
            // newFishGO.GetComponent<Fish>().FishData = selectedFish;
        }
        else
        {
            Debug.Log($"No fish found for {targetBiome.biomeName} at depth {currentDepth:F1}.");
        }
    }
}
