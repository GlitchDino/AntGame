using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Ant : MonoBehaviour
{
    public enum AntState { Wander, ReturnHome }

    [Header("Settings")]
    public float moveSpeed = 2f;
    public float acceleration = 5f;
    public float steeringWeight = 1f;
    public float dropDistance = 1f;
    public float detectionRadius = 0.6f;
    public float turnAroundDuration = 1.5f;
    public float obstacleAvoidStrength = 3f;
    public float antennaLength = 0.5f;
    public float facingOffset = 0f;

    [Header("Wandering")]
    public float wanderStrength = 0.5f;
    public float wanderUpdateInterval = 1f;

    [Header("Perception")]
    public int rayCount = 7;
    public float coneAngle = 60f;
    public float visionDistance = 2f;

    [Header("Pheromone Trail Response")]
    public float trailSlowSpeed = 1f;
    public float trailRecoverSpeed = 2f;
    public float pheromoneTrackingDuration = 1f;

    [Header("Targets")]
    public Transform colonyOrigin;
    public LayerMask foodLayer;
    public LayerMask obstacleLayer;
    public GameObject toHomeMarkerPrefab;
    public GameObject toFoodMarkerPrefab;

    public Transform antennaLeft;
    public Transform antennaRight;
    public PheromoneSensor leftSensor;
    public PheromoneSensor centerSensor;
    public PheromoneSensor rightSensor;

    private Rigidbody2D rb;
    private Vector2 currentVelocity;
    private Vector2 steeringForce;
    private Vector2 lastDropPos;
    private Vector2 turnAroundForce;
    private Vector2 currentWanderTarget;

    private Vector2 persistentAvoidance = Vector2.zero;
    private float avoidDecayRate = 2f;
    private float lastAvoidTime = 0f;

    private AntState currentState = AntState.Wander;
    private bool hasFood = false;
    private Food targetFood;
    private bool turningAround = false;
    private float turnAroundEndTime = 0f;
    private float nextWanderUpdateTime = 0f;
    private float targetSpeed;
    private float pheromoneTrackingTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentVelocity = Random.insideUnitCircle.normalized * moveSpeed;
        lastDropPos = rb.position;
        targetSpeed = moveSpeed;
    }

    void Update()
    {
        if (currentState == AntState.Wander)
        {
            LookForFood();
        }
        else if (currentState == AntState.ReturnHome)
        {
            SteerToward(colonyOrigin.position);
        }

        HandleObstacleAvoidance();

        if (Vector2.Distance(rb.position, lastDropPos) >= dropDistance)
        {
            DropMarker();
            lastDropPos = rb.position;
        }
    }

    void FixedUpdate()
    {
        ApplySteering();
    }

    void LookForFood()
    {
        if (hasFood) return;

        Vector2 origin = transform.position;
        Vector2 forward = currentVelocity.normalized;
        float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - coneAngle / 2f;
        float angleStep = coneAngle / (rayCount - 1);

        bool sawTarget = false;

        if (targetFood != null)
        {
            for (int i = 0; i < rayCount; i++)
            {
                float angle = startAngle + i * angleStep;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                RaycastHit2D hit = Physics2D.Raycast(origin, dir, visionDistance, foodLayer);
                Debug.DrawRay(origin, dir * visionDistance, Color.red, 0.1f);

                if (hit.collider != null && hit.collider.TryGetComponent<Food>(out Food seenFood))
                {
                    if (seenFood == targetFood)
                    {
                        SteerToward(seenFood.transform.position);
                        sawTarget = true;
                        break;
                    }
                }
            }

            if (!sawTarget)
            {
                targetFood = null;
            }
        }

        if (targetFood == null)
        {
            for (int i = 0; i < rayCount; i++)
            {
                float angle = startAngle + i * angleStep;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                RaycastHit2D hit = Physics2D.Raycast(origin, dir, visionDistance, foodLayer);
                Debug.DrawRay(origin, dir * visionDistance, Color.green, 0.1f);

                if (hit.collider != null && hit.collider.TryGetComponent<Food>(out Food food))
                {
                    targetFood = food;
                    SteerToward(food.transform.position);
                    sawTarget = true;
                    break;
                }
            }
        }

        if (pheromoneTrackingTimer > 0f)
        {
            pheromoneTrackingTimer -= Time.deltaTime;
            return;
        }

        if (targetFood == null)
        {
            int left = leftSensor.toFoodCount;
            int center = centerSensor.toFoodCount;
            int right = rightSensor.toFoodCount;

            Vector2 steer = Vector2.zero;
            steer += left * (Vector2)(-transform.right) * 0.5f;
            steer += center * (Vector2)transform.up;
            steer += right * (Vector2)transform.right * 0.5f;

            if (left + center + right > 0 && steer.sqrMagnitude > 0.01f)
            {
                Debug.DrawRay(transform.position, steer.normalized, Color.yellow, 0.1f);
                SteerToward(rb.position + steer.normalized);
                targetSpeed = trailSlowSpeed;
                pheromoneTrackingTimer = pheromoneTrackingDuration;
                return;
            }
            else
            {
                targetSpeed = Mathf.MoveTowards(targetSpeed, moveSpeed, trailRecoverSpeed * Time.deltaTime);
            }
        }

        if (targetFood == null && Time.time >= nextWanderUpdateTime)
        {
            Vector2 forwardDir = currentVelocity.normalized;
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            currentWanderTarget = rb.position + (forwardDir + randomOffset).normalized * wanderStrength;
            nextWanderUpdateTime = Time.time + wanderUpdateInterval;
        }

        if (targetFood == null)
        {
            SteerToward(currentWanderTarget);
        }
    }

    void SteerToward(Vector2 target)
    {
        Vector2 desiredDirection = (target - rb.position).normalized;
        steeringForce = desiredDirection * steeringWeight;
    }

    void ApplySteering()
    {
        Vector2 totalSteering = Vector2.ClampMagnitude(steeringForce, steeringWeight);

        if (turningAround && Time.time < turnAroundEndTime)
        {
            totalSteering += turnAroundForce * steeringWeight;
        }
        else
        {
            turningAround = false;
        }

        Vector2 desiredVelocity = totalSteering.normalized * targetSpeed;
        Vector2 steering = desiredVelocity - currentVelocity;
        Vector2 accelerationVector = Vector2.ClampMagnitude(steering * acceleration, acceleration);

        currentVelocity += accelerationVector * Time.fixedDeltaTime;
        currentVelocity = Vector2.ClampMagnitude(currentVelocity, targetSpeed);

        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);

        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + facingOffset);
        }
    }

    void DropMarker()
    {
        GameObject prefab = hasFood ? toFoodMarkerPrefab : toHomeMarkerPrefab;
        if (prefab != null)
        {
            Instantiate(prefab, rb.position, Quaternion.identity);
        }
    }

    void HandleObstacleAvoidance()
    {
        RaycastHit2D hitLeft = Physics2D.Raycast(antennaLeft.position, antennaLeft.right, antennaLength, obstacleLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(antennaRight.position, antennaRight.right, antennaLength, obstacleLayer);

        if (hitLeft || hitRight)
        {
            RaycastHit2D hit = hitLeft ? hitLeft : hitRight;
            Vector2 normal = hit.normal;
            persistentAvoidance = normal * obstacleAvoidStrength;
            lastAvoidTime = Time.time;

            nextWanderUpdateTime = 0f;
            currentVelocity += persistentAvoidance.normalized * 0.1f;
        }

        if (Time.time - lastAvoidTime < 0.5f)
        {
            steeringForce += persistentAvoidance;
        }
        else
        {
            persistentAvoidance = Vector2.Lerp(persistentAvoidance, Vector2.zero, avoidDecayRate * Time.deltaTime);
        }
    }

    void StartTurnAround()
    {
        turningAround = true;
        turnAroundEndTime = Time.time + turnAroundDuration;
        turnAroundForce = -currentVelocity.normalized;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasFood && other.TryGetComponent<Food>(out Food food) && food == targetFood)
        {
            food.TakeBite();
            hasFood = true;
            targetFood = null;
            GetComponent<SpriteRenderer>().color = Color.yellow;
            currentState = AntState.ReturnHome;
        }
        else if (hasFood && other.transform == colonyOrigin)
        {
            hasFood = false;
            GetComponent<SpriteRenderer>().color = Color.white;
            currentState = AntState.Wander;

            Vector2 awayFromColony = (rb.position - (Vector2)colonyOrigin.position).normalized;
            currentVelocity = awayFromColony * moveSpeed;
            nextWanderUpdateTime = 0f;
        }
    }

    void OnDrawGizmos()
    {
        if (leftSensor != null)
        {
            Gizmos.color = leftSensor.toFoodCount > 0 ? Color.yellow : Color.gray;
            Gizmos.DrawSphere(leftSensor.transform.position, 0.05f);
        }
        if (centerSensor != null)
        {
            Gizmos.color = centerSensor.toFoodCount > 0 ? Color.yellow : Color.gray;
            Gizmos.DrawSphere(centerSensor.transform.position, 0.05f);
        }
        if (rightSensor != null)
        {
            Gizmos.color = rightSensor.toFoodCount > 0 ? Color.yellow : Color.gray;
            Gizmos.DrawSphere(rightSensor.transform.position, 0.05f);
        }
    }
}
