using UnityEngine;

public class AntColony : MonoBehaviour
{
    public GameObject antPrefab;
    public float spawnRadius = 0.1f; // Small offset so ants don't overlap colony
    public int antCount = 8;

    void Start()
    {
        SpawnAnts();
    }

    void SpawnAnts()
    {
        float angleStep = 360f / antCount;

        for (int i = 0; i < antCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            Vector2 spawnPosition = (Vector2)transform.position + direction * spawnRadius;

            GameObject ant = Instantiate(antPrefab, spawnPosition, Quaternion.identity);

            Ant antScript = ant.GetComponent<Ant>();
            if (antScript != null)
            {
                antScript.enabled = true;

                antScript.SendMessage("SetInitialDirection", direction, SendMessageOptions.DontRequireReceiver);

                // Set the colony origin
                antScript.colonyOrigin = this.transform;
            }

        }
    }
}
