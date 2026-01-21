using UnityEngine;

public class EnemySetup : MonoBehaviour
{
    void Start()
    {
        GameObject enemy = GameObject.Find("Enemy");
        if (enemy != null && enemy.GetComponent<EnemyAI>() == null)
        {
            enemy.AddComponent<EnemyAI>();
            Debug.Log("EnemyAI component added!");
            Destroy(this.gameObject);
        }
    }
}
