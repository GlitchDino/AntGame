using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PheromoneFade : MonoBehaviour
{
    public enum PheromoneType { ToHome, ToFood }
    public PheromoneType type = PheromoneType.ToHome;

    [Header("Lifetime Settings")]
    [Tooltip("Base lifetime in seconds before random variance")]
    public float baseLifetime = -1f; // -1 means use default for type

    [Tooltip("Random variation added/subtracted to base lifetime")]
    public float randomVariance = 1f;

    private float lifetime;
    private float timer = 0f;
    private SpriteRenderer sr;
    private Color originalColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;

        // Set default lifetime based on type if user didn't override
        float defaultLifetime = type == PheromoneType.ToHome ? 6f : 10f;
        lifetime = baseLifetime > 0 ? baseLifetime : defaultLifetime;

        // Apply variance
        lifetime += Random.Range(-randomVariance, randomVariance);
        lifetime = Mathf.Max(1f, lifetime); // Clamp to avoid too-short/negative lifetimes
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifetime);

        Color faded = originalColor;
        faded.a = Mathf.Lerp(1f, 0f, t);
        sr.color = faded;

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
