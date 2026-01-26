using Febucci.UI;
using TMPro;
using UnityEngine;

public class DialogueAnimator : MonoBehaviour
{
    [SerializeField] private TypewriterByCharacter typewriter;
    [SerializeField] private TMP_Text textComponent;

    public bool IsBusy { get; private set; } = false;

    private void Awake()
    {
        // Eðer inspector'dan atanmadýysa otomatik bul
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        // Text Animator genelde TMP ile ayný objededir ama deðilse childlarda ara
        if (textComponent == null)
            textComponent = GetComponentInChildren<TMP_Text>();
        // Eventleri dinle
        typewriter.onTextShowed.AddListener(OnTypingFinished);
        typewriter.onTextDisappeared.AddListener(OnDisappearFinished);
    }

    public void SetColor(Color color)
    {
        if (textComponent != null)
        {
            textComponent.color = color;
        }
    }

    public void Show(string text)
    {
        IsBusy = true;
        gameObject.SetActive(true);

        // Eðer önceki text hala disappear oluyorsa, onu anýnda kesip yenisini yazabiliriz
        // veya Febucci'nin ShowText'i zaten resetler.
        typewriter.ShowText(text);
        typewriter.StartShowingText();
    }

    public void Hide()
    {
        // Direkt kapanmasýn, disappear efektini baþlatsýn
        if (gameObject.activeInHierarchy)
            typewriter.StartDisappearingText();
        else
            IsBusy = false;
    }

    public void ForceHide()
    {
        // Acil durum kapatmasý (Reset için)
        typewriter.StopShowingText();
        gameObject.SetActive(false);
        IsBusy = false;
    }

    public void SkipTypewriter()
    {
        typewriter.SkipTypewriter();
    }

    // --- CALLBACKS ---
    private void OnTypingFinished()
    {
        // Yazma bitti ama hala ekranda duruyor, o yüzden hala Busy sayýlýr.
        // Busy false yapmak için Disappear olmasýný bekleyeceðiz.
    }

    private void OnDisappearFinished()
    {
        // Artýk tamamen yok oldu, yeni görev alabilir.
        IsBusy = false;
        gameObject.SetActive(false);
    }

    public bool IsTyping()
    {
        return typewriter.isShowingText; // Febucci'nin kendi bool'u
    }
}