using DG.Tweening; // DOTween kütüphanesini ekledik
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Image background; // Slotun çerçevesi
        public Image icon;       // Ýçindeki eþyanýn resmi
    }

    [Header("UI References")]
    public SlotUI[] slots;

    [Header("Size Settings (Vector2)")]
    public Vector2 defaultBackSize = new Vector2(80f, 80f);
    public Vector2 selectedBackSize = new Vector2(100f, 100f);
    [Space]
    public Vector2 defaultIconSize = new Vector2(60f, 60f);
    public Vector2 selectedIconSize = new Vector2(80f, 80f);

    [Header("Animation Settings")]
    public float animDuration = 0.25f;

    [Header("Ease Types")]
    // Seçilirken "Pop" etkisi için OutBack harikadýr (hafif taþar geri gelir)
    public Ease selectEase = Ease.OutBack;
    // Seçimden çýkarken daha yumuþak sönmesi için OutQuad veya InQuad iyidir
    public Ease deselectEase = Ease.OutQuad;

    [Header("Color Settings")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = new Color(0, 0, 0, 0.5f);
    public Color emptySlotColor = new Color(0, 0, 0, 0.2f);

    public void UpdateDisplay(IGrabable[] items, int activeSlotIndex)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            SlotUI currentSlot = slots[i];
            bool isSelected = (i == activeSlotIndex);

            // Dizi sýnýrlarýný aþmayalým (gerçi fixed 4 ama olsun)
            // VE item null deðilse doludur.
            bool hasItem = (i < items.Length && items[i] != null);

            // 1. ÝKON GÖRÜNÜRLÜÐÜ
            if (hasItem)
            {
                currentSlot.icon.gameObject.SetActive(true);
                currentSlot.icon.sprite = items[i].IconData.sprite;
                currentSlot.icon.rectTransform.anchoredPosition = items[i].IconData.offset;
            }
            else
            {
                currentSlot.icon.gameObject.SetActive(false);
                currentSlot.icon.sprite = null;
                currentSlot.icon.rectTransform.anchoredPosition = Vector2.zero;
            }

            // 2. RENK AYARI
            if (isSelected)
                currentSlot.background.color = selectedColor;
            else
                currentSlot.background.color = hasItem ? normalColor : emptySlotColor;

            // 3. DOTWEEN
            AnimateSlot(currentSlot, isSelected);
        }
    }

    private void AnimateSlot(SlotUI slot, bool isSelected)
    {
        // Hedef Boyutlarý Belirle
        Vector2 targetBackSize = isSelected ? selectedBackSize : defaultBackSize;
        Vector2 targetIconSize = isSelected ? selectedIconSize : defaultIconSize;

        // Ease Tipini Belirle (Büyürken zýplak, küçülürken sakin)
        Ease currentEase = isSelected ? selectEase : deselectEase;

        // --- BACKGROUND TWEEN ---
        // Önceki tween varsa kafasýna sýk (DOKill)
        slot.background.rectTransform.DOKill();

        // DOSizeDelta: RectTransform'un Width/Height deðerlerini tweenler
        slot.background.rectTransform.DOSizeDelta(targetBackSize, animDuration)
            .SetEase(currentEase);

        // --- ICON TWEEN ---
        slot.icon.rectTransform.DOKill();

        // Sadece ikon aktifse boyutlandýr, deðilse boþuna iþlem yapma
        if (slot.icon.gameObject.activeSelf)
        {
            slot.icon.rectTransform.DOSizeDelta(targetIconSize, animDuration)
                .SetEase(currentEase);
        }
    }
}