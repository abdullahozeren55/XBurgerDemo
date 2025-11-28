using UnityEngine;
using UnityEngine.Audio;

public class AudioReverbTrigger : MonoBehaviour
{
    [Header("Mixer Ayarlarý")]
    public float transitionTime = 1.0f;

    [Header("Yön Ayarlarý")]
    [Tooltip("Yeþil Tarafa (Forward) geçince devreye girecek Snapshot")]
    public string greenZoneSnapshot;

    [Tooltip("Kýrmýzý Tarafa (Back) geçince devreye girecek Snapshot")]
    public string redZoneSnapshot;

    private void OnTriggerExit(Collider other)
    {
        // Sadece Player triggerdan TAMAMEN ÇIKTIÐINDA çalýþýr.
        // Böylece oyuncu kapýda beklerse ses gidip gelmez.
        if (other.CompareTag("Player"))
        {
            // Oyuncunun trigger'a göre yerel pozisyonunu al
            // (Unity'nin matematik kütüphanesi yönü otomatik hesaplar)
            Vector3 localPos = transform.InverseTransformPoint(other.transform.position);

            if (localPos.z > 0)
            {
                SoundManager.Instance.SwitchSnapshot(greenZoneSnapshot, transitionTime);
            }
            else
            {
                SoundManager.Instance.SwitchSnapshot(redZoneSnapshot, transitionTime);
            }
        }
    }

    // --- GÖRSELLEÞTÝRME (GÝZMOS) ---
    private void OnDrawGizmos()
    {
        // Objenin kendi rotasyonunu ve scale'ini hesaba katarak çizim yap
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;

        // Collider boyutunu al (Varsayýlan 1x1x1 küp üzerinden)
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            // Trigger'ýn kendisini þeffaf sarý çiz (Eþik)
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(box.center, box.size);

            // --- YEÞÝL BÖLGE (Gidilecek Yer) ---
            // Z ekseninde ileriye (Forward) ufak bir küre koy
            Gizmos.color = new Color(0, 1, 0, 0.8f); // Yeþil
            Vector3 greenPos = box.center + new Vector3(0, 0, 0.5f);
            Gizmos.DrawSphere(greenPos, 0.2f);

            // --- KIRMIZI BÖLGE (Gelinen Yer) ---
            // Z ekseninde geriye (Back) ufak bir küre koy
            Gizmos.color = new Color(1, 0, 0, 0.8f); // Kýrmýzý
            Vector3 redPos = box.center - new Vector3(0, 0, 0.5f);
            Gizmos.DrawSphere(redPos, 0.2f);

            // Ok iþareti niyetine çizgi
            Gizmos.color = Color.white;
            Gizmos.DrawLine(redPos, greenPos);
        }
    }
}