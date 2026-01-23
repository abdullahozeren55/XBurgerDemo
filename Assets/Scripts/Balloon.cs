using UnityEngine;
using System.Collections.Generic;

public class Balloon : MonoBehaviour
{
    [Header("Settings")]
    public bool IsMainBalloon = false;       // Ana balon mu?
    public BalloonData data;

    [Header("References")]
    public List<Rigidbody> childBalloons;    // Inspector'dan atacaðýn alt balonlar

    private Color balloonColor; // Rengi hafýzaya atacaðýmýz deðiþken

    private void Awake()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Materyalin o anki rengini alýp saklýyoruz.
            // NOT: Eðer modelin bir alt objede (child) ise GetComponentInChildren<Renderer>() kullanmalýsýn.
            balloonColor = rend.material.color;
        }
        else
        {
            // Eðer renderer bulamazsa varsayýlan beyaz olsun ki hata vermesin
            balloonColor = Color.white;
        }
    }

    // Býçak scriptin burayý tetikleyecek
    public void PopBalloon()
    {
        // 1. Ses ve Efekt
        if (data.destroyParticles != null)
        {
            // Particle'ý oluþtur ve referansýný 'ps' deðiþkenine al
            ParticleSystem ps = Instantiate(data.destroyParticles, transform.position, Quaternion.identity);

            // 2. ADIM: Rengi Particle'a Basma
            // Particle System'in 'Main' modülüne eriþmemiz lazým
            var mainSettings = ps.main;

            // Particle rengini, balonun hafýzadaki rengine eþitliyoruz
            mainSettings.startColor = balloonColor;
        }

        SoundManager.Instance.PlaySoundFX(data.popSound, transform, data.popVolume, data.popMinPitch, data.popMaxPitch, false);

        // 2. Ana Balon Mantýðý
        if (IsMainBalloon && childBalloons.Count > 0)
        {
            ReleaseChildren();
            // TODO: BalloonManager entegrasyonu buraya gelecek.
        }

        // 3. Kendini yok et
        Destroy(gameObject);
    }

    private void ReleaseChildren()
    {
        foreach (Rigidbody childRB in childBalloons)
        {
            if (childRB != null)
            {
                childRB.transform.SetParent(null);
                childRB.isKinematic = false;

                // 1. Ýtme Kuvveti (Mevcut kodun)
                Vector3 randomDir = Random.insideUnitSphere;
                childRB.AddForce(randomDir * data.releaseExplosionForce, ForceMode.Impulse);

                // 2. YENÝ: Rastgele Tork (Döndürme) Kuvveti
                // Random.insideUnitSphere kullanarak her eksende rastgele bir dönüþ saðlarýz.
                Vector3 randomTorque = Random.insideUnitSphere * data.releaseTorqueForce;

                // ForceMode.Impulse, anlýk bir darbe gibi çalýþýr, patlama için idealdir.
                childRB.AddTorque(randomTorque, ForceMode.Impulse);
            }
        }
    }
}