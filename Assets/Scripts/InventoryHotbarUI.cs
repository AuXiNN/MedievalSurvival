using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// HUD hotbar: shows the 5 inventory slots and lets the player use one with the 1-5 keys.
/// Built by Tools > Medieval Survival > Build Inventory System.
/// </summary>
public class InventoryHotbarUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] slotLabels;

    void OnEnable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= Refresh;
    }

    void Update()
    {
        if (Keyboard.current == null || Inventory.Instance == null || slotLabels == null) return;

        for (int i = 0; i < slotLabels.Length; i++)
        {
            Key key = (Key)((int)Key.Digit1 + i); // Digit1..Digit9 are contiguous
            if (Keyboard.current[key].wasPressedThisFrame)
                Inventory.Instance.UseSlot(i);
        }
    }

    private void Refresh()
    {
        if (Inventory.Instance == null || slotLabels == null) return;

        for (int i = 0; i < slotLabels.Length; i++)
        {
            if (slotLabels[i] == null) continue;

            InventorySlot slot = Inventory.Instance.GetSlot(i);
            if (slot == null || slot.item == null)
            {
                slotLabels[i].text = (i + 1) + ". -";
            }
            else
            {
                string countSuffix = slot.item.stackable ? $" x{slot.count}" : "";
                slotLabels[i].text = $"{i + 1}. {slot.item.itemName}{countSuffix}";
            }
        }
    }
}
