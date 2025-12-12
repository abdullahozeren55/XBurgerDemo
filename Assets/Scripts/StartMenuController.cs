using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StartMenuController : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public GameObject startMenuPanel; // Açýlacak olan menü paneli
    public GameObject startMenuPanelWorld; // Açýlacak olan menü paneli
    public Image buttonImage;         // Butonun kendi görseli
    public Image buttonImageWorld;         // Butonun kendi görseli

    [Header("Sprites")]
    public Sprite normalSprite;       // Normal (Dýþa çýkýk)
    public Sprite pressedSprite;      // Basýlý (Ýçe göçük)

    [Header("Helper")]
    // Menü açýkken ekrana döþeyeceðimiz görünmez kapatýcý
    // Bunu Inspector'dan atamana gerek yok, kodda dinamik halledeceðiz veya
    // sahnede hazýr tutabilirsin. Ben dinamik olaný deðil, manuel olaný anlatayým daha saðlam.
    public GameObject blockerObj;

    private bool isOpen = false;

    // Týklama Algýlama
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        ToggleMenu();
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        UpdateVisuals();
    }

    // Dýþarýdan (Blocker'dan) çaðrýlacak kapatma fonksiyonu
    public void CloseMenu()
    {
        isOpen = false;
        UpdateVisuals();
    }

    public void OpenMenuVisual()
    {
        isOpen = true;

        if (buttonImage != null)
            buttonImage.sprite = isOpen ? pressedSprite : normalSprite;

        if (buttonImageWorld != null)
            buttonImageWorld.sprite = isOpen ? pressedSprite : normalSprite;
    }

    private void UpdateVisuals()
    {
        // 1. Panel Aç/Kapa
        if (startMenuPanel != null)
            startMenuPanel.SetActive(isOpen);

        if (startMenuPanelWorld != null)
            startMenuPanelWorld.SetActive(isOpen);

        // 2. Görsel Deðiþtir
        if (buttonImage != null)
            buttonImage.sprite = isOpen ? pressedSprite : normalSprite;

        if (buttonImageWorld != null)
            buttonImageWorld.sprite = isOpen ? pressedSprite : normalSprite;

        // 3. Blocker (Dýþarý týklama engelleyici) Yönetimi
        if (blockerObj != null)
            blockerObj.SetActive(isOpen);
    }
}