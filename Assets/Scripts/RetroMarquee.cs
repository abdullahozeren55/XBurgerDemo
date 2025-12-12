using UnityEngine;
using TMPro;
using System.Collections;

public class RetroMarquee : MonoBehaviour
{
    [Header("Hýz Ayarlarý")]
    // stepInterval'ý artýk Manager yönetecek, buradaki sadece bilgi amaçlý durabilir
    public float pixelsPerStep = 16f;
    public float gap = 64f;

    private RectTransform rect1;
    private RectTransform rect2;
    private float textWidth;
    private float parentWidth;

    // Manager'ýn "Hazýr mýsýn?" diye sormasý için
    public bool IsReady { get; private set; } = false;

    private void Awake()
    {
        rect1 = GetComponent<RectTransform>();
    }

    // Bu artýk Coroutine deðil, tek seferlik kurulum fonksiyonu
    IEnumerator SetupMarqueeRoutine()
    {
        IsReady = false; // Kurulum bitene kadar bekle

        // Unity UI'ý oturtana kadar bekle (Bu obje aktifken çalýþmak zorunda)
        yield return new WaitForEndOfFrame();

        textWidth = rect1.rect.width;

        // Parent Geniþliði
        if (transform.parent != null)
        {
            RectTransform pRect = transform.parent.GetComponent<RectTransform>();
            if (pRect != null) parentWidth = pRect.rect.width;
        }
        else
        {
            parentWidth = 500f;
        }

        // Varsa eski klonu temizle (Refresh durumunda)
        string cloneName = gameObject.name + "_Clone";
        Transform oldClone = transform.parent.Find(cloneName);
        if (oldClone != null) Destroy(oldClone.gameObject);

        // Kopyayý oluþtur
        GameObject cloneObj = Instantiate(gameObject, transform.parent);
        cloneObj.name = cloneName;

        // Kopyadaki scripti yok et
        Destroy(cloneObj.GetComponent<RetroMarquee>());

        rect2 = cloneObj.GetComponent<RectTransform>();

        // --- KONUMLARI GÜNCELLE ---
        // Rect1: Panelin EN SAÐINDA
        rect1.anchoredPosition = new Vector2(parentWidth, rect1.anchoredPosition.y);

        // Rect2: Rect1'in arkasýnda
        rect2.anchoredPosition = new Vector2(parentWidth + textWidth + gap, rect1.anchoredPosition.y);

        IsReady = true; // Artýk Manager beni hareket ettirebilir
    }

    // --- MANAGER BURAYI ÇAÐIRACAK ---
    public void Step()
    {
        if (!IsReady || rect1 == null || rect2 == null) return;

        MoveRect(rect1);
        MoveRect(rect2);

        Debug.Log("ben gidiyon");
    }

    void MoveRect(RectTransform rect)
    {
        Vector2 pos = rect.anchoredPosition;
        pos.x -= pixelsPerStep;

        if (pos.x < -textWidth)
        {
            RectTransform otherRect = (rect == rect1) ? rect2 : rect1;
            pos.x = otherRect.anchoredPosition.x + textWidth + gap;
        }

        rect.anchoredPosition = pos;
    }

    public void RefreshText(string newText)
    {
        GetComponent<TextMeshProUGUI>().text = newText;
        StopAllCoroutines();

        // Eðer obje o an kapalýysa Coroutine çalýþmaz, 
        // o yüzden açýlýnca çalýþsýn diye OnEnable kullanabilirsin 
        // ama þimdilik aktif olduðunu varsayýyoruz.
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(SetupMarqueeRoutine());
        }
    }
}