using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MonitorManager : MonoBehaviour
{
    public static MonitorManager Instance;

    public bool IsFocused;

    public Monitor MonitorSC;

    public Transform windowParent;

    [System.Serializable]
    public struct SongData
    {
        public string trackName; // Örn: "Track 01 - Spooky Burger"
        public AudioClip clip;   // Müzik dosyasý
    }

    public enum PageState
    {
        Closed,
        Opened,
        Minimized
    }

    [Header("Burger Page Settings")]
    // YENÝ: Hangi font tipini kullanacaðýz?
    public FontType burgerFontType = FontType.RetroUINoOutline;

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

    // --- HAFIZA DEÐÝÞKENLERÝ (Font Boyutlarý için) ---
    private float _initHeaderSize;
    private float _initIngSize;
    private float _initDescSize;

    // Karakter boþluklarýný da korumak istersen (Opsiyonel ama önerilir):
    private float _initHeaderSpacing;
    private float _initIngSpacing;
    private float _initDescSpacing;

    [Header("Order List Settings (YENÝ)")]
    public FontType orderFontType = FontType.RetroUINoOutline; // Sipariþ listesi için font tipi
    public TMP_Text currentOrdersTMP;      // UI'daki text
    public TMP_Text currentOrdersTMPWorld; // World Space'deki text (Varsa)

    private List<OrderData> _activeOrderList = new List<OrderData>();

    // --- HAFIZA DEÐÝÞKENLERÝ (Order List) ---
    private float _initOrderSize;
    private float _initOrderSpacing;
    private float _initOrderLineSpacing;

    [Header("Notepad Page Settings")]
    public InputMirror notePadInputMirror;

    [Header("Music Player Settings")]
    public PageState musicPlayerPageState;
    public AudioSource musicSource;
    public List<SongData> playlist;
    public bool MusicIsPlaying = false;
    public float musicVolumeMultiplier = 0.2f; //to prevent it from being too loud
    private int currentIndex = 0;
    [Space]
    public RetroMarquee universalMarquee;
    public float marqueeStepInterval = 0.4f; // Týrtýklý kayma hýzý burada
    [Space]
    public GameObject musicPlayerWindow; //Oyun baþý açmak için
    public Image playPauseImage;
    public Image playPauseImageWorld;
    public Sprite[] playPauseSprites; //0 play, 1 pause

    [Header("Video Player Settings")]
    public VideoPlayer monitorVideoPlayer; // Inspector'dan VideoPlayer'ý buraya sürükle
    public VideoClip[] videoClips;         // Videolarý buraya ekleyeceksin



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

        // --- BAÞLANGIÇ DEÐERLERÝNÝ KAYDET ---
        // Oyun baþýndaki (muhtemelen Ýngilizce/Latin) boyutlarý referans alýyoruz.
        if (headerTMP != null)
        {
            _initHeaderSize = headerTMP.fontSize;
            _initHeaderSpacing = headerTMP.characterSpacing;
        }
        if (ingredientsTMP != null)
        {
            _initIngSize = ingredientsTMP.fontSize;
            _initIngSpacing = ingredientsTMP.characterSpacing;
        }
        if (descriptionTMP != null)
        {
            _initDescSize = descriptionTMP.fontSize;
            _initDescSpacing = descriptionTMP.characterSpacing;
        }

        // --- ORDER LIST BAÞLANGIÇ DEÐERLERÝ (YENÝ) ---
        if (currentOrdersTMP != null)
        {
            _initOrderSize = currentOrdersTMP.fontSize;
            _initOrderSpacing = currentOrdersTMP.characterSpacing;
            _initOrderLineSpacing = currentOrdersTMP.lineSpacing;
        }
    }

    private void Start()
    {
        // 1. Oynatma listesinde þarký varsa baþlat
        if (playlist.Count > 0)
        {
             // ... Diðer kodlarýn ...
             OpenWindow(musicPlayerWindow);
             HandleMusicPlayerPage(true);
             SetMusicPlayerPageState(1);
             LoadTrack(0, true);
        }

        StartCoroutine(MarqueeDriverLoop());

        // Baþlangýçta fontlarý bir kere ayarla
        RefreshPage();
    }

    private void Update()
    {
        if (IsFocused)
        {
            if (InputManager.Instance.PlayerThrow())
            {

                notePadInputMirror.SyncInputs();
                MonitorSC.FinishMonitorUI();
                IsFocused = false;
            }
        }
    }

    // Butonlardan çaðýracaðýn fonksiyon (Örn: OnClick -> PlayMonitorVideo(0))
    public void PlayMonitorVideo(int index)
    {
        // 1. Güvenlik Kontrolü: Ýndex var mý?
        if (index < 0 || index >= videoClips.Length)
        {
            Debug.LogWarning($"MonitorManager: {index} numaralý video bulunamadý!");
            return;
        }

        // 2. Video Player referansý var mý?
        if (monitorVideoPlayer == null)
        {
            monitorVideoPlayer = GetComponent<VideoPlayer>();
        }

        // 3. Videoyu Ata ve Oynat
        monitorVideoPlayer.clip = videoClips[index];
        monitorVideoPlayer.Play();
    }

    // Videoyu durdurup temizleyen fonksiyon
    public void StopMonitorVideo()
    {
        if (monitorVideoPlayer == null) return;

        // Oynuyorsa durdur
        if (monitorVideoPlayer.isPlaying)
        {
            monitorVideoPlayer.Stop();
        }

        // Clip'i boþa çýkar (Temizle)
        monitorVideoPlayer.clip = null;
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

    // --- SÝPARÝÞÝ LÝSTE OLARAK AL ---
    public void SetCurrentOrder(List<OrderData> orders)
    {
        _activeOrderList = orders; // Listeyi kaydet
        RefreshOrderListDisplay(); // Ekraný güncelle
    }

    public void ClearCurrentOrder()
    {
        _activeOrderList.Clear(); // Listeyi temizle
        if (currentOrdersTMP != null) currentOrdersTMP.text = "";
        if (currentOrdersTMPWorld != null) currentOrdersTMPWorld.text = "";
    }

    // --- OTOMATÝK ÇALIÞAN FONKSÝYON ---
    // Hem SetBurgerPage çaðýrýr, hem de Dil Deðiþimi çaðýrýr
    private void RefreshPage()
    {
        // 1. Burger Sayfasýný Yenile
        RefreshBurgerPageDisplay();

        // 2. Sipariþ Listesini Yenile (Eðer aktif bir sipariþ varsa)
        RefreshOrderListDisplay();
    }

    private void RefreshBurgerPageDisplay()
    {
        if (currentBurgerIndex < 0 || currentBurgerIndex >= burgerSprites.Length) return;

        burgerImage.sprite = burgerSprites[currentBurgerIndex];
        burgerImageWorld.sprite = burgerImage.sprite;

        if (LocalizationManager.Instance != null)
        {
            var targetData = LocalizationManager.Instance.GetFontDataForCurrentLanguage(burgerFontType);
            var defaultData = LocalizationManager.Instance.GetDefaultFontData(burgerFontType);

            float defaultBase = Mathf.Max(defaultData.basePixelSize, 0.1f);
            float ratio = targetData.basePixelSize / defaultBase;
            float charSpacingDiff = targetData.characterSpacingOffset - defaultData.characterSpacingOffset;

            string headText = LocalizationManager.Instance.GetText(headerKeys[currentBurgerIndex]);
            UpdateTextComp(headerTMP, headText, targetData.font, _initHeaderSize * ratio, _initHeaderSpacing + charSpacingDiff);
            UpdateTextComp(headerTMPWorld, headText, targetData.font, _initHeaderSize * ratio, _initHeaderSpacing + charSpacingDiff);

            string ingText = LocalizationManager.Instance.GetText(ingredientKeys[currentBurgerIndex]);
            UpdateTextComp(ingredientsTMP, ingText, targetData.font, _initIngSize * ratio, _initIngSpacing + charSpacingDiff);
            UpdateTextComp(ingredientsTMPWorld, ingText, targetData.font, _initIngSize * ratio, _initIngSpacing + charSpacingDiff);

            string descText = LocalizationManager.Instance.GetText(descriptionKeys[currentBurgerIndex]);
            UpdateTextComp(descriptionTMP, descText, targetData.font, _initDescSize * ratio, _initDescSpacing + charSpacingDiff);
            UpdateTextComp(descriptionTMPWorld, descText, targetData.font, _initDescSize * ratio, _initDescSpacing + charSpacingDiff);
        }
    }

    // --- YENÝ: SÝPARÝÞ LÝSTESÝNÝ GÜNCELLEME MANTIÐI ---
    private void RefreshOrderListDisplay()
    {
        if (_activeOrderList == null || _activeOrderList.Count == 0) return;
        if (currentOrdersTMP == null && currentOrdersTMPWorld == null) return;
        if (LocalizationManager.Instance == null) return;

        // 1. Font Verilerini Al (Ayný kalýyor)
        var targetData = LocalizationManager.Instance.GetFontDataForCurrentLanguage(orderFontType);
        var defaultData = LocalizationManager.Instance.GetDefaultFontData(orderFontType);

        float defaultBase = Mathf.Max(defaultData.basePixelSize, 0.1f);
        float ratio = targetData.basePixelSize / defaultBase;
        float charSpacingDiff = targetData.characterSpacingOffset - defaultData.characterSpacingOffset;
        float lineSpacingDiff = targetData.lineSpacingOffset - defaultData.lineSpacingOffset;

        // --- 2. METNÝ OLUÞTUR (DEÐÝÞÝKLÝK BURADA) ---
        StringBuilder finalSb = new StringBuilder();

        for (int i = 0; i < _activeOrderList.Count; i++)
        {
            // Tekil sipariþi string'e çevir
            string singleOrderText = BuildOrderString(_activeOrderList[i]);

            // Eðer sipariþ boþ deðilse ekle
            if (!string.IsNullOrEmpty(singleOrderText))
            {
                finalSb.Append(singleOrderText);

                // Eðer bu son sipariþ deðilse, araya BOÞLUK (Satýr atlama) koy
                if (i < _activeOrderList.Count - 1)
                {
                    finalSb.Append("\n\n"); // Ýki kere alt satýra geç = 1 satýr boþluk
                }
            }
        }

        string finalContent = finalSb.ToString();

        // 3. Ekrana Bas (Ayný kalýyor)
        ApplyOrderTextSettings(currentOrdersTMP, finalContent, targetData.font,
            _initOrderSize * ratio, _initOrderSpacing + charSpacingDiff, _initOrderLineSpacing + lineSpacingDiff);

        ApplyOrderTextSettings(currentOrdersTMPWorld, finalContent, targetData.font,
            _initOrderSize * ratio, _initOrderSpacing + charSpacingDiff, _initOrderLineSpacing + lineSpacingDiff);
    }

    private string BuildOrderString(OrderData data)
    {
        StringBuilder sb = new StringBuilder();

        // --- BURGERLER ---
        foreach (var item in data.RequiredBurgers)
        {
            // FÝLTRE: Key boþsa veya sayý 0 ise GEÇ
            if (string.IsNullOrEmpty(item.OrderKey) || item.Count <= 0) continue;

            string name = LocalizationManager.Instance.GetText(item.OrderKey);
            sb.AppendLine($"{name} x{item.Count}");
        }

        // --- ÝÇECEKLER ---
        foreach (var item in data.RequiredDrinks)
        {
            if (string.IsNullOrEmpty(item.OrderKey) || item.Count <= 0) continue; // <--- FÝLTRE

            string name = LocalizationManager.Instance.GetText(item.OrderKey);
            sb.AppendLine($"{name} x{item.Count}");
        }

        // --- YAN ÜRÜNLER ---
        foreach (var item in data.RequiredSides)
        {
            if (string.IsNullOrEmpty(item.OrderKey) || item.Count <= 0) continue; // <--- FÝLTRE

            string name = LocalizationManager.Instance.GetText(item.OrderKey);
            sb.AppendLine($"{name} x{item.Count}");
        }

        // --- SOSLAR ---
        foreach (var item in data.RequiredSauces)
        {
            if (string.IsNullOrEmpty(item.OrderKey) || item.Count <= 0) continue; // <--- FÝLTRE

            string name = LocalizationManager.Instance.GetText(item.OrderKey);
            sb.AppendLine($"{name} x{item.Count}");
        }

        // --- OYUNCAKLAR ---
        foreach (var item in data.RequiredToys)
        {
            if (string.IsNullOrEmpty(item.OrderKey) || item.Count <= 0) continue; // <--- FÝLTRE

            string name = LocalizationManager.Instance.GetText(item.OrderKey);
            sb.AppendLine($"{name} x{item.Count}");
        }

        return sb.ToString().TrimEnd();
    }

    private void ApplyOrderTextSettings(TMP_Text textComp, string content, TMP_FontAsset font, float size, float spacing, float lineSpacing)
    {
        if (textComp == null) return;

        textComp.text = content;
        if (font != null) textComp.font = font;
        textComp.fontSize = size;
        textComp.characterSpacing = spacing;
        textComp.lineSpacing = lineSpacing;
    }

    // Kod tekrarýný önlemek için yardýmcý metod
    private void UpdateTextComp(TMP_Text textComp, string content, TMP_FontAsset font, float size, float spacing)
    {
        if (textComp == null) return;

        textComp.text = content;

        if (font != null) textComp.font = font;

        textComp.fontSize = size;
        textComp.characterSpacing = spacing;
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

        // 2. ÝSÝM FORMATLAMA (Localization Ýptal)
        // Format: "01. Þarký Adý"
        // {index + 1:00} -> Sayýyý iki haneli yapar (1 -> 01, 10 -> 10)

        // Not: song.trackNameKey deðiþkeninde artýk direkt þarkýnýn adý (Örn: "Midnight Grill") yazmalý.
        string displayName = $"{index + 1:00}. {song.trackName}";

        // 3. Kayan yazýyý güncelle
        if (universalMarquee != null)
        {
            universalMarquee.RefreshText(displayName);
        }

        // 4. Çalma ve Ýkon iþlemleri
        if (autoPlay || musicSource.isPlaying)
        {
            musicSource.Play();
            MusicIsPlaying = true;

            if (playPauseImage != null && playPauseSprites.Length > 1)
                playPauseImage.sprite = playPauseSprites[1];

            if (playPauseImageWorld != null && playPauseSprites.Length > 1)
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
            yield return wait;

            // Tek bir komutla ikisini de ilerlet
            // Focus UI kapalý olsa bile arka planda koordinatý güncellenecek
            if (universalMarquee != null)
            {
                universalMarquee.Step();
            }
        }
    }
}