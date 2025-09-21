using UnityEngine;

public class MirrorManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera mirrorCam;
    [SerializeField] private float fovAmplitude = 5f;
    [SerializeField] private float pitchAmplitude = 5f;
    [SerializeField] private float smooth = 5f;

    private float baseFov;

    private void Start()
    {
        baseFov = mirrorCam.fieldOfView;
    }

    private void LateUpdate()
    {
        // --- YATAY AÇI (sadece pozisyona göre)
        Vector3 posY = new Vector3(player.position.x, transform.position.y, player.position.z);
        Vector3 toPlayer = posY - transform.position; // aynadan oyuncuya doðru vektör
        Vector3 mirrorForward = transform.forward;

        float angleY = Vector3.SignedAngle(mirrorForward, toPlayer, Vector3.up);

        // --- DÝKEY AÇI (zýplama / yükseklik farký)
        float verticalOffset = player.position.y - transform.position.y;
        float targetPitch = Mathf.Clamp(verticalOffset * pitchAmplitude, -pitchAmplitude, pitchAmplitude);

        // --- Rotasyonu uygula
        Quaternion targetRot = Quaternion.Euler(targetPitch, angleY, 0);
        mirrorCam.transform.localRotation = Quaternion.Slerp(mirrorCam.transform.localRotation, targetRot, Time.deltaTime * smooth);

        // --- FOV (aynaya yaklaþ / uzaklaþ)
        float dist = Vector3.Distance(player.position, transform.position);
        float targetFov = baseFov + (fovAmplitude * (1f / dist));
        mirrorCam.fieldOfView = Mathf.Lerp(mirrorCam.fieldOfView, targetFov, Time.deltaTime * smooth);
    }
}
