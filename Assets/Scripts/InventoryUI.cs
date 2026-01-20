using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Image background;
        public Image icon;
    }

    [Header("Standard Inventory References")]
    public RectTransform standardSlotsParent;
    public SlotUI[] slots;

    [Header("Tray Slot References")]
    public RectTransform traySlotParent;
    public SlotUI traySlot;

    [Header("Animation Positions")]
    public float standardOffScreenX = -500f;
    public float standardOnScreenX = 50f;

    public float trayOffScreenY = -200f;
    public float trayOnScreenY = 50f;

    [Header("Shared Settings")]
    public float animDuration = 0.25f;
    public Ease transitionEase = Ease.OutBack;

    [Header("Size Settings (Vector2)")]
    public Vector2 defaultBackSize = new Vector2(80f, 80f);
    public Vector2 selectedBackSize = new Vector2(100f, 100f);
    public Vector2 defaultIconSize = new Vector2(60f, 60f);
    public Vector2 selectedIconSize = new Vector2(80f, 80f);

    [Header("Color Settings")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = new Color(0, 0, 0, 0.5f);
    public Color emptySlotColor = new Color(0, 0, 0, 0.2f);

    private void Start()
    {
        // --- 2. SORUN ÇÖZÜMÜ: BAÞLANGIÇ POZÝSYONLARI ---

        // Standart slotlar ekranda baþlasýn
        if (standardSlotsParent != null)
            standardSlotsParent.anchoredPosition = new Vector2(standardOnScreenX, standardSlotsParent.anchoredPosition.y);

        // Tepsi slotu ekran dýþýnda (aþaðýda) baþlasýn
        if (traySlotParent != null)
            traySlotParent.anchoredPosition = new Vector2(traySlotParent.anchoredPosition.x, trayOffScreenY);

        // Tepsi slotunun içi baþta boþ görünsün
        UpdateSingleSlot(traySlot, null, false);
    }

    public void UpdateDisplay(IGrabable[] items, int activeSlotIndex)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            UpdateSingleSlot(slots[i], (i < items.Length ? items[i] : null), (i == activeSlotIndex));
        }
    }

    public void UpdateTrayDisplay(IGrabable trayItem)
    {
        // --- 1. SORUN ÇÖZÜMÜ: RENKLENDÝRME ---
        // Tepsi slotunu güncellerken de "UpdateSingleSlot" kullanýyoruz.
        // Böylece renk, boyut ve ikon ayarlarý standart slotlarla birebir ayný çalýþýr.
        // isSelected = true yolluyoruz çünkü tepsi elimizdeyse o slot seçilidir.
        UpdateSingleSlot(traySlot, trayItem, true);
    }

    // Ortak Güncelleme Fonksiyonu
    private void UpdateSingleSlot(SlotUI slot, IGrabable item, bool isSelected)
    {
        bool hasItem = (item != null);

        // Ýkon Ýþlemleri
        if (hasItem)
        {
            slot.icon.gameObject.SetActive(true);
            slot.icon.sprite = item.IconData.sprite;
            slot.icon.rectTransform.anchoredPosition = item.IconData.offset;
        }
        else
        {
            slot.icon.gameObject.SetActive(false);
            slot.icon.sprite = null;
        }

        // Renk Ýþlemleri (Burada senin istediðin mantýk çalýþacak)
        if (isSelected)
            slot.background.color = selectedColor;
        else
            slot.background.color = hasItem ? normalColor : emptySlotColor;

        // Animasyon
        AnimateSlot(slot, isSelected);
    }

    public void SwitchToTrayMode(IGrabable trayItem)
    {
        UpdateTrayDisplay(trayItem);

        standardSlotsParent.DOKill();
        standardSlotsParent.DOAnchorPosX(standardOffScreenX, animDuration).SetEase(transitionEase);

        traySlotParent.DOKill();
        traySlotParent.DOAnchorPosY(trayOnScreenY, animDuration).SetEase(transitionEase);
    }

    public void SwitchToStandardMode()
    {
        standardSlotsParent.DOKill();
        standardSlotsParent.DOAnchorPosX(standardOnScreenX, animDuration).SetEase(transitionEase);

        traySlotParent.DOKill();
        traySlotParent.DOAnchorPosY(trayOffScreenY, animDuration).SetEase(transitionEase);
    }

    private void AnimateSlot(SlotUI slot, bool isSelected)
    {
        Vector2 targetBackSize = isSelected ? selectedBackSize : defaultBackSize;
        Vector2 targetIconSize = isSelected ? selectedIconSize : defaultIconSize;
        Ease currentEase = isSelected ? Ease.OutBack : Ease.OutQuad;

        slot.background.rectTransform.DOKill();
        slot.background.rectTransform.DOSizeDelta(targetBackSize, animDuration).SetEase(currentEase);

        slot.icon.rectTransform.DOKill();
        if (slot.icon.gameObject.activeSelf)
        {
            slot.icon.rectTransform.DOSizeDelta(targetIconSize, animDuration).SetEase(currentEase);
        }
    }
}