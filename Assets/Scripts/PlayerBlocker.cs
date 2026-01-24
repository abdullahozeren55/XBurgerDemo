using System.Collections;
using System.Collections.Generic; // List kullanýmý için gerekli
using UnityEngine;

public class PlayerBlocker : MonoBehaviour
{
    [SerializeField] private DialogueData[] dialogues;

    // Ýndexleri tutacaðýmýz torbamýz
    private List<int> _dialogueIndexBag = new List<int>();

    private void Start()
    {
        // Oyun baþladýðýnda torbayý ilk kez doldur ve karýþtýr
        RefillAndShuffleBag();
    }

    /*private void OnTriggerEnter(Collider other) TODO: LAZIMSA YENÝ DÝYALOG SÝSTEMÝNE GEÇÝRÝCEZ YOKSA SÝLÝCEZ BU KODU
    {
        if (other.CompareTag("Player"))
        {
            if (!DialogueManager.Instance.IsInDialogue)
            {
                // Eðer torbada diyalog kalmadýysa, yeniden doldur ve karýþtýr
                if (_dialogueIndexBag.Count == 0)
                {
                    RefillAndShuffleBag();
                }

                // Torbanýn en üstündeki (0. index) numarayý al
                int selectedIndex = _dialogueIndexBag[0];

                // O numarayý torbadan sil (bir daha seçilmesin diye)
                _dialogueIndexBag.RemoveAt(0);

                // Diyaloðu oynat
                DialogueManager.Instance.StartSelfDialogue(dialogues[selectedIndex]);
            }
        }
    }*/

    // Torbayý doldurup karýþtýran fonksiyon
    private void RefillAndShuffleBag()
    {
        _dialogueIndexBag.Clear();

        // 1. Adým: Tüm indexleri (0, 1, 2, 3...) sýrayla listeye ekle
        for (int i = 0; i < dialogues.Length; i++)
        {
            _dialogueIndexBag.Add(i);
        }

        // 2. Adým: Fisher-Yates Shuffle algoritmasý ile listeyi karýþtýr
        for (int i = 0; i < _dialogueIndexBag.Count; i++)
        {
            int temp = _dialogueIndexBag[i];
            int randomIndex = Random.Range(i, _dialogueIndexBag.Count);

            _dialogueIndexBag[i] = _dialogueIndexBag[randomIndex];
            _dialogueIndexBag[randomIndex] = temp;
        }
    }
}