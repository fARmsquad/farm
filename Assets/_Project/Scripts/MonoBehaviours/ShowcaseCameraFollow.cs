using UnityEngine;

/// <summary>
/// Smooth camera follow for the asset showcase scene.
/// </summary>
public class ShowcaseCameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 15, -10);
    [SerializeField] private float smoothSpeed = 5f;

    private Transform target;

    void Start()
    {
        var player = GameObject.Find("Player");
        if (player != null) target = player.transform;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
