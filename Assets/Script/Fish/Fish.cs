using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class Fish : MonoBehaviour 
{ // ó��
    public FishData fishData;
    public Boid parentBoid;

    // ���� ��� ���� (FishData���� ������ ���� �ƴ�)
    private Vector2 acceleration;
    private Vector2 velocity;

    public LayerMask obstacleLayer;

    // Boid�κ��� ���� ���� ��� ���� (FishData�� ������ ���� �ƴ�)
    private Vector2 _flockingBoundsCenter;
    private float _flockingBoundsRadius;

    // �� Fish ��ü�� ������ ���� ��� ������ �����մϴ�. (Boid���� ȣ��)
    public void SetFlockingBounds(Vector2 center, float radius)
    {
        _flockingBoundsCenter = center;
        _flockingBoundsRadius = radius;
    }

    protected virtual void Start()
    {
        // fishData�� ������ �⺻�� ��� �Ǵ� ���� ó�� (�ʿ��)
        float currentMaxSpeed = 0f;

        if (fishData != null)
            currentMaxSpeed = fishData.speed;
        else
            Debug.Log("������ Ȯ��");

        // ���� �� ���� �������� �ʱ� �ӵ� ����
        float angle = Random.Range(0, 2 * Mathf.PI);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * currentMaxSpeed;
        transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
    }

    public void Update()
    {
        // �� ������ ���ӵ� �ʱ�ȭ
        acceleration = Vector2.zero;

        // 1. �ֺ� �̿� Ž�� (�ڽŰ� ���� Boid �Ʒ��� �ִ� Fish�鸸 ������� ��)
        var fishColliders = Physics2D.OverlapCircleAll(transform.position, fishData.flockNeighborhoodRadius); // FishData �� ���� ���
        var neighboringFish = fishColliders.Select(o => o.GetComponent<Fish>()).Where(f => f != null && f != this && f.transform.parent == this.transform.parent).ToList();

        // 2. ���� ��Ģ ����
        Flock(neighboringFish);

        // 3. ��ֹ� ȸ�� ����
        ObstacleAvoidance();

        // 4. ��� ȸ�� ���� (���� ���)
        CircularBoundaryAvoidance();

        // 5. �ӵ� �� ��ġ ������Ʈ
        UpdateVelocity();
        UpdatePosition();

        // 6. ȸ�� ������Ʈ (�ٶ󺸴� ����)
        UpdateRotation();
    }

    /// <summary>
    /// �� ���� ���� ��Ģ (����, ����, �и�)�� �����Ͽ� ���ӵ��� ����մϴ�.
    /// </summary>
    /// <param name="fishAgents">�ֺ��� �ִ� �ٸ� Fish ������Ʈ ���</param>
    private void Flock(IEnumerable<Fish> fishAgents)
    {
        Vector2 alignmentForce = Alignment(fishAgents);
        Vector2 cohesionForce = Cohesion(fishAgents);
        Vector2 separationForce = Separation(fishAgents);

        // FishData �� ���� ���
        acceleration += separationForce * fishData.flockSeparationWeight;
        acceleration += cohesionForce * fishData.flockCohesionWeight;
        acceleration += alignmentForce * fishData.flockAlignmentWeight;
    }

    /// <summary>
    /// ��ֹ����� �浹�� ȸ���ϴ� ���� ����Ͽ� ���ӵ��� �߰��մϴ�.
    /// </summary>
    private void ObstacleAvoidance()
    {
        Vector2 currentForward = velocity.normalized;
        Vector2 leftRayDir = Quaternion.Euler(0, 0, 30) * currentForward;
        Vector2 rightRayDir = Quaternion.Euler(0, 0, -30) * currentForward;

        // FishData �� ���� ���
        RaycastHit2D frontHit = Physics2D.CircleCast(transform.position, 0.2f, currentForward, fishData.raycastLength, obstacleLayer);
        RaycastHit2D leftHit = Physics2D.CircleCast(transform.position, 0.2f, leftRayDir, fishData.raycastLength, obstacleLayer);
        RaycastHit2D rightHit = Physics2D.CircleCast(transform.position, 0.2f, rightRayDir, fishData.raycastLength, obstacleLayer);

        // ����� �׸��� (���⵵ FishData �� ���� ���)
        Debug.DrawRay(transform.position, currentForward * fishData.raycastLength, frontHit.collider ? Color.red : Color.white);
        Debug.DrawRay(transform.position, leftRayDir * fishData.raycastLength, leftHit.collider ? Color.red : Color.green);
        Debug.DrawRay(transform.position, rightRayDir * fishData.raycastLength, rightHit.collider ? Color.red : Color.green);

        Vector2 steerForce = Vector2.zero;
        int hitCount = 0;

        if (frontHit.collider)
        {
            hitCount++;
            steerForce += (Vector2)(transform.position - (Vector3)frontHit.point).normalized * 2f;
        }
        if (leftHit.collider)
        {
            hitCount++;
            steerForce += (Vector2)(transform.position - (Vector3)leftHit.point).normalized;
        }
        if (rightHit.collider)
        {
            hitCount++;
            steerForce += (Vector2)(transform.position - (Vector3)rightHit.point).normalized;
        }

        if (hitCount >= 2)
        {
            Vector2 perpendicularDir = new Vector2(-currentForward.y, currentForward.x).normalized;
            if (Random.value < 0.5f) perpendicularDir *= -1;
            steerForce = perpendicularDir;
        }

        if (hitCount > 0)
        {
            // FishData �� ���� ���
            acceleration += Steer(steerForce.normalized * fishData.speed) * fishData.obstacleAvoidanceWeight;
        }
    }

    /// <summary>
    /// ������ ���� ��� ������ ������ �ʵ��� ȸ���ϴ� ���� ����Ͽ� ���ӵ��� �߰��մϴ�.
    /// </summary>
    private void CircularBoundaryAvoidance()
    {
        if (_flockingBoundsRadius <= 0) return;
        if (fishData == null) return; // fishData�� ������ �������� ����

        float distanceFromCenter = Vector2.Distance(transform.position, _flockingBoundsCenter);

        // FishData �� ���� ���
        if (distanceFromCenter >= _flockingBoundsRadius - fishData.boundaryMargin)
        {
            Debug.Log("��谪 ����");

            Vector2 desiredDirection = (_flockingBoundsCenter - (Vector2)transform.position).normalized;
            // FishData �� ���� ���
            Vector2 steerForce = Steer(desiredDirection * fishData.speed);

            // ��迡 �������� ���� ���ϰ� ���� (FishData �� ���� ���)
            float strength = Mathf.Clamp01((distanceFromCenter - (_flockingBoundsRadius - fishData.boundaryMargin)) / fishData.boundaryMargin);

            // FishData �� ���� ���
            acceleration += steerForce * fishData.boundsAvoidanceWeight * strength;
        }
    }

    /// <summary>
    /// ���ӵ��� �ӵ��� �����ϰ�, �ӵ� ������ �����մϴ�.
    /// </summary>
    protected void UpdateVelocity()
    {
        velocity += acceleration * Time.deltaTime;

        // FishData �� ���� ���
        float minCurrentSpeed = fishData.speed * 0.4f;
        if (velocity.magnitude < minCurrentSpeed)
        {
            velocity = velocity.normalized * minCurrentSpeed;
        }

        // FishData �� ���� ���
        velocity = LimitMagnitude(velocity, fishData.speed);
    }

    /// <summary>
    /// �ӵ��� �̿��Ͽ� ������Ʈ�� ��ġ�� ������Ʈ�մϴ�.
    /// </summary>
    protected void UpdatePosition()
    {
        Vector3 newPosition = transform.position + (Vector3)velocity * Time.deltaTime;
        newPosition.z = 0f;
        transform.position = newPosition;
    }

    /// <summary>
    /// ������Ʈ�� ���� �ӵ� ������ �ٶ󺸵��� ȸ����ŵ�ϴ�.
    /// </summary>
    protected void UpdateRotation()
    {
        if (velocity.sqrMagnitude < 0.001f) return;
        if (fishData == null) return; // fishData�� ������ �������� ����

        float targetAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

        // FishData �� ���� ���
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, fishData.rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// �ֺ� �̿����� ��� �ӵ��� ������ �������� �����̷��� �� (����)�� ����մϴ�.
    /// </summary>
    /// <param name="fishAgents">�ֺ� �̿� ���</param>
    /// <returns>���� �� ����</returns>
    private Vector2 Alignment(IEnumerable<Fish> fishAgents)
    {
        if (!fishAgents.Any()) return Vector2.zero;

        Vector2 averageVelocity = Vector2.zero;
        foreach (var f in fishAgents)
        {
            averageVelocity += f.velocity;
        }
        averageVelocity /= fishAgents.Count();

        // FishData �� ���� ���
        return Steer(averageVelocity.normalized * fishData.speed);
    }

    /// <summary>
    /// �ֺ� �̿����� ��� ��ġ(���� �߽�)�� �̵��Ϸ��� �� (����)�� ����մϴ�.
    /// </summary>
    /// <param name="fishAgents">�ֺ� �̿� ���</param>
    /// <returns>���� �� ����</returns>
    private Vector2 Cohesion(IEnumerable<Fish> fishAgents)
    {
        if (!fishAgents.Any()) return Vector2.zero;

        Vector2 centerOfMass = Vector2.zero;
        foreach (var f in fishAgents)
        {
            centerOfMass += (Vector2)f.transform.position;
        }
        centerOfMass /= fishAgents.Count();

        // FishData �� ���� ���
        return Steer((centerOfMass - (Vector2)transform.position).normalized * fishData.speed);
    }

    /// <summary>
    /// ����� �̿���� �浹���� �ʵ��� �о���� �� (�и�)�� ����մϴ�.
    /// </summary>
    /// <param name="fishAgents">�ֺ� �̿� ���</param>
    /// <returns>�и� �� ����</returns>
    private Vector2 Separation(IEnumerable<Fish> fishAgents)
    {
        // FishData �� ���� ���
        var closeFish = fishAgents.Where(f => Vector2.Distance(transform.position, f.transform.position) <= fishData.flockSeparationRadius).ToList();
        if (!closeFish.Any()) return Vector2.zero;

        Vector2 repulsionForce = Vector2.zero;
        foreach (var f in closeFish)
        {
            Vector2 diff = (Vector2)transform.position - (Vector2)f.transform.position;
            repulsionForce += diff.normalized / Mathf.Max(0.001f, diff.magnitude * diff.magnitude);
        }
        repulsionForce /= closeFish.Count;
        // FishData �� ���� ���
        return Steer(repulsionForce.normalized * fishData.speed);
    }

    /// <summary>
    /// ���ϴ� �ӵ�(desired)�� ���� ���� �ʿ��� ���ӵ�(steering force)�� ����մϴ�.
    /// �� ���� �ִ� ��(maxForce)���� ���ѵ˴ϴ�.
    /// </summary>
    /// <param name="desired">���ϴ� �ӵ� ����</param>
    /// <returns>���� ���� �� ����</returns>
    private Vector2 Steer(Vector2 desired)
    {
        // FishData �� ���� ���
        Vector2 steerForce = desired - velocity;
        return LimitMagnitude(steerForce, fishData.flockMaxForce); // maxForce�� fishData���� ������
    }

    /// <summary>
    /// ������ ũ�⸦ �����մϴ�.
    /// </summary>
    /// <param name="vector">������ ����</param>
    /// <param name="max">�ִ� ũ��</param>
    /// <returns>ũ�Ⱑ ���ѵ� ����</returns>
    private Vector2 LimitMagnitude(Vector2 vector, float max)
    {
        return vector.sqrMagnitude > max * max ? vector.normalized * max : vector;
    }

    // --- Gizmos for Debugging ---
    protected virtual void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && fishData != null) // fishData�� ���� ���� �׸���
        {
            Gizmos.color = Color.magenta;
            // Boid�κ��� ���� ���� ��踦 �׸��ϴ�.
            Gizmos.DrawWireSphere(_flockingBoundsCenter, _flockingBoundsRadius);
            // FishData �� ���� ���
            Gizmos.DrawWireSphere(_flockingBoundsCenter, _flockingBoundsRadius - fishData.boundaryMargin);
        }

        // FishData�� ������ �⺻������ �׸��� (���� ����)
        float currentNeighborhoodRadius = (fishData != null) ? fishData.flockNeighborhoodRadius : 1.2f;
        float currentSeparationRadius = (fishData != null) ? fishData.flockSeparationRadius : 0.6f;
        float currentRaycastLength = (fishData != null) ? fishData.raycastLength : 1.5f;

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, currentNeighborhoodRadius);
        Gizmos.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
        Gizmos.DrawWireSphere(transform.position, currentSeparationRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)velocity);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)acceleration * 10f);

#if UNITY_EDITOR
        if (Application.isPlaying && fishData != null)
        {
            var fishColliders = Physics2D.OverlapCircleAll(transform.position, fishData.flockNeighborhoodRadius);
            var neighboringFish = fishColliders.Select(o => o.GetComponent<Fish>()).Where(f => f != null && f != this && f.transform.parent == this.transform.parent).ToList();

            // FishData �� ���� ���
            Vector2 align = Alignment(neighboringFish);
            Vector2 coh = Cohesion(neighboringFish);
            Vector2 sep = Separation(neighboringFish);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)align * fishData.flockAlignmentWeight);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)coh * fishData.flockCohesionWeight);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)sep * fishData.flockSeparationWeight);

            Vector2 currentForward = velocity.normalized;
            RaycastHit2D frontHit = Physics2D.CircleCast(transform.position, 0.2f, currentForward, fishData.raycastLength, obstacleLayer);
            if (frontHit.collider)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)(transform.position - (Vector3)frontHit.point).normalized * fishData.obstacleAvoidanceWeight);
            }
        }
#endif
    }
}
