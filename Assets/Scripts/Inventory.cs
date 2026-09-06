using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int count;
}

/// <summary>
/// Fixed-size inventory (a hotbar): collects potions/weapons via ItemPickup and lets the
/// player use them with the 1-5 keys (see InventoryHotbarUI). Potions are consumable and
/// stack; weapons are equipped once (applies a permanent damage bonus) and then removed.
/// </summary>
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [SerializeField] private int slotCount = 5;
    private InventorySlot[] slots;

    private PlayerHealth playerHealth;
    private PlayerCombat playerCombat;

    /// <summary>Raised whenever a slot's contents change, so the UI can refresh.</summary>
    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
            slots[i] = new InventorySlot();
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerCombat = playerObj.GetComponent<PlayerCombat>();
        }
    }

    public int SlotCount => slots.Length;

    public InventorySlot GetSlot(int index)
    {
        return (slots != null && index >= 0 && index < slots.Length) ? slots[index] : null;
    }

    /// <summary>
    /// Adds an item, stacking onto an existing slot first, then filling the next empty slot.
    /// Returns false if the inventory is full and nothing more could fit.
    /// </summary>
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (item.stackable)
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot.item != item || slot.count >= item.maxStack) continue;

                int space = item.maxStack - slot.count;
                int toAdd = Mathf.Min(space, amount);
                slot.count += toAdd;
                amount -= toAdd;

                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot.item != null) continue;

            slot.item = item;
            slot.count = item.stackable ? Mathf.Min(amount, item.maxStack) : 1;
            amount -= slot.count;

            if (amount <= 0)
            {
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        OnInventoryChanged?.Invoke();
        if (amount > 0)
            Debug.Log("Inventory full - couldn't fit " + amount + "x " + item.itemName);

        return amount <= 0;
    }

    /// <summary>Uses (consumes/equips) whatever is in the given slot.</summary>
    public void UseSlot(int index)
    {
        InventorySlot slot = GetSlot(index);
        if (slot == null || slot.item == null) return;

        ItemData item = slot.item;

        switch (item.type)
        {
            case ItemType.Potion:
                if (playerHealth != null) playerHealth.Heal(item.healAmount);
                slot.count--;
                if (slot.count <= 0)
                {
                    slot.item = null;
                    slot.count = 0;
                }
                break;

            case ItemType.Weapon:
                if (playerCombat != null) playerCombat.AddDamageBonus(item.damageBonus);
                Debug.Log("Equipped " + item.itemName + " (+" + item.damageBonus + " damage)");
                slot.item = null;
                slot.count = 0;
                break;
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Empties every slot - used on a full game restart.</summary>
    public void Clear()
    {
        foreach (InventorySlot slot in slots)
        {
            slot.item = null;
            slot.count = 0;
        }
        OnInventoryChanged?.Invoke();
    }
}
