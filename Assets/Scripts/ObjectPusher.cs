using UnityEngine;

public class ObjectPusher : MonoBehaviour
{
    [Header("Güç Ayarlarý")]
    [Tooltip("Uygulanacak minimum itme kuvveti.")]
    [SerializeField] private float minPushPower = 5.0f;

    [Tooltip("Uygulanacak maksimum itme kuvveti.")]
    [SerializeField] private float maxPushPower = 10.0f;

    [Header("Yön Ayarlarý")]
    [Tooltip("Nesnenin ne kadar yukarý fýrlayacaðý (0 ile 1 arasý önerilir).")]
    [SerializeField] private float upwardBias = 0.4f;

    [Tooltip("Nesnenin saða/sola ne kadar saçýlacaðý.")]
    [SerializeField] private float sideRandomness = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip balloonKickSound;
    [SerializeField] private float balloonKickVolume = 1f;
    [SerializeField] private float balloonKickMinPitch = 0.8f;
    [SerializeField] private float balloonKickMaxPitch = 1.2f;
    [Space]
    [SerializeField] private AudioClip ballKickSound;
    [SerializeField] private float ballKickVolume = 1f;
    [SerializeField] private float ballKickMinPitch = 0.8f;
    [SerializeField] private float ballKickMaxPitch = 1.2f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Balloon"))
        {
            // 1. Çarptýðýmýz objenin Rigidbody'sini alalým
            Rigidbody body = other.attachedRigidbody;

            // 2. Güvenlik Kontrolleri
            // Rigidbody yoksa veya Kinematic ise (hareket edemezse) iþlemi iptal et.
            if (body == null || body.isKinematic)
            {
                return;
            }
            // 4. Yön Vektörünü Hesaplama

            // Temel yön: Karakterin baktýðý yön (Ýleri)
            Vector3 pushDir = transform.forward;

            // Yukarý doðru hafif bir eðim ekle (Hafif havaya kalkmasý için)
            pushDir.y = upwardBias;

            // Saða veya sola rastgele bir sapma ekle (-0.5 ile +0.5 arasý gibi)
            float randomSide = Random.Range(-sideRandomness, sideRandomness);
            pushDir += transform.right * randomSide;

            // Vektörü normalize et (Yönü koru, büyüklüðü 1 yap)
            pushDir.Normalize();

            // 5. Kuvvet Büyüklüðünü Belirleme (Random Aralýk)
            float currentPower = Random.Range(minPushPower, maxPushPower);

            // 6. Kuvveti Uygulama
            // ForceMode.Impulse: Kütlesi olan objelere ani vuruþ hissi vermek için en uygun moddur.
            // AtWorldPosition kullanarak tam çarpýþma noktasýndan itmiyoruz, 
            // direkt merkeze kuvvet uyguluyoruz ki obje çok fazla kendi ekseninde dönmesin (spin atmasýn).
            body.AddForce(pushDir * currentPower, ForceMode.Impulse);

            SoundManager.Instance.PlaySoundFX(balloonKickSound, other.transform, balloonKickVolume, balloonKickMinPitch, balloonKickMaxPitch, false);
        }
        else if (other.gameObject.CompareTag("Ball"))
        {
            // 1. Çarptýðýmýz objenin Rigidbody'sini alalým
            Rigidbody body = other.attachedRigidbody;

            // 2. Güvenlik Kontrolleri
            // Rigidbody yoksa veya Kinematic ise (hareket edemezse) iþlemi iptal et.
            if (body == null || body.isKinematic)
            {
                return;
            }
            // 4. Yön Vektörünü Hesaplama

            // Temel yön: Karakterin baktýðý yön (Ýleri)
            Vector3 pushDir = transform.forward;

            // Yukarý doðru hafif bir eðim ekle (Hafif havaya kalkmasý için)
            pushDir.y = upwardBias;

            // Saða veya sola rastgele bir sapma ekle (-0.5 ile +0.5 arasý gibi)
            float randomSide = Random.Range(-sideRandomness, sideRandomness);
            pushDir += transform.right * randomSide;

            // Vektörü normalize et (Yönü koru, büyüklüðü 1 yap)
            pushDir.Normalize();

            // 5. Kuvvet Büyüklüðünü Belirleme (Random Aralýk)
            float currentPower = Random.Range(minPushPower, maxPushPower);

            // 6. Kuvveti Uygulama
            // ForceMode.Impulse: Kütlesi olan objelere ani vuruþ hissi vermek için en uygun moddur.
            // AtWorldPosition kullanarak tam çarpýþma noktasýndan itmiyoruz, 
            // direkt merkeze kuvvet uyguluyoruz ki obje çok fazla kendi ekseninde dönmesin (spin atmasýn).
            body.AddForce(pushDir * currentPower, ForceMode.Impulse);

            SoundManager.Instance.PlaySoundFX(ballKickSound, other.transform, ballKickVolume, ballKickMinPitch, ballKickMaxPitch, false);
        }

    }
}