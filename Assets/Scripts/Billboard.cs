using UnityEngine;

/// <summary>
/// Rotates this object to always face the main camera - used by the enemy health bar
/// (a small World Space Canvas floating above the enemy's head).
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera cam;

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
