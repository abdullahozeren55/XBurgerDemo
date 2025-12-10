using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonitorManager : MonoBehaviour
{
    public static MonitorManager Instance;

    [Header("Burger Page Settings")]
    public GameObject burgerPage;
    [Space]
    public Image burgerImage;
    public Sprite[] burgerSprites;
    [Space]
    public TMP_Text ingredientsTMP;
    public string[] ingredientKeys;
    [Space]
    public TMP_Text descriptionTMP;
    public string[] descriptionKeys;
    [Space]
    public TMP_Text headerTMP;
    public string[] headerKeys;

    [Header("Burger List Page Settings")]
    public GameObject burgerListPage;

    [Header("How To Page Settings")]
    public GameObject howToPage;

    [Header("Notepad Page Settings")]
    public GameObject notePadPage;

    [Header("Recycle Bin Page Settings")]
    public GameObject recycleBinPage;

    [Header("Deleted Note Page Settings")]
    public GameObject deletedNotePage;

    // --- EKLENEN KISIM: SEÇÝM YÖNETÝMÝ ---
    public DesktopIcon CurrentSelectedIcon { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Ýkonlar buraya "Ben seçildim abi" diyecek
    public void SelectIcon(DesktopIcon newIcon)
    {
        if (CurrentSelectedIcon != null && CurrentSelectedIcon != newIcon)
        {
            CurrentSelectedIcon.DeselectVisuals();
        }

        CurrentSelectedIcon = newIcon;
    }

    public void DeselectAll()
    {
        if (CurrentSelectedIcon != null)
        {
            CurrentSelectedIcon.DeselectVisuals();
            CurrentSelectedIcon = null;
        }
    }

    // ... (Burger Page kodlarýn aynen kalýyor) ...
    public void SetBurgerPage(int value)
    {
        burgerImage.sprite = burgerSprites[value];
        ingredientsTMP.text = LocalizationManager.Instance.GetText(ingredientKeys[value]);
        descriptionTMP.text = LocalizationManager.Instance.GetText(descriptionKeys[value]);
        headerTMP.text = LocalizationManager.Instance.GetText(headerKeys[value]);
        burgerPage.SetActive(true);
    }

    public void HandleBurgerPage(bool open)
    {
        burgerPage.SetActive(open);
    }

    public void HandleBurgerListPage(bool open)
    {
        burgerListPage.SetActive(open);
    }

    public void HandleHowToPage(bool open)
    {
        howToPage.SetActive(open);
    }

    public void HandleNotepadPage(bool open)
    {
        notePadPage.SetActive(open);
    }

    public void HandleRecycleBinPage(bool open)
    {
        recycleBinPage.SetActive(open);
    }

    public void HandleDeletedNotePage(bool open)
    {
        deletedNotePage.SetActive(open);
    }
}