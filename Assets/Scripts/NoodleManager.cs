using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoodleManager : MonoBehaviour
{
    public static NoodleManager Instance;

    public enum NoodleStatus
    {
        OnShelf,
        JustGrabbed,
        JustBought,
        ReadyToPrepare,
        LidOpened,
        SaucePackInstantiated,
        OnHouseHologram,
        FilledWithWater,
        SauceAdded,
        LidClosed,
        WaitingToBeReady,
        Ready,
        Finished
    }

    [Header("Day1 Settings")]
    [SerializeField] private GameObject afterFirstNoodleCutsceneTrigger;
    [SerializeField] private DialogueData afterFirstNoodleSelfTalk;

    [Header("Holograms")]
    [SerializeField] private GameObject hologramHouseNoodle;
    [SerializeField] private GameObject hologramKettle;
    [SerializeField] private GameObject hologramSaucePack;

    [Header("GameObjects")]
    [SerializeField] private GameObject kettleGO;

    [Header("Close The Door And Prepare Noodle Settings")]
    [SerializeField] private GameObject playerTriggerCloseTheDoorAndStartNoodlePrepare;
    [SerializeField] private Door houseDoor;
    [SerializeField] private DialogueData[] closeTheDoorAndStartNoodlePrepareDialogues;

    [Header("Noodle Water Settings")]
    private Vector3 waterStartPos;
    public Vector3 waterEndPos;
    private Vector3 waterStartScale;
    public Vector3 waterEndScale;
    public int requiredDrops = 200;
    private int currentDrops = 0;
    [Space]
    [SerializeField] private Color saucedWaterColor;
    [SerializeField] private float timeToSauceWater = 0.3f;

    [Header("Cooking Settings")]
    [SerializeField] private Mesh cookedNoodleMesh;
    [SerializeField] private Mesh finishedNoodleMesh;
    [Space]
    [SerializeField] private Material cookedNoodleMat;
    [SerializeField] private Material finishedNoodleMat;

    [Header("ShopSeller")]
    [SerializeField] private ShopSeller shopSeller;

    [HideInInspector] public GameObject currentNoodleGO;
    [HideInInspector] public GameObject currentSmokeGO;
    [HideInInspector] public GameObject currentWaterGO;
    [HideInInspector] public GameObject currentSaucePackGO;

    private Noodle currentNoodleScript;
    private SaucePack currentSaucePackScript;
    private Kettle kettleScript;

    private bool saucePackIsOnHologram;

    private Material currentWaterMat;
    private Material hologramHouseNoodleMat;
    private Material hologramSaucePackMat;
    private Material hologramKettleMat;

    private SkinnedMeshRenderer currentNoodleSkinnedMeshRenderer;
    private SkinnedMeshRenderer hologramHouseNoodleSkinnedMeshRenderer;

    private ParticleSystem currentSmokePS;

    private Color hologramHouseNoodleMatDefaultColor;
    private Color hologramSaucePackMatDefaultColor;
    private Color hologramKettleMatDefaultColor;

    private Collider hologramKettleCollider;
    private Collider hologramHouseNoodleCollider;
    private Collider hologramSaucePackCollider;

    private int grabableLayer;
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
        interactableLayer = LayerMask.NameToLayer("Interactable");

        hologramHouseNoodleMat = hologramHouseNoodle.GetComponent<Renderer>().material;
        hologramSaucePackMat = hologramSaucePack.GetComponent<Renderer>().material;
        hologramKettleMat = hologramKettle.GetComponent<Renderer>().material;

        hologramHouseNoodleMatDefaultColor = hologramHouseNoodleMat.color;
        hologramSaucePackMatDefaultColor = hologramSaucePackMat.color;
        hologramKettleMatDefaultColor = hologramKettleMat.color;

        kettleScript = kettleGO.GetComponent<Kettle>();

        hologramKettleCollider = hologramKettle.GetComponent<Collider>();
        hologramHouseNoodleCollider = hologramHouseNoodle.GetComponent<Collider>();
        hologramSaucePackCollider = hologramSaucePack.GetComponent<Collider>();

        hologramHouseNoodleSkinnedMeshRenderer = hologramHouseNoodle.GetComponent<SkinnedMeshRenderer>();

        SetHologramHouseNoodle(false);
        SetHologramSaucePack(false);
        SetHologramKettle(false);

        SetHologramKettleCollider(false);
    }

    public void SetCurrentNoodleStatus(NoodleStatus status)
    {
        if (currentNoodleScript == null) return;

        currentNoodleScript.NoodleStatus = status;
    }

    public NoodleStatus GetCurrentNoodleStatus() => currentNoodleScript.NoodleStatus;

    public void HandleHologramNoodleLid(float value)
    {
        hologramHouseNoodleSkinnedMeshRenderer.SetBlendShapeWeight(0, value);
    }

    public void SetKettleGrabable()
    {
        kettleGO.layer = grabableLayer;
    }

    private void TrySettingKettleGrabable()
    {
        if (saucePackIsOnHologram && currentNoodleScript.NoodleStatus == NoodleStatus.OnHouseHologram)
            SetKettleGrabable();
    }

    public void SetHologramHouseNoodle(bool shouldTurnOn)
    {
        hologramHouseNoodleMat.color = shouldTurnOn ? hologramHouseNoodleMatDefaultColor : Color.clear;
    }

    public void SetHologramSaucePack(bool shouldTurnOn)
    {
        if (!saucePackIsOnHologram)
            hologramSaucePackMat.color = shouldTurnOn ? hologramSaucePackMatDefaultColor : Color.clear;
    }

    public void SetHologramKettle(bool shouldTurnOn)
    {
        hologramKettleMat.color = shouldTurnOn ? hologramKettleMatDefaultColor : Color.clear;
    }

    public void SetHologramKettleCollider(bool shouldTurnOn)
    {
        hologramKettleCollider.enabled = shouldTurnOn;
    }

    public void SetHologramHouseNoodleCollider(bool shouldTurnOn)
    {
        hologramHouseNoodleCollider.enabled = shouldTurnOn;
    }

    public void SetHologramSaucePackCollider(bool shouldTurnOn)
    {
        hologramSaucePackCollider.enabled = shouldTurnOn;
    }

    public void PutCurrentNoodleOnHologramHouse()
    {
        if (currentNoodleScript.NoodleStatus == NoodleStatus.SaucePackInstantiated || currentNoodleScript.NoodleStatus == NoodleStatus.LidClosed)
        {
            SetHologramHouseNoodleCollider(false);
            currentNoodleScript.PutOnHologram(hologramHouseNoodle.transform.position, hologramHouseNoodle.transform.rotation);
            TrySettingKettleGrabable();
        }
    }

    public void PutCurrentSaucePackOnHologram()
    {
        SetHologramSaucePackCollider(false);
        currentSaucePackScript.PutOnHologram(hologramSaucePack.transform.position, hologramSaucePack.transform.rotation);
        saucePackIsOnHologram = true;
        TrySettingKettleGrabable();
    }

    public void PutKettleOnHologram()
    {
        SetHologramKettleCollider(false);
        kettleScript.PutOnHologram(hologramKettle.transform.position, hologramKettle.transform.rotation);
        currentSaucePackGO.layer = grabableLayer;
        currentSaucePackScript.CanGetFocused = true;
    }

    public void SetCurrentSmokeParticleSystem(bool shouldPlay)
    {
        if (shouldPlay)
            currentSmokePS.Play();
        else
            currentSmokePS.Stop();
    }

    public void SetCurrentNoodle(GameObject noodle)
    {
        currentNoodleGO = noodle;
        currentSmokeGO = noodle.transform.Find("Smoke").gameObject;
        currentWaterGO = noodle.transform.Find("Water").gameObject;

        currentNoodleScript = noodle.GetComponent<Noodle>();

        currentWaterMat = currentWaterGO.GetComponent<MeshRenderer>().material;

        currentSmokePS = currentSmokeGO.GetComponent<ParticleSystem>();

        shopSeller.CurrentStatus = ShopSeller.ShopSellerStatus.NoodleObtained;

        currentNoodleSkinnedMeshRenderer = currentNoodleGO.GetComponent<SkinnedMeshRenderer>();

        waterStartPos = currentWaterGO.transform.localPosition;
        waterStartScale = currentWaterGO.transform.localScale;

        playerTriggerCloseTheDoorAndStartNoodlePrepare.SetActive(true);
    }

    public void HandleCloseTheDoorAndStartNoodlePrepare()
    {
        if (currentNoodleScript == null) return;

        int rand = Random.Range(0, closeTheDoorAndStartNoodlePrepareDialogues.Length);

        if (!houseDoor.isOpened)
        {
            PlayerManager.Instance.ResetPlayerInteract(houseDoor, true);
            DialogueManager.Instance.StartSelfDialogue(closeTheDoorAndStartNoodlePrepareDialogues[rand]);
        }

        else
        {
            houseDoor.dialogueAfterInteraction = closeTheDoorAndStartNoodlePrepareDialogues[rand];
            houseDoor.shouldBeUninteractableAfterInteraction = true;
            houseDoor.shouldPlayDialogueAfterInteraction = true;
        }

        currentNoodleScript.IsUseable = true;
        PlayerManager.Instance.DecideUIText();
    }

    public void SetCurrentSaucePack(GameObject saucePack)
    {
        currentSaucePackGO = saucePack;
        currentSaucePackScript = saucePack.GetComponent<SaucePack>();
    }

    public void CookNoodle()
    {
        currentNoodleSkinnedMeshRenderer.sharedMesh = cookedNoodleMesh;
        currentNoodleSkinnedMeshRenderer.material = cookedNoodleMat;
        currentWaterGO.SetActive(false);

        currentNoodleScript.NoodleStatus = NoodleStatus.Ready;
        currentNoodleScript.CookAmount = Cookable.CookAmount.REGULAR;
        currentNoodleScript.IsUseable = true;
        currentNoodleGO.layer = grabableLayer;
        currentNoodleScript.CanGetFocused = true;
    }

    public void AddWaterToNoodle()
    {
        currentDrops++;
        float t = Mathf.Clamp01((float)currentDrops / requiredDrops);

        currentWaterGO.transform.localPosition = Vector3.Lerp(waterStartPos, waterEndPos, t);
        currentWaterGO.transform.localScale = Vector3.Lerp(waterStartScale, waterEndScale, t);
        
        

        if (t > 0.5f)
        {
            currentSmokeGO.SetActive(true);
        }

        if (t > 0.8f)
        {
            currentNoodleScript.NoodleStatus = NoodleStatus.FilledWithWater;
            SetHologramKettle(true);
            SetHologramKettleCollider(true);
        }
    }

    public void AddSauceToWater()
    {
        if (currentNoodleScript.NoodleStatus == NoodleStatus.FilledWithWater)
        {
            Destroy(currentSaucePackGO);

            currentNoodleScript.IsUseable = true;
            currentNoodleGO.layer = grabableLayer;
            currentNoodleScript.NoodleStatus = NoodleStatus.SauceAdded;
            currentNoodleScript.CanGetFocused = true;

            StartCoroutine(LerpColor());
        }
        
    }

    public void HandleAfterNoodle()
    {
        if (DayManager.Instance.DayCount == 0)
        {
            afterFirstNoodleCutsceneTrigger.SetActive(true);
            DialogueManager.Instance.StartSelfDialogue(afterFirstNoodleSelfTalk);

            houseDoor.dialogueAfterInteraction = null;
            houseDoor.shouldBeUninteractableAfterInteraction = false;
            houseDoor.shouldPlayDialogueAfterInteraction = false;

            houseDoor.CanInteract = true;
            houseDoor.ChangeLayer(interactableLayer);
        }
            
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
    }

}
