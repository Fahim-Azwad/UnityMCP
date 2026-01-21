using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 6, -10);
    public float smoothSpeed = 5f;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("PlayerRoot");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        transform.LookAt(target);
    }
}
