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

    public Transform windowParent;

    [System.Serializable]
    public struct SongData
    {
        public string trackNameKey; // Örn: "Track 01 - Spooky Burger"
        public AudioClip clip;   // Müzik dosyasý
    }

    public enum PageState
    {
        Closed,
        Opened,
        Minimized
    }

    [Header("Burger Page Settings")]
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
    [Space]
    private int currentBurgerIndex = 0;

    [Header("Notepad Page Settings")]
    public InputMirror notePadInputMirror;

    [Header("Music Player Settings")]
    public PageState musicPlayerPageState;
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

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            // Dil deðiþirse "RefreshPage" çalýþsýn
            LocalizationManager.Instance.OnLanguageChanged += RefreshPage;
        }
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= RefreshPage;
        }
    }

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

    public void FocusWindow(WindowController targetWindow)
    {
        // 1. Hedef pencereyi hiyerarþide EN ALTA (yani en öne) taþý
        targetWindow.SetLast();

        // 2. Tüm pencereleri gez ve renklerini ayarla
        foreach (Transform child in windowParent)
        {
            // Sadece açýk olan ve WindowController'ý olanlara bak
            if (!child.gameObject.activeSelf) continue;

            WindowController wc = child.GetComponent<WindowController>();
            if (wc != null)
            {
                // Eðer döngüdeki pencere bizim hedefimizse -> AKTÝF yap
                // Deðilse -> PASÝF yap
                bool isTarget = (wc == targetWindow);
                wc.SetState(isTarget);
            }
        }
    }

    public void OnWindowClosed(WindowController closedWindow)
    {
        // Pencere kapandýðýnda, geriye kalanlar arasýnda 
        // Hiyerarþide en altta (yani en önde) olaný bulup onu aktif yapmalýyýz.

        WindowController nextActive = null;
        int highestIndex = -1;

        foreach (Transform child in windowParent)
        {
            // Kapanan pencereyi ve kapalý objeleri görmezden gel
            if (child == closedWindow.transform || !child.gameObject.activeSelf) continue;

            WindowController wc = child.GetComponent<WindowController>();
            if (wc != null)
            {
                // Hiyerarþi sýrasýna (SiblingIndex) bak. 
                // Ýndex ne kadar büyükse o kadar öndedir.
                if (child.GetSiblingIndex() > highestIndex)
                {
                    highestIndex = child.GetSiblingIndex();
                    nextActive = wc;
                }
            }
        }

        // Eðer arkada açýk baþka bir pencere bulduysak, onu parlat
        if (nextActive != null)
        {
            FocusWindow(nextActive);
        }
    }

    // Dýþarýdan pencere açmak istersen bunu kullan
    public void OpenWindow(GameObject windowObj)
    {
        WindowController wc = windowObj.GetComponent<WindowController>();
        if (wc != null)
        {
            wc.TurnOnOff(true);
            FocusWindow(wc);
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
        // 1. Önce hafýzaya at: "Kardeþim biz þu an 3 numaralý burgerdeyiz"
        currentBurgerIndex = value;

        // 2. Sonra sayfayý yenile
        RefreshPage();
    }

    // --- OTOMATÝK ÇALIÞAN FONKSÝYON ---
    // Hem SetBurgerPage çaðýrýr, hem de Dil Deðiþimi çaðýrýr
    private void RefreshPage()
    {
        // Güvenlik kontrolü (Index dizi dýþýna taþmasýn)
        if (currentBurgerIndex < 0 || currentBurgerIndex >= burgerSprites.Length) return;

        // --- GÖRSEL ---
        // Görsel dil deðiþiminden etkilenmez ama yine de burada durabilir
        burgerImage.sprite = burgerSprites[currentBurgerIndex];
        burgerImageWorld.sprite = burgerImage.sprite;

        // --- METÝNLER (LocalizationManager'dan çekilecek) ---
        if (LocalizationManager.Instance != null)
        {
            // Malzemeler
            string ingText = LocalizationManager.Instance.GetText(ingredientKeys[currentBurgerIndex]);
            ingredientsTMP.text = ingText;
            ingredientsTMPWorld.text = ingText;

            // Açýklama
            string descText = LocalizationManager.Instance.GetText(descriptionKeys[currentBurgerIndex]);
            descriptionTMP.text = descText;
            descriptionTMPWorld.text = descText;

            // Baþlýk
            string headText = LocalizationManager.Instance.GetText(headerKeys[currentBurgerIndex]);
            headerTMP.text = headText;
            headerTMPWorld.text = headText;
        }
    }

    public void HandleMusicPlayerPage(bool open)
    {
        if (open)
        {
            if (musicPlayerPageState == PageState.Closed)
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

    public void SetMusicPlayerPageState(int num) //0 closed, 1 opened, 2 minimized
    {
        musicPlayerPageState = (PageState)num;
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

            playPauseImage.sprite = playPauseSprites[1];
            playPauseImageWorld.sprite = playPauseSprites[1];
        }
    }

    public void SetMusicVolume(float value)
    {
        musicSource.volume = value * musicVolumeMultiplier;
    }

    public void UpadateShowHint()
    {
        MonitorSC.UpdateShowHint();
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