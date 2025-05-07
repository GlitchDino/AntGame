using UnityEngine;

public class Food : MonoBehaviour
{
    public int hitPoints = 5;

    public void TakeBite()
    {
        hitPoints--;
        if (hitPoints <= 0)
        {
            Destroy(gameObject);
        }
    }
}
