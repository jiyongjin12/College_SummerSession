using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Flocking_Test : MonoBehaviour
{
    public Vector3 baseRotation;
    public float maxSpeed = 3f;
    public float maxForce = 0.01f;
    public float neighborhoodRadius = 1.2f;
    public float separationAmount = 1.2f;
    public float cohesionAmount = 1f;
    public float alignmentAmount = 1f;
    public float obstacleAvoidanceAmount = 2f;
    public float boundsAvoidanceAmount = 2f;

    public Vector2 acceleration;
    public Vector2 velocity;

    // 추가: 범위
    private Vector2 boundsCenter;
    private Vector2 boundsSize;

    public void SetBounds(Vector2 center, Vector2 size)
    {
        boundsCenter = center;
        boundsSize = size;
    }

    private Vector2 Position
    {
        get => transform.position;
        set => transform.position = value;
    }

    private void Start()
    {
        float angle = Random.Range(0, 2 * Mathf.PI);
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle) + baseRotation);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private void Update()
    {
        var boidColliders = Physics2D.OverlapCircleAll(Position, neighborhoodRadius);
        var boids = boidColliders.Select(o => o.GetComponent<Flocking_Test>()).Where(b => b != null && b != this).ToList();

        Flock(boids);
        ObstacleAvoidance();
        BoundaryAvoidance();

        UpdateVelocity();
        UpdatePosition();
        UpdateRotation();
    }

    private void Flock(IEnumerable<Flocking_Test> boids)
    {
        var alignment = Alignment(boids);
        var separation = Separation(boids);
        var cohesion = Cohesion(boids);

        acceleration = alignmentAmount * alignment + cohesionAmount * cohesion + separationAmount * separation;

        //

        //var alignment = Alignment(boids);
        //var cohesion = Cohesion(boids);
        //var separation = Separation(boids);

        //// 힘을 Lerp로 부드럽게 적용
        //alignment = Vector2.Lerp(Vector2.zero, alignment, 0.5f);
        //cohesion = Vector2.Lerp(Vector2.zero, cohesion, 0.4f);
        //separation = Vector2.Lerp(Vector2.zero, separation, 0.6f);

        //acceleration += separationAmount * separation + cohesionAmount * cohesion + alignmentAmount * alignment;
    }

    private void ObstacleAvoidance()
    {
        Vector2 forward = velocity.normalized;
        Vector2 left = Quaternion.Euler(0, 0, 30) * forward;
        Vector2 right = Quaternion.Euler(0, 0, -30) * forward;

        float frontLength = 1.5f;
        float sideLength = 1.5f;

        RaycastHit2D frontHit = Physics2D.CircleCast(Position, 0.2f, forward, frontLength, LayerMask.GetMask("Wall"));
        RaycastHit2D leftHit = Physics2D.CircleCast(Position, 0.2f, left, sideLength, LayerMask.GetMask("Wall"));
        RaycastHit2D rightHit = Physics2D.CircleCast(Position, 0.2f, right, sideLength, LayerMask.GetMask("Wall"));

        // Gizmo
        Debug.DrawRay(Position, forward * frontLength, Color.white);
        Debug.DrawRay(Position, left * sideLength, leftHit.collider ? Color.red : Color.green);
        Debug.DrawRay(Position, right * sideLength, rightHit.collider ? Color.red : Color.green);

        int hitCount = 0;
        Vector2 steer = Vector2.zero;

        if (leftHit.collider)
        {
            hitCount++;
            steer += (Vector2)leftHit.normal;
        }
        if (rightHit.collider)
        {
            hitCount++;
            steer += (Vector2)rightHit.normal;
        }

        // 좌/우 둘 다 감지 → 벽에 갇힘 → 반대방향으로 피함
        if (hitCount == 2)
        {
            steer = -forward;
        }

        if (hitCount > 0)
        {
            acceleration += Steer(steer.normalized * maxSpeed) * obstacleAvoidanceAmount;
        }
    }

    private void BoundaryAvoidance()
    {
        Vector2 min = boundsCenter - boundsSize / 2f;
        Vector2 max = boundsCenter + boundsSize / 2f;
        Vector2 steer = Vector2.zero;
        Vector2 desired = Vector2.zero;

        if (Position.x < min.x || Position.x > max.x || Position.y < min.y || Position.y > max.y)
        {
            desired = (boundsCenter - Position).normalized * maxSpeed;
            steer = Steer(desired);

            float distance = Vector2.Distance(Position, boundsCenter);
            float strength = Mathf.Clamp01(distance / 5f); // 경계에서 멀수록 강하게
            acceleration += steer * boundsAvoidanceAmount * strength;
        }
    }

    public void UpdateVelocity()
    {
        acceleration = Vector2.Lerp(acceleration, Vector2.zero, Time.deltaTime * 2f); // 감쇠
        velocity += acceleration * Time.deltaTime * 60f; // 60프레임 기준 보정

        float minSpeed = maxSpeed * 0.4f;
        if (velocity.magnitude < minSpeed)
        {
            velocity = velocity.normalized * minSpeed;
        }

        velocity = LimitMagnitude(velocity, maxSpeed);
    }

    private void UpdatePosition() => Position += velocity * Time.deltaTime;

    private void UpdateRotation()
    {
        var angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle) + baseRotation);
    }

    private Vector2 Alignment(IEnumerable<Flocking_Test> boids)
    {
        if (!boids.Any()) return Vector2.zero;
        var averageVelocity = boids.Aggregate(Vector2.zero, (sum, b) => sum + b.velocity) / boids.Count();
        return Steer(averageVelocity.normalized * maxSpeed);
    }

    private Vector2 Cohesion(IEnumerable<Flocking_Test> boids)
    {
        if (!boids.Any()) return Vector2.zero;
        var centerOfMass = boids.Aggregate(Vector2.zero, (sum, b) => sum + b.Position) / boids.Count();
        return Steer((centerOfMass - Position).normalized * maxSpeed);
    }

    private Vector2 Separation(IEnumerable<Flocking_Test> boids)
    {
        var closeBoids = boids.Where(b => DistanceTo(b) <= neighborhoodRadius / 2).ToList();
        if (!closeBoids.Any()) return Vector2.zero;

        Vector2 repulsion = Vector2.zero;
        foreach (var b in closeBoids)
        {
            Vector2 diff = Position - b.Position;
            repulsion += diff.normalized / diff.magnitude;
        }
        repulsion /= closeBoids.Count;
        return Steer(repulsion.normalized * maxSpeed);
    }

    private Vector2 Steer(Vector2 desired)
    {
        // 회피 방향이 너무 튀지 않게 부드럽게 회전 (Slerp로 혼합)
        Vector2 smoothed = ((Vector3)Vector3.Slerp((Vector3)velocity.normalized, (Vector3)desired.normalized, 0.5f));
        desired = smoothed * maxSpeed;

        Vector2 steer = desired - velocity;
        steer = LimitMagnitude(steer, maxForce);
        return steer;
    }

    private float DistanceTo(Flocking_Test boid) => Vector2.Distance(Position, boid.Position);

    private Vector2 LimitMagnitude(Vector2 v, float max)
    {
        return v.sqrMagnitude > max * max ? v.normalized * max : v;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, neighborhoodRadius); // 탐지 반경

        // Velocity
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + velocity);

        // Acceleration
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + acceleration);

#if UNITY_EDITOR
        // 이웃 정보는 플레이 중에만 보여짐
        if (Application.isPlaying)
        {
            // 실제 Steering 벡터 계산
            var boidColliders = Physics2D.OverlapCircleAll(Position, neighborhoodRadius);
            var boids = boidColliders.Select(o => o.GetComponent<Flocking_Test>()).Where(b => b != null && b != this).ToList();

            Vector2 align = Alignment(boids);
            Vector2 coh = Cohesion(boids);
            Vector2 sep = Separation(boids);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + align);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + coh);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + sep);
        }
#endif
    }
}