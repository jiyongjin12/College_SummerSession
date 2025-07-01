using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapManager : MonoBehaviour
{
    [Header("�� ��ü ũ�� (���� ��ǥ ����)")]
    public Vector3 mapSize = new Vector3(100f, 185f, 30f);

    [Header("�ʿ� ��ġ�� ���̿� ������ (MapManager ���� ����)")]
    public List<Biome> registeredBiomes;

    private Biome _normalBiomeInfo;

    // [SerializeField] private List<FishData> allFishData; 

    void Awake()
    {
        // �� �Ŵ����� ��ġ�� ���� ���� �߾ӿ� ���� (Y=0�� ������ �ǵ���)
        // ���� ���(Y=0)�� �����̰�, �ϴ�(Y=-mapSize.y)�� �ٴ��� �˴ϴ�.
        this.transform.position = new Vector3(0, -(mapSize.y / 2f), 0);

        InitializeMapBiomes();
    }


    // �� ���̿� �ý����� �ʱ�ȭ�մϴ�.
    void InitializeMapBiomes()
    {
        // _normalBiomeInfo�� ���������� �ʱ�ȭ
        _normalBiomeInfo = ScriptableObject.CreateInstance<Biome>();
        _normalBiomeInfo.biomeName = "Normal Zone (Auto-Generated)";
        _normalBiomeInfo.habitatType = FishHabitat.Normal;
        _normalBiomeInfo.center = Vector3.zero; // Normal ���̿��� MapManager ���� 0,0,0�� �������� ��
        _normalBiomeInfo.size = mapSize; // �������� �� ��ü ũ�� (���� ������ GetBiomeAtPosition���� ����)
        _normalBiomeInfo.colorString = "0.2,0.7,0.2,0.5"; // �븻 ���̿� �⺻ ���� (���)

        // registeredBiomes ����Ʈ���� null �׸� ����
        if (registeredBiomes != null)
        {
            registeredBiomes.RemoveAll(item => item == null);
        }
        else
        {
            registeredBiomes = new List<Biome>();
        }
    }


    // �־��� ���� ��ǥ�� �ش��ϴ� BiomeData�� ��ȯ�մϴ�.
    // Ư�� ���̿ȿ� ������ ������ �ڵ����� Normal BiomeData�� ��ȯ�մϴ�.
    public Biome GetBiomeAtPosition(Vector3 worldPosition)
    {
        // �� �Ŵ����� ���� ��ǥ��� ��ȯ
        Vector3 localPosition = worldPosition - this.transform.position;

        // �� ��ü ������ ����� null ��ȯ (���� ���� ��ǥ ����)
        if (localPosition.x < -mapSize.x / 2f || localPosition.x > mapSize.x / 2f ||
            localPosition.y < -mapSize.y / 2f || localPosition.y > mapSize.y / 2f ||
            localPosition.z < -mapSize.z / 2f || localPosition.z > mapSize.z / 2f)
        {
            return null;
        }

        // ��ϵ� Ư�� ���̿ȿ� ���ϴ��� Ȯ�� (���� ��ǥ ���)
        foreach (Biome biome in registeredBiomes)
        {
            if (biome != null && biome.Contains(localPosition))
            {
                return biome;
            }
        }

        // � Ư�� ���̿ȿ��� ������ ������ Normal ���̿����� ����
        return _normalBiomeInfo;
    }

    // �� ������ ���̿��� Scene �信 �ð�ȭ
    void OnDrawGizmos()
    {
        // �����Ϳ����� ���� ���� �� �Ǵ� ���� ���� ���� �� �׸���
        if (this.enabled)
        {
            // �� ��ü ���� �׸��� (MapManager�� transform.position�� �߽�����)
            Gizmos.color = new Color(0, 0.5f, 1f, 0.2f); // �ϴû� ����
            Gizmos.DrawCube(transform.position, mapSize);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, mapSize);

            // �� ���̿� ���� �׸��� (MapManager�� transform.position�� �������� ������)
            if (registeredBiomes != null)
            {
                foreach (Biome biome in registeredBiomes)
                {
                    if (biome == null) continue;

                    Color gizmoColor = biome.GetGizmoColor();
                    Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f); // ���� ����
                    // biome.center�� MapManager ���� ��ǥ�̹Ƿ�, transform.position�� ���� ���� ��ǥ�� ��ȯ
                    Gizmos.DrawCube(biome.center + transform.position, biome.size);
                    Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f); // ���̾� �������� �������ϰ�
                    Gizmos.DrawWireCube(biome.center + transform.position, biome.size);
                }
            }
        }
    }

    public float GetDepthFromYPosition(float yPosition)
    {
        // Y���� �����ϼ��� ������Ƿ� -�� �ٿ� ��� �������� ��ȯ
        return -yPosition;
    }

    // ����� ��ȯ ���� (?)
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
            // ����� �������� ������ (Habitat)�� ���� ���̿��� HabitatType�� ��ġ�ϴ��� Ȯ��
            bool habitatMatches = fish.habitats.Contains(targetBiome.habitatType);

            // ����� �������� �ּ�/�ִ� ���� ������ ���� ���ɰ� ��ġ�ϴ��� Ȯ��
            // ����� Data�� minDepth, maxDepth�� ��� ���� ������ ����
            bool depthMatches = currentDepth >= fish.minDepth && currentDepth <= fish.maxDepth;

            if (habitatMatches && depthMatches)
            {
                possibleFishToSpawn.Add(fish);
            }
        }

        if (possibleFishToSpawn.Count > 0)
        {
            FishData selectedFish = possibleFishToSpawn[Random.Range(0, possibleFishToSpawn.Count)];
            // ���� ����� �������� �ν��Ͻ�ȭ�ϰ� FishData�� �Ҵ��ϴ� ������ ���⿡ �߰�
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
