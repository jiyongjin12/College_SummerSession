using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    [Header("���� ����")]
    public FishData targetFishData;

    // TODO: ���⿡ ���� Boid(����) ������ �߰��մϴ�.
    // - �� Boid ���� �� ���� �������� ������, ����, ����, �и� ���� ó���մϴ�.
    // - targetFishData�� speed, behaviorType ���� �����Ͽ� �����մϴ�.

    void Start()
    {
        if (targetFishData != null)
        {
            // ��: targetFishData.fishPrefab�� ����Ͽ� ���� ����� �� ����
            // GameObject fishModel = Instantiate(targetFishData.fishPrefab, transform);
            // fishModel.transform.localPosition = Vector3.zero;

            // Boid�� �ʱ� �ӵ� ���� ��
        }
        else
        {
            Debug.LogWarning("Boid has no targetFishData assigned!");
        }
    }

    // ���⿡ Boid �˰��� (Cohesion, Alignment, Separation) ������ �����մϴ�.
    // ���� ���:
    // private void Update()
    // {
    //     Vector3 cohesion = CalculateCohesion();
    //     Vector3 alignment = CalculateAlignment();
    //     Vector3 separation = CalculateSeparation();

    //     Vector3 finalDirection = (cohesion + alignment + separation).normalized;
    //     transform.position += finalDirection * targetFishData.speed * Time.deltaTime;
    // }

}
