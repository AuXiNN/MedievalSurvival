using UnityEngine;

/// <summary>
/// A world pickup that adds an ItemData to the player's Inventory on touch
/// (as opposed to PowerUp, which applies its effect immediately).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField] private int amount = 1;
    [SerializeField] private AudioClip pickupSound;

    [Header("Idle Animation")]
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float spinSpeed = 60f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, y, startPos.z);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || item == null) return;
        if (Inventory.Instance == null) return;

        bool added = Inventory.Instance.AddItem(item, amount);
        if (!added) return; // inventory full - leave it in the world

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, GameSettings.SfxVolume);

        Destroy(gameObject);
    }
}
