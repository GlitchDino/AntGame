using System.Collections.Generic;
using UnityEngine;

public class PheromoneSensor : MonoBehaviour
{
    [Header("Sensor Settings")]
    public float detectionRadius = 0.5f;
    public LayerMask pheromoneLayer;

    [HideInInspector] public int toFoodCount;
    [HideInInspector] public int toHomeCount;

    void Update()
    {
        ScanForPheromones();
    }

    void ScanForPheromones()
    {
        toFoodCount = 0;
        toHomeCount = 0;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, pheromoneLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("ToFoodPheromone"))
            {
                toFoodCount++;
            }
            else if (hit.CompareTag("ToHomePheromone"))
            {
                toHomeCount++;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
