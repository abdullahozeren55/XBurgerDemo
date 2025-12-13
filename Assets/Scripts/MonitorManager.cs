using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonitorManager : MonoBehaviour
{
    public static MonitorManager Instance;

    public bool IsFocused;

    public Monitor MonitorSC;

    [System.Serializable]
    public struct SongData
    {
        public string trackNameKey; // Örn: "Track 01 - Spooky Burger"
        public AudioClip clip;   // Müzik dosyasý
    }

    [Header("Burger Page Settings")]
    public GameObject burgerPage;
    public GameObject burgerPageWorld;
    [Space]
    public Image burgerImage;
    public Image burgerImageWorld;
    public Sprite[] burgerSprites;
    [Space]
    public TMP_Text ingredientsTMP;
    public TMP_Text ingredientsTMPWorld;
    public string[] ingredientKeys;
    [Space]
    public TMP_Text descriptionTMP;
    public TMP_Text descriptionTMPWorld;
    public string[] descriptionKeys;
    [Space]
    public TMP_Text headerTMP;
    public TMP_Text headerTMPWorld;
    public string[] headerKeys;

    [Header("Burger List Page Settings")]
    public GameObject burgerListPage;
    public GameObject burgerListPageWorld;

    [Header("How To Page Settings")]
    public GameObject howToPage;
    public GameObject howToPageWorld;

    [Header("Notepad Page Settings")]
    public GameObject notePadPage;
    public GameObject notePadPageWorld;
    public InputMirror notePadInputMirror;

    [Header("Recycle Bin Page Settings")]
    public GameObject recycleBinPage;
    public GameObject recycleBinPageWorld;

    [Header("Deleted Note Page Settings")]
    public GameObject deletedNotePage;
    public GameObject deletedNotePageWorld;

    [Header("Music Player Settings")]
    public GameObject musicPlayerPage;
    public GameObject musicPlayerPageWorld;
    public GameObject musicPlayerButton;
    public GameObject musicPlayerButtonWorld;
    public StartMenuController musicPlayerButtonScript;
    [Space]
    public AudioSource musicSource;
    public List<SongData> playlist;
    public bool MusicIsPlaying = false;
    public float musicVolumeMultiplier = 0.2f; //to prevent it from being too loud
    public string trackPrefixKey;
    private int currentIndex = 0;
    [Space]
    public RetroMarquee marqueeScript;
    public RetroMarquee marqueeScriptWorld;
    public float marqueeStepInterval = 0.4f; // Týrtýklý kayma hýzý burada
    [Space]
    public Image playPauseImage;
    public Image playPauseImageWorld;
    public Sprite[] playPauseSprites; //0 play, 1 pause
    


    // --- EKLENEN KISIM: SEÇÝM YÖNETÝMÝ ---
    public DesktopIcon CurrentSelectedIcon { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {

        if (playlist.Count > 0)
        {
            LoadTrack(0, false); // false = Otomatik baþlatma
        }

        StartCoroutine(MarqueeDriverLoop());
    }

    private void Update()
    {
        if (IsFocused)
        {
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Mouse1))
            {

                notePadInputMirror.SyncInputs();
                MonitorSC.FinishMonitorUI();
                IsFocused = false;
            }
        }
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
        burgerImageWorld.sprite = burgerImage.sprite;

        ingredientsTMP.text = LocalizationManager.Instance.GetText(ingredientKeys[value]);
        ingredientsTMPWorld.text = ingredientsTMP.text;

        descriptionTMP.text = LocalizationManager.Instance.GetText(descriptionKeys[value]);
        descriptionTMPWorld.text = descriptionTMP.text;

        headerTMP.text = LocalizationManager.Instance.GetText(headerKeys[value]);
        headerTMPWorld.text = headerTMP.text;

        burgerPage.SetActive(true);
        burgerPageWorld.SetActive(true);
    }

    public void HandleBurgerPage(bool open)
    {
        burgerPage.SetActive(open);
        burgerPageWorld.SetActive(open);
    }

    public void HandleBurgerListPage(bool open)
    {
        burgerListPage.SetActive(open);
        burgerListPageWorld.SetActive(open);
    }

    public void HandleHowToPage(bool open)
    {
        howToPage.SetActive(open);
        howToPageWorld.SetActive(open);
    }

    public void HandleNotepadPage(bool open)
    {
        notePadPage.SetActive(open);
        notePadPageWorld.SetActive(open);
    }

    public void HandleRecycleBinPage(bool open)
    {
        recycleBinPage.SetActive(open);
        recycleBinPageWorld.SetActive(open);
    }

    public void HandleDeletedNotePage(bool open)
    {
        deletedNotePage.SetActive(open);
        deletedNotePageWorld.SetActive(open);
    }

    public void HandleMusicPlayerPage(bool open)
    {
        musicPlayerPage.SetActive(open);
        musicPlayerPageWorld.SetActive(open);

        musicPlayerButton.SetActive(open);
        musicPlayerButtonWorld.SetActive(open);

        if (open)
        {
            musicPlayerButtonScript.OpenMenuVisual();

            if (!MusicIsPlaying)
                LoadTrack(currentIndex, false);
        }
        else
        {
            if (MusicIsPlaying)
            {
                MusicIsPlaying = false;
                musicSource.Stop();

                playPauseImage.sprite = MusicIsPlaying ? playPauseSprites[1] : playPauseSprites[0]; //if playing image should be pause, if paused image should be play
                playPauseImageWorld.sprite = playPauseImage.sprite;
            }
        }
    }

    public void PlayPauseMusic()
    {
        MusicIsPlaying = !MusicIsPlaying;

        playPauseImage.sprite = MusicIsPlaying ? playPauseSprites[1] : playPauseSprites[0]; //if playing image should be pause, if paused image should be play
        playPauseImageWorld.sprite = playPauseImage.sprite;

        if (MusicIsPlaying)
            musicSource.Play();
        else
            musicSource.Pause();
    }

    public void NextSong()
    {
        currentIndex++;

        // Listenin sonuna geldik mi? Baþa dön.
        if (currentIndex >= playlist.Count)
        {
            currentIndex = 0;
        }

        LoadTrack(currentIndex, true); // true = Þarkýyý deðiþtirdiðim için hemen çal
    }

    public void PreviousSong()
    {
        currentIndex--;

        // Listenin baþýna geldik mi? Sona dön.
        if (currentIndex < 0)
        {
            currentIndex = playlist.Count - 1;
        }

        LoadTrack(currentIndex, true);
    }

    // --- ÇEKÝRDEK MANTIK ---

    private void LoadTrack(int index, bool autoPlay)
    {
        if (playlist.Count == 0) return;

        SongData song = playlist[index];

        // 1. AudioSource'a klibi ver
        musicSource.clip = song.clip;

        // 2. LOCALIZATION ÝÞLEMÝ BURADA YAPILIYOR
        // Manager yoksa patlamasýn diye güvenlik kontrolü
        string localizedPrefix = "Track";
        string localizedSongName = song.trackNameKey; // Varsayýlan olarak key'i göster (Hata ayýklama için iyi)

        if (LocalizationManager.Instance != null)
        {
            // "Track" kelimesini çevir (Örn: "Parça")
            localizedPrefix = LocalizationManager.Instance.GetText(trackPrefixKey);

            // Þarký adýný çevir (Örn: "Gerilim Müziði")
            localizedSongName = LocalizationManager.Instance.GetText(song.trackNameKey);
        }

        // 3. Formatý birleþtir: [Parça 01] Gerilim Müziði
        string displayName = $"[{localizedPrefix} {index + 1:00}] {localizedSongName}";

        // 4. Kayan yazýya gönder
        if (marqueeScript != null)
        {
            marqueeScript.RefreshText(displayName);
        }

        if (marqueeScriptWorld != null)
        {
            marqueeScriptWorld.RefreshText(displayName);
        }

        if (autoPlay || musicSource.isPlaying)
        {
            musicSource.Play();
            MusicIsPlaying = true;
        }
    }

    public void SetMusicVolume(float value)
    {
        musicSource.volume = value * musicVolumeMultiplier;
    }

    private IEnumerator MarqueeDriverLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(marqueeStepInterval);

        while (true)
        {
            // Bekle
            yield return wait;

            // Eðer kayýtlý bir marquee varsa ve hazýrsa, onu dürtekle
            if (marqueeScript != null && marqueeScript.IsReady)
            {
                // UI kapalý (SetActive false) olsa bile
                // C# referansý memory'de yaþadýðý için bu fonksiyon çalýþýr
                // ve RectTransform deðerlerini günceller!
                marqueeScript.Step();
            }

            if (marqueeScriptWorld != null && marqueeScriptWorld.IsReady)
            {
                marqueeScriptWorld.Step();
            }
        }
    }
}