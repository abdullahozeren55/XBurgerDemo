using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhoneManager : MonoBehaviour
{
    public static PhoneManager Instance { get; private set; }

    public bool IsFocused;

    public Phone PhoneSC;

    public enum PhoneMenuType
    {
        MainMenu,
        IncomingCallMenu,
        FlashlightMenu,
        AnsweredCallMenu,
        Null,
        NotesMenu,
        MessagesMenu
    }

    [System.Serializable]
    public class PhoneMenu
    {
        public PhoneMenuType type;
        public GameObject uiGO;
        public GameObject worldGO;
    }

    public PhoneMenu[] phoneMenus;
    [Space]
    public DialogueData[] dialogueDatasForPhoneCalls;
    private int dialogueDataNumToPlay;

    private PhoneMenuType currentMenu;
    private PhoneMenuType previousMenu;

    [Header("Flashlight App Settings")]
    public Image[] flashlightPowerButtonImages; //0 ui, 1 world
    public Sprite[] flashlightPowerButtonSprites; //0 off, 1 on
    public GameObject flashlightGO; //The light
    public bool FlashlightIsOn;

    [Header("Notes App Settings")]
    public TMP_Text missionTextUI;
    public TMP_Text missionTextWorld;

    [Header("Answered Call Settings")]
    public TMP_Text callTimerText;
    private float currentCallTime;
    private bool isInCall;

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

        FlashlightIsOn = false;
        isInCall = false;

        dialogueDataNumToPlay = 0;

        currentMenu = PhoneMenuType.MainMenu;
        previousMenu = PhoneMenuType.Null;
    }

    private void Update()
    {
        if (IsFocused)
        {
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Mouse1))
            {
                PhoneSC.FinishPhoneUI();
                IsFocused = false;
            }

            if (isInCall)
            {
                currentCallTime += Time.deltaTime;
                UpdateCallTimeText();
            }
        }
    }

    private void UpdateCallTimeText()
    {
        int hours = Mathf.FloorToInt(currentCallTime / 3600);
        int minutes = Mathf.FloorToInt((currentCallTime % 3600) / 60);
        int seconds = Mathf.FloorToInt(currentCallTime % 60);

        callTimerText.text = $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    public void HandleMainMenuButton()
    {
        if (currentMenu == PhoneMenuType.MainMenu) return;

        previousMenu = currentMenu;
        currentMenu = PhoneMenuType.MainMenu;

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

    public void HandleBackButton()
    {
        if (previousMenu == PhoneMenuType.Null) return;

        PhoneMenuType curMen = currentMenu;
        currentMenu = previousMenu;
        previousMenu = curMen;

        foreach (PhoneMenu menu in phoneMenus)
        {
            if (menu.type != currentMenu)
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
        previousMenu = currentMenu;
        currentMenu = PhoneMenuType.FlashlightMenu;

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
        FlashlightIsOn = !FlashlightIsOn;

        PhoneSC.PhoneState = FlashlightIsOn ? 1 : 0;

        flashlightPowerButtonImages[0].sprite = flashlightPowerButtonSprites[FlashlightIsOn ? 1 : 0];
        flashlightPowerButtonImages[1].sprite = flashlightPowerButtonSprites[FlashlightIsOn ? 1 : 0];

        flashlightGO.SetActive(FlashlightIsOn);
    }

    public void HandleNotesMenuButton()
    {
        previousMenu = currentMenu;
        currentMenu = PhoneMenuType.NotesMenu;

        foreach (PhoneMenu menu in phoneMenus)
        {
            if (menu.type != PhoneMenuType.NotesMenu)
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

    public void HandleMessagesMenuButton()
    {
        previousMenu = currentMenu;
        currentMenu = PhoneMenuType.MessagesMenu;

        foreach (PhoneMenu menu in phoneMenus)
        {
            if (menu.type != PhoneMenuType.MessagesMenu)
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
    public void SetMissionText(string text)
    {
        missionTextUI.text = text;
        missionTextWorld.text = text;
    }

    public void HandleAcceptTheCallButton()
    {
        foreach (PhoneMenu menu in phoneMenus)
        {
            if (menu.type != PhoneMenuType.AnsweredCallMenu)
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentCallTime = 0f;
        isInCall = true;

        StartCoroutine(StartPhoneDialogue());
    }

    public void HandleFinishingTheCall()
    {
        isInCall = false;

        StartCoroutine(FinishCallScreen());
    }

    private IEnumerator StartPhoneDialogue()
    {
        yield return new WaitForSeconds(1.5f);

        DialogueManager.Instance.StartPhoneDialogue(dialogueDatasForPhoneCalls[dialogueDataNumToPlay]);
        dialogueDataNumToPlay++;
    }

    private IEnumerator FinishCallScreen()
    {
        yield return new WaitForSeconds(0.5f);

        foreach (PhoneMenu menu in phoneMenus)
        {
            if (menu.type != currentMenu)
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
}
