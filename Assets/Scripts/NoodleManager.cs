using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoodleManager : MonoBehaviour
{
    public static NoodleManager Instance;

    public enum NoodleStatus
    {
        Unprepared,
        Prepared,
        Ready,
        Trash
    }
    [Header("Noodle Water Settings")]
    public Kettle kettle;
    public GameObject hologramKettle;
    public Collider waterCollider;
    [Space]
    private Vector3 waterStartPos;
    public Vector3 waterEndPos;
    private Vector3 waterStartScale;
    public Vector3 waterEndScale;
    public int requiredDrops = 200;
    private int currentDrops = 0;
    [Space]
    [SerializeField] private Color saucedWaterColor;
    [SerializeField] private float timeToSauceWater = 0.3f;

    [HideInInspector] public GameObject currentNoodleGO;
    [HideInInspector] public GameObject currentSmokeGO;
    [HideInInspector] public GameObject currentWaterGO;
    [HideInInspector] public GameObject currentSaucePackGO;
    [HideInInspector] public NoodleStatus currentNoodleStatus;

    private Material currentWaterMat;
    private Collider currentNoodleCollider;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int ungrabableLayer;
    private int interactableLayer;

    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;

            // Optionally, mark GameManager as not destroyed between scene loads
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an instance already exists, destroy this one to enforce the singleton pattern
            Destroy(gameObject);
        }

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        interactableLayer = LayerMask.NameToLayer("Interactable");
    }

    public void SetCurrentNoodle(GameObject noodle)
    {
        currentNoodleGO = noodle;
        currentSmokeGO = noodle.transform.Find("Smoke").gameObject;
        currentWaterGO = noodle.transform.Find("Water").gameObject;
        currentSaucePackGO = noodle.transform.Find("SaucePack").gameObject;

        currentNoodleStatus = NoodleStatus.Unprepared;

        currentWaterMat = currentWaterGO.GetComponent<MeshRenderer>().material;
        currentNoodleCollider = currentNoodleGO.GetComponent<Collider>();

        waterCollider.enabled = true;

        waterStartPos = currentWaterGO.transform.localPosition;
        waterStartScale = currentWaterGO.transform.localScale;
    }

    public void AddWaterToNoodle()
    {
        currentDrops++;
        float t = Mathf.Clamp01((float)currentDrops / requiredDrops);

        currentWaterGO.transform.localPosition = Vector3.Lerp(waterStartPos, waterEndPos, t);
        currentWaterGO.transform.localScale = Vector3.Lerp(waterStartScale, waterEndScale, t);
        
        currentSmokeGO.SetActive(true);

        if (t > 0.9f)
        {
            waterCollider.isTrigger = true;
            hologramKettle.SetActive(true);
        }
    }

    public void AddSauceToWater()
    {
        StartCoroutine(LerpColor());
    }

    private IEnumerator LerpColor()
    {
        Color startColor = currentWaterMat.color;

        float elapsedTime = 0f;

        while (elapsedTime < timeToSauceWater)
        {
            currentWaterMat.color = Color.Lerp(startColor, saucedWaterColor, elapsedTime / timeToSauceWater);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        currentWaterMat.color = saucedWaterColor;
        currentNoodleGO.layer = interactableLayer;
        currentNoodleCollider.enabled = true;
    }

}
