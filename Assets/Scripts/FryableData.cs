using UnityEngine;

[CreateAssetMenu(fileName = "NewFryableData", menuName = "Data/Fryable")]
public class FryableData : ScriptableObject
{
    [System.Serializable]
    public class MeshConfig
    {
        public Mesh mesh; // Görsel þekil
        public Vector3 posOffset; // Blender hatasýný düzeltme payý
        public Vector3 rotOffset; // Yan duruyorsa düzeltme payý
        public float height;

        // --- YENÝ: Box Collider Ayarlarý ---
        public Vector3 colliderCenter;
        public Vector3 colliderSize = Vector3.one; // Varsayýlan 1 olsun ki kaybolmasýn
    }

    [Header("Visual Configurations")]
    [Tooltip("0: Sepet Dibi, 1: Orta, 2: Tepe")]
    public MeshConfig handMesh; // Elde tutulan toplu hali
    public MeshConfig[] basketMeshes; // Sepetteki yayýk halleri (3 tane eklersin)

    [Header("Identity")]
    public float putOnBasketDuration = 0.2f; // Varsayýlan 0.2 saniye olsun
    public Holder.HolderIngredient type;
    public ItemIcon iconData;
    public string[] focusTextKeys;
    public PlayerManager.HandGrabTypes handGrabType;
    public PlayerManager.HandRigTypes handRigType;
    [Space]
    public bool isUseable = false;
    public bool isThrowable = true;
    public float throwMultiplier = 1f;
    [Space]
    public Vector3 localScaleWhenGrabbed;
    public Vector3 localScaleWhenUnpacked = new Vector3(100f, 100f, 100f);
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]

    [Header("Cooking Settings")]
    public float timeToCook = 10f; // Kaç saniyede piþer?
    public float timeToBurn = 20f; // Kaç saniyede yanar?
    public float cookingVariance = 0.15f;

    [Header("Visuals (Materials)")]
    // Mesh swap yerine Material swap daha ucuzdur ve PSX için yeterlidir.
    // Ama model deðiþsin istersen buraya Mesh referanslarý da koyabiliriz.
    public Material rawMat;
    public Material cookedMat;
    public Material burntMat;
    [Space]
    public ParticleSystem[] dropParticles;
    public ParticleSystem[] throwParticles;
    [Space]

    [Header("Audio")]
    public AudioClip placeSound; // Sepete yerleþme sesi (Juice/Thud)
    [Space]
    public AudioClip[] audioClips;
    [Space]
    public float grabSoundVolume = 1f;
    public float grabSoundMinPitch = 0.85f;
    public float grabSoundMaxPitch = 1.15f;
    [Space]
    public float dropSoundVolume = 1f;
    public float dropSoundMinPitch = 0.85f;
    public float dropSoundMaxPitch = 1.15f;
    [Space]
    public float throwSoundVolume = 1f;
    public float throwSoundMinPitch = 0.85f;
    public float throwSoundMaxPitch = 1.15f;
    [Space]
    public float traySoundVolume = 1f;
    public float traySoundMinPitch = 0.85f;
    public float traySoundMaxPitch = 1.15f;
    [Space]
    public float soundCooldown = 0.1f;
    public float throwThreshold = 5f;
    public float dropThreshold = 1f;
    [Space]
    public float placeSoundVolume = 1f;
    public float placeSoundMinPitch = 0.85f;
    public float placeSoundMaxPitch = 1.15f;
    [Space]
    public float cookedSoundMultiplier = 0.8f;
    public float burntSoundMultiplier = 0.6f;
    
}