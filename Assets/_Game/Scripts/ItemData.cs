using UnityEngine;

public enum ItemType
{
    Potion,
    Weapon
}

/// <summary>
/// Defines a collectible inventory item (as opposed to PowerUp, which applies instantly
/// on touch). Create instances via Assets > Create > Medieval Survival > Item, or let
/// Tools > Medieval Survival > Build Inventory System generate the starter set.
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Medieval Survival/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";
    [TextArea] public string description;
    public ItemType type = ItemType.Potion;
    public Sprite icon;

    public bool stackable = true;
    public int maxStack = 99;

    [Header("Potion Effect (used if type = Potion)")]
    public int healAmount = 30;

    [Header("Weapon Effect (used if type = Weapon)")]
    public int damageBonus = 10;
}
