using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target Tracking")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Dead Zone")]
    [SerializeField] private Vector2 deadZoneSize = new Vector2(3f, 2f);

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 currentCamPos = transform.position;
        Vector3 targetCamPos = currentCamPos;

        float halfDeadZoneWidth = deadZoneSize.x * 0.5f;
        float halfDeadZoneHeight = deadZoneSize.y * 0.5f;

        float deltaX = target.position.x - currentCamPos.x;
        float deltaY = target.position.y - currentCamPos.y;

        if (deltaX > halfDeadZoneWidth)
        {
            targetCamPos.x = target.position.x - halfDeadZoneWidth;
        }
        else if (deltaX < -halfDeadZoneWidth)
        {
            targetCamPos.x = target.position.x + halfDeadZoneWidth;
        }

        if (deltaY > halfDeadZoneHeight)
        {
            targetCamPos.y = target.position.y - halfDeadZoneHeight;
        }
        else if (deltaY < -halfDeadZoneHeight)
        {
            targetCamPos.y = target.position.y + halfDeadZoneHeight;
        }

        Vector3 smoothedPos = Vector3.MoveTowards(currentCamPos, targetCamPos, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPos;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(deadZoneSize.x, deadZoneSize.y, 0.1f));
    }
}