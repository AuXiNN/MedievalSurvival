using UnityEngine;

/// <summary>
/// Rotates this object to always face the main camera - used by the enemy health bar
/// (a small World Space Canvas floating above the enemy's head).
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera cam; // cached reference to the main camera so we don't call Camera.main every frame

    // LateUpdate runs after all normal Update calls, so the camera has already finished
    // moving this frame before we rotate to face it - avoids one-frame lag/jitter.
    void LateUpdate()
    {
        if (cam == null) // lazily find the main camera the first time we need it
        {
            cam = Camera.main;
            if (cam == null) return; // no camera in the scene yet, skip this frame
        }

        // Point this object's forward direction away from the camera so its front face
        // (the health bar canvas) is always what the camera sees, like a sprite billboard.
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
