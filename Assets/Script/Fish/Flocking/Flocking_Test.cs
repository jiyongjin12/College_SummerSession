using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Flocking_Test : MonoBehaviour
{
    // �ʵ� ���� �� ������ ����
    public float maxSpeed = 3f; // �ִ� �ӵ�
    public float maxForce = 0.01f; // �� ������ �ִ�ġ
    public float neighborhoodRadius = 1.2f; // �ֺ� �̿� Ž�� �ݰ�
    public float separationRadius = 0.6f; // �и� (�浹 ȸ��)�� ���� ����� �̿� �ݰ� (neighborhoodRadius�� ����)

    [Range(0f, 2f)] public float separationWeight = 1.5f; // �и� ���� ����ġ
    [Range(0f, 2f)] public float cohesionWeight = 1f;    // ���� ���� ����ġ
    [Range(0f, 2f)] public float alignmentWeight = 1f;   // ���� ���� ����ġ

    [Header("ȸ�� ����")]
    public LayerMask obstacleLayer; // ��ֹ� ���̾� (�ν����Ϳ��� ����)
    public float obstacleAvoidanceWeight = 2f; // ��ֹ� ȸ�� ���� ����ġ
    public float raycastLength = 1.5f; // ��ֹ� ���� Raycast ����
    public float rotationSpeed = 360f; // �ʴ� ȸ�� ����

    [Header("��� ����")]
    public float boundsAvoidanceWeight = 2f; // ��� ȸ�� ���� ����ġ
    public float boundaryMargin = 3f; // ���κ��� �̸�ŭ ������ ���� ������ ȸ�� ����

    // ���� ��� ���� (private)
    private Vector2 acceleration;
    private Vector2 velocity;
    private Vector2 flockingBoundsCenter;
    private Vector2 flockingBoundsSize;

    // �� �÷�ŷ ������Ʈ�� ������ ��� ������ �����մϴ�.
    public void SetBounds(Vector2 center, Vector2 size)
    {
        flockingBoundsCenter = center;
        flockingBoundsSize = size;
    }

    private void Start()
    {
        // ���� �� ���� �������� �ʱ� �ӵ� ����
        float angle = Random.Range(0, 2 * Mathf.PI);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * maxSpeed; // �ʱ� �ӵ��� maxSpeed�� ��������
        transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg); // Z�� ȸ���� ����
    }

    private void Update()
    {
        // �� ������ ���ӵ� �ʱ�ȭ
        acceleration = Vector2.zero;

        // 1. �ֺ� �̿� Ž��
        var boidColliders = Physics2D.OverlapCircleAll(transform.position, neighborhoodRadius);
        var boids = boidColliders
                    .Select(o => o.GetComponent<Flocking_Test>())
                    .Where(b => b != null && b != this)
                    .ToList();

        // 2. ���� ��Ģ ����
        Flock(boids);

        // 3. ��ֹ� ȸ�� ����
        ObstacleAvoidance();

        // 4. ��� ȸ�� ����
        BoundaryAvoidance();

        // 5. �ӵ� �� ��ġ ������Ʈ
        UpdateVelocity();
        UpdatePosition();

        // 6. ȸ�� ������Ʈ (�ٶ󺸴� ����)
        UpdateRotation();
    }

    // �� ���� ���� ��Ģ (����, ����, �и�)�� �����Ͽ� ���ӵ��� ����մϴ�.
    private void Flock(IEnumerable<Flocking_Test> boids)
    {
        Vector2 alignmentForce = Alignment(boids);
        Vector2 cohesionForce = Cohesion(boids);
        Vector2 separationForce = Separation(boids);

        // �� ���� ����ġ�� ���Ͽ� ���ӵ��� �ջ�
        acceleration += separationForce * separationWeight;
        acceleration += cohesionForce * cohesionWeight;
        acceleration += alignmentForce * alignmentWeight;
    }

    // ��ֹ����� �浹�� ȸ���ϴ� ���� ����Ͽ� ���ӵ��� �߰��մϴ�.
    private void ObstacleAvoidance()
    {
        Vector2 currentForward = velocity.normalized;
        // ����, ���� 30��, ���� 30�� �������� Raycast �߻�
        Vector2 leftRayDir = Quaternion.Euler(0, 0, 30) * currentForward;
        Vector2 rightRayDir = Quaternion.Euler(0, 0, -30) * currentForward;

        RaycastHit2D frontHit = Physics2D.CircleCast(transform.position, 0.2f, currentForward, raycastLength, obstacleLayer);
        RaycastHit2D leftHit = Physics2D.CircleCast(transform.position, 0.2f, leftRayDir, raycastLength, obstacleLayer);
        RaycastHit2D rightHit = Physics2D.CircleCast(transform.position, 0.2f, rightRayDir, raycastLength, obstacleLayer);

        // Gizmo (Scene �信�� ���������� Raycast �ð�ȭ)
        Debug.DrawRay(transform.position, currentForward * raycastLength, frontHit.collider ? Color.red : Color.white);
        Debug.DrawRay(transform.position, leftRayDir * raycastLength, leftHit.collider ? Color.red : Color.green);
        Debug.DrawRay(transform.position, rightRayDir * raycastLength, rightHit.collider ? Color.red : Color.green);

        Vector2 steerForce = Vector2.zero;
        int hitCount = 0;

        // �浹 ���� �� ȸ�� ���� ���
        if (frontHit.collider)
        {
            hitCount++;
            // ���� �浹 �� ���ϰ� �о�� (�浹 �������κ��� �ݴ� ����)
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

        // ��/��/���� ��� �����Ǿ� ������ ��, �����ϰ� ���� �������� ȸ��
        if (hitCount >= 2)
        {
            // ���� ���� ������ ���� ���� �� �ϳ��� ����
            Vector2 perpendicularDir = new Vector2(-currentForward.y, currentForward.x).normalized;
            if (Random.value < 0.5f) perpendicularDir *= -1; // �ݴ� ���� ���⵵ ���
            steerForce = perpendicularDir; // ���� �������� ���ϰ� ����
        }

        if (hitCount > 0)
        {
            acceleration += Steer(steerForce.normalized * maxSpeed) * obstacleAvoidanceWeight;
        }
    }

    // ������ ��� ���� ������ ������ �ʵ��� ȸ���ϴ� ���� ����Ͽ� ���ӵ��� �߰��մϴ�.
    private void BoundaryAvoidance()
    {
        Vector2 minBounds = flockingBoundsCenter - flockingBoundsSize / 2f;
        Vector2 maxBounds = flockingBoundsCenter + flockingBoundsSize / 2f;
        Vector2 desiredVelocity = Vector2.zero;

        // X�� ��� üũ
        if (transform.position.x < minBounds.x + boundaryMargin)
        {
            desiredVelocity.x = maxSpeed; // ���������� �̵�
        }
        else if (transform.position.x > maxBounds.x - boundaryMargin)
        {
            desiredVelocity.x = -maxSpeed; // �������� �̵�
        }

        // Y�� ��� üũ
        if (transform.position.y < minBounds.y + boundaryMargin)
        {
            desiredVelocity.y = maxSpeed; // �������� �̵�
        }
        else if (transform.position.y > maxBounds.y - boundaryMargin)
        {
            desiredVelocity.y = -maxSpeed; // �Ʒ������� �̵�
        }

        if (desiredVelocity != Vector2.zero)
        {
            Vector2 steerForce = Steer(desiredVelocity);

            // ��迡 �������� ���� ���ϰ� ����
            float distanceToClosestBoundary = Mathf.Min(
                Mathf.Abs(transform.position.x - minBounds.x),
                Mathf.Abs(transform.position.x - maxBounds.x),
                Mathf.Abs(transform.position.y - minBounds.y),
                Mathf.Abs(transform.position.y - maxBounds.y)
            );
            float strength = 1f - Mathf.Clamp01(distanceToClosestBoundary / boundaryMargin);

            acceleration += steerForce * boundsAvoidanceWeight * strength;
        }
    }

    // ���ӵ��� �ӵ��� �����ϰ�, �ӵ� ������ �����մϴ�.
    public void UpdateVelocity()
    {
        // ���ӵ��� �ð��� �����ӿ� ���������� ����
        velocity += acceleration * Time.deltaTime;

        // �ּ� �ӵ� ���� (����Ⱑ ������ �ʵ���)
        float minCurrentSpeed = maxSpeed * 0.4f;
        if (velocity.magnitude < minCurrentSpeed)
        {
            velocity = velocity.normalized * minCurrentSpeed;
        }

        // �ִ� �ӵ� ����
        velocity = LimitMagnitude(velocity, maxSpeed);
    }

    // �ӵ��� �̿��Ͽ� ������Ʈ�� ��ġ�� ������Ʈ�մϴ�.
    private void UpdatePosition()
    {
        // 2D �����̹Ƿ� Z���� 0���� ����
        Vector3 newPosition = transform.position + (Vector3)velocity * Time.deltaTime;
        newPosition.z = 0f;
        transform.position = newPosition;
    }

    // ������Ʈ�� ���� �ӵ� ������ �ٶ󺸵��� ȸ����ŵ�ϴ�.
    private void UpdateRotation()
    {
        if (velocity.sqrMagnitude < 0.001f) return; // �ӵ��� ���� ������ ȸ������ ����

        // �ٶ���� �� ���� ��� (Z�� ����)
        float targetAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);

        // ���� ȸ������ ��ǥ ȸ������ �ڿ������� ����
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // �ֺ� �̿����� ��� �ӵ��� ������ �������� �����̷��� �� (����)�� ����մϴ�.
    private Vector2 Alignment(IEnumerable<Flocking_Test> boids)
    {
        if (!boids.Any()) return Vector2.zero;

        Vector2 averageVelocity = Vector2.zero;
        foreach (var b in boids)
        {
            averageVelocity += b.velocity;
        }
        averageVelocity /= boids.Count();

        return Steer(averageVelocity.normalized * maxSpeed);
    }

    // �ֺ� �̿����� ��� ��ġ(���� �߽�)�� �̵��Ϸ��� �� (����)�� ����մϴ�.
    private Vector2 Cohesion(IEnumerable<Flocking_Test> boids)
    {
        if (!boids.Any()) return Vector2.zero;

        Vector2 centerOfMass = Vector2.zero;
        foreach (var b in boids)
        {
            centerOfMass += (Vector2)b.transform.position; // Vector3�� Vector2�� ĳ����
        }
        centerOfMass /= boids.Count();

        return Steer((centerOfMass - (Vector2)transform.position).normalized * maxSpeed);
    }

    // ����� �̿���� �浹���� �ʵ��� �о���� �� (�и�)�� ����մϴ�.
    private Vector2 Separation(IEnumerable<Flocking_Test> boids)
    {
        // neighborhoodRadius ��� separationRadius�� ����Ͽ� �� ����� �̿��� ���
        var closeBoids = boids.Where(b => Vector2.Distance(transform.position, b.transform.position) <= separationRadius).ToList();
        if (!closeBoids.Any()) return Vector2.zero;

        Vector2 repulsionForce = Vector2.zero;
        foreach (var b in closeBoids)
        {
            Vector2 diff = (Vector2)transform.position - (Vector2)b.transform.position;
            // �Ÿ��� �������� ���ϰ� �о�� ���� �Ÿ��� ������ ������ ����
            repulsionForce += diff.normalized / Mathf.Max(0.001f, diff.magnitude * diff.magnitude);
        }
        repulsionForce /= closeBoids.Count;
        return Steer(repulsionForce.normalized * maxSpeed);
    }

    // ���ϴ� �ӵ�(desired)�� ���� ���� �ʿ��� ���ӵ�(steering force)�� ���
    // �� ���� �ִ� ��(maxForce)���� ����
    private Vector2 Steer(Vector2 desired)
    {
        Vector2 steerForce = desired - velocity;
        return LimitMagnitude(steerForce, maxForce);
    }

    // ������ ũ�⸦ �����մϴ�.
    private Vector2 LimitMagnitude(Vector2 vector, float max)
    {
        return vector.sqrMagnitude > max * max ? vector.normalized * max : vector;
    }

    // --- Gizmos for Debugging ---
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            // �����Ϳ����� ��� �ڽ� �׸���
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(flockingBoundsCenter, flockingBoundsSize);
        }

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, neighborhoodRadius); // �̿� Ž�� �ݰ�
        Gizmos.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
        Gizmos.DrawWireSphere(transform.position, separationRadius); // �и� �ݰ�

        // ���� �ӵ�
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)velocity);

        // ���� ���ӵ�
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)acceleration * 10f); // ���ӵ��� �۰� ��Ÿ�� �� �����Ƿ� 10�� Ȯ��

#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            // ���� Steering ���� �ð�ȭ (�÷��� ��)
            var boidColliders = Physics2D.OverlapCircleAll(transform.position, neighborhoodRadius);
            var boids = boidColliders.Select(o => o.GetComponent<Flocking_Test>()).Where(b => b != null && b != this).ToList();

            // �� �� ���
            Vector2 align = Alignment(boids);
            Vector2 coh = Cohesion(boids);
            Vector2 sep = Separation(boids);
            Vector2 obstacleSteer = Vector2.zero; // ObstacleAvoidance ���ο��� ���� steerForce�� ������ ����� �ʿ��ϰų�, ���⿡ �ٽ� ��� ������ �־�� ��.
            Vector2 boundarySteer = Vector2.zero; // BoundaryAvoidance ���ο��� ���� steerForce�� ������ ����� �ʿ��ϰų�, ���⿡ �ٽ� ��� ������ �־�� ��.


            Gizmos.color = Color.blue; // ����
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)align * alignmentWeight);

            Gizmos.color = Color.green; // ����
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)coh * cohesionWeight);

            Gizmos.color = Color.red; // �и�
            Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)sep * separationWeight);

            // ��ֹ� ȸ�� (ObstacleAvoidance���� ���� steerForce�� ���� ���� �� �ִٸ� ����)
            // �ӽ÷� ����ĳ��Ʈ ����� ���� �뷫���� ���⸸ �ð�ȭ
            Vector2 currentForward = velocity.normalized;
            RaycastHit2D frontHit = Physics2D.CircleCast(transform.position, 0.2f, currentForward, raycastLength, obstacleLayer);
            if (frontHit.collider)
            {
                Gizmos.color = Color.magenta; // ��ֹ� ȸ��
                Gizmos.DrawLine(transform.position, (Vector3)transform.position + (Vector3)(transform.position - (Vector3)frontHit.point).normalized * obstacleAvoidanceWeight);
            }
        }
#endif
    }
}