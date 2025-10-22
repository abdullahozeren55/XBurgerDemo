using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhoneManager : MonoBehaviour
{
    public static PhoneManager Instance { get; private set; }

    public enum PhoneMenuType
    {
        MainMenu,
        IncomingCallMenu,
        FlashlightMenu
    }

    [System.Serializable]
    public class PhoneMenu
    {
        public PhoneMenuType type;
        public GameObject uiGO;
        public GameObject worldGO;
    }

    public PhoneMenu[] phoneMenus;

    [Header("Flashlight App Settings")]
    public Image[] flashlightPowerButtonImages; //0 ui, 1 world
    public Sprite[] flashlightPowerButtonSprites; //0 off, 1 on
    public GameObject flashlightGO; //The light
    private bool flashlightIsOn;

    private void Awake()
    {
        // Eðer Instance zaten varsa, bu nesneyi yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        flashlightIsOn = false;
    }

    public void HandleMainMenuButton()
    {
        foreach (PhoneMenu menu in phoneMenus)
        {
            if (menu.type != PhoneMenuType.MainMenu)
            {
                menu.uiGO.SetActive(false);
                menu.worldGO.SetActive(false);
            }
            else
            {
                menu.uiGO.SetActive(true);
                menu.worldGO.SetActive(true);
            }
        }
    }

    public void HandleFlashlightMenuButton()
    {
        foreach (PhoneMenu menu in phoneMenus)
        {
            if (menu.type != PhoneMenuType.FlashlightMenu)
            {
                menu.uiGO.SetActive(false);
                menu.worldGO.SetActive(false);
            }
            else
            {
                menu.uiGO.SetActive(true);
                menu.worldGO.SetActive(true);
            }
        }
    }

    public void HandleFlashlightPowerButton()
    {
        flashlightIsOn = !flashlightIsOn;

        flashlightPowerButtonImages[0].sprite = flashlightPowerButtonSprites[flashlightIsOn ? 1 : 0];
        flashlightPowerButtonImages[1].sprite = flashlightPowerButtonSprites[flashlightIsOn ? 1 : 0];

        flashlightGO.SetActive(flashlightIsOn);
    }
}
