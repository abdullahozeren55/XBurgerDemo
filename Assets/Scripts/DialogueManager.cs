using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Febucci.UI; // Text Animator namespace

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Glitch Settings")]
    [SerializeField] private AudioClip glitchSFX;
    [SerializeField] private AudioSource sfxSource;

    // Sahnedeki tüm konuþmacýlarý ID'lerine göre burada tutuyoruz
    private Dictionary<string, IDialogueSpeaker> speakers = new Dictionary<string, IDialogueSpeaker>();

    private DialogueData currentData;
    private int currentIndex;
    private bool isDialogueActive;
    private bool isTyping;

    // Þu an konuþan kiþi
    private IDialogueSpeaker currentSpeaker;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        // Input kontrolü (Senin InputManager'ýnla deðiþtir)
        if (InputManager.Instance.PlayerInteract())
        {
            if (isTyping)
            {
                // Yazýyorsa tamamla (Febucci özelliði)
                currentSpeaker?.TextPlayer.SkipTypewriter();
            }
            else
            {
                // Yazmýyorsa sonrakine geç
                AdvanceDialogue();
            }
        }
    }

    // --- SPEAKER KAYIT SÝSTEMÝ ---
    public void RegisterSpeaker(IDialogueSpeaker speaker)
    {
        if (!speakers.ContainsKey(speaker.SpeakerID))
            speakers.Add(speaker.SpeakerID, speaker);
    }

    public void UnregisterSpeaker(IDialogueSpeaker speaker)
    {
        if (speakers.ContainsKey(speaker.SpeakerID))
            speakers.Remove(speaker.SpeakerID);
    }

    // --- DÝYALOG BAÞLATMA ---
    public void StartDialogue(DialogueData data)
    {
        currentData = data;
        currentIndex = -1;
        isDialogueActive = true;

        // Player hareketini kitleme vs. buraya
        // PlayerManager.Instance.SetPlayerBasicMovements(false);

        AdvanceDialogue();
    }

    public void AdvanceDialogue()
    {
        // Önceki konuþmacýyý sustur
        if (currentSpeaker != null)
        {
            currentSpeaker.OnSpeakEnd();
            currentSpeaker.TextPlayer.ShowText(""); // Temizle
        }

        currentIndex++;

        if (currentIndex >= currentData.lines.Count)
        {
            EndDialogue();
            return;
        }

        PlayLine(currentData.lines[currentIndex]);
    }

    private void PlayLine(DialogueData.DialogueLine line)
    {
        if (!speakers.ContainsKey(line.SpeakerID))
        {
            Debug.LogError($"Speaker ID '{line.SpeakerID}' sahnede bulunamadý!");
            return;
        }

        currentSpeaker = speakers[line.SpeakerID];
        currentSpeaker.OnSpeakStart();

        // Febucci eventlerini dinle (Yazma bitti mi?)
        currentSpeaker.TextPlayer.onTextShowed.RemoveAllListeners();
        currentSpeaker.TextPlayer.onTextShowed.AddListener(() => isTyping = false);
        currentSpeaker.TextPlayer.onTypewriterStart.RemoveAllListeners();
        currentSpeaker.TextPlayer.onTypewriterStart.AddListener(() => isTyping = true);

        // --- GLITCH MANTIÐI ---
        if (line.IsGlitchLine)
        {
            StartCoroutine(HandleGlitchRoutine(line));
        }
        else
        {
            // Normal Metin
            string finalList = LocalizationManager.Instance.GetText(line.LocalizationKey);
            currentSpeaker.TextPlayer.ShowText(finalList);
        }

        // Ses Efekti (Custom varsa onu çal, yoksa daktilo sesi Febucci içinden ayarlanýr)
        if (line.CustomVoiceOrSFX != null)
        {
            sfxSource.PlayOneShot(line.CustomVoiceOrSFX);
        }
    }

    // --- KORKU EFEKTLERÝ ---
    private IEnumerator HandleGlitchRoutine(DialogueData.DialogueLine line)
    {
        string realText = LocalizationManager.Instance.GetText(line.LocalizationKey);

        switch (line.glitchType)
        {
            case DialogueData.GlitchType.TextSwap:
                // 1. Önce Halüsinasyonu Göster
                string fakeText = LocalizationManager.Instance.GetText(line.GlitchAltKey);

                // Febucci'nin taglarýný kullanarak titrek, kýrmýzý yazdýrabiliriz
                // Örn: <wiggle><color=red>Ne yaptýðýný biliyorum...</color></wiggle>
                string styledFake = $"<wiggle a=0.5><color=#FF0000>{fakeText}</color></wiggle>";

                currentSpeaker.TextPlayer.ShowText(styledFake);
                isTyping = true; // Oyuncu geçemesin diye kitlemek istersen burada kontrol et

                // Belirlenen süre kadar bekle (veya oyuncu okuyana kadar)
                yield return new WaitForSeconds(line.GlitchDuration);

                // 2. GLITCH EFEKTÝ (Ses + Görsel)
                if (glitchSFX) sfxSource.PlayOneShot(glitchSFX);

                // Buraya kamera shake kodu da ekleyebilirsin
                // CameraManager.Instance.Shake(...);

                // 3. Gerçek Metni Yapýþtýr (Aniden belirsin istiyorsan typewriter skip yap)
                // <appearance> tagý Febucci'de metnin nasýl belireceðini seçer.
                // Resetleyip normal metni veriyoruz.
                currentSpeaker.TextPlayer.ShowText(realText);
                break;

            case DialogueData.GlitchType.Corruption:
                // Sadece metnin belirli yerlerini bozmak.
                // Bunu Localization dosyasýnda Tag kullanarak yapmak daha mantýklý.
                // Örn: "Burger <shake>alabilir miyim?</shake>"
                // Ama kodla da inject edebiliriz:

                // Veya tüm metni korkunçlaþtýr:
                string corruptedText = $"<shake a=0.2><font=\"GlitchFont\">{realText}</font></shake>";
                currentSpeaker.TextPlayer.ShowText(corruptedText);
                break;
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        if (currentSpeaker != null)
        {
            currentSpeaker.OnSpeakEnd();
            currentSpeaker.TextPlayer.ShowText("");
        }

        // PlayerManager.Instance.SetPlayerBasicMovements(true);
        Debug.Log("Diyalog Bitti.");

        // Buradan Customer scriptine "Sipariþ verdim, hadi ben beklemeye geçiyorum" sinyali atacaðýz.
    }
}