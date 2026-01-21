using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 3.0f;
    private Transform player;

    void Start()
    {
        GameObject playerObj = GameObject.Find("PlayerRoot");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Move towards player
        Vector3 direction = (player.position - transform.position).normalized;
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, player.position, step);

        // Rotate to face player
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }
}
