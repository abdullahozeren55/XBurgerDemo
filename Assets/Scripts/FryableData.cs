using UnityEngine;

[CreateAssetMenu(fileName = "NewFryableData", menuName = "Data/Fryable")]
public class FryableData : ScriptableObject
{
    public enum FryableType
    {
        Fries,      // Patates
        OnionRing,  // Soðan Halkasý
        Nugget,     // Tavuk Parçasý
        Mozzarella  // Peynir Çubuðu vs.
    }

    [System.Serializable]
    public class MeshConfig
    {
        public Mesh mesh; // Görsel þekil
        public Vector3 posOffset; // Blender hatasýný düzeltme payý
        public Vector3 rotOffset; // Yan duruyorsa düzeltme payý
        public float height;
    }

    [Header("Visual Configurations")]
    [Tooltip("0: Sepet Dibi, 1: Orta, 2: Tepe")]
    public MeshConfig handMesh; // Elde tutulan toplu hali
    public MeshConfig[] basketMeshes; // Sepetteki yayýk halleri (3 tane eklersin)

    [Header("Identity")]
    public FryableType type;
    public Sprite icon;
    public string[] focusTextKeys;
    public PlayerManager.HandGrabTypes handGrabType;

    [Header("Stacking Physics")]
    [Tooltip("Bu objenin dikeyde kapladýðý alan. Burger mantýðýndaki 'Height'.")]
    public float stackHeight = 0.1f;
    [Space]
    public bool isUseable = false;
    public bool isThrowable = true;
    public float throwMultiplier = 1f;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]

    [Header("Cooking Settings")]
    public float timeToCook = 10f; // Kaç saniyede piþer?
    public float timeToBurn = 20f; // Kaç saniyede yanar?

    [Header("Visuals (Materials)")]
    // Mesh swap yerine Material swap daha ucuzdur ve PSX için yeterlidir.
    // Ama model deðiþsin istersen buraya Mesh referanslarý da koyabiliriz.
    public Material rawMat;
    public Material cookedMat;
    public Material burntMat;
}