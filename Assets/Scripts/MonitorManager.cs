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

    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;
        }
        else
        {
            // If an instance already exists, destroy this one to enforce the singleton pattern
            Destroy(gameObject);
        }
    }

    public void HandleBurgerPage(int value)
    {
        burgerImage.sprite = burgerSprites[value];

        ingredientsTMP.text = LocalizationManager.Instance.GetText(ingredientKeys[value]);

        descriptionTMP.text = LocalizationManager.Instance.GetText(descriptionKeys[value]);

        headerTMP.text = LocalizationManager.Instance.GetText(headerKeys[value]);

        burgerPage.SetActive(true);
    }
}