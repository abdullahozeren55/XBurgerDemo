using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoodleInteractable : MonoBehaviour, IInteractable
{
    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip closeSound;
    private AudioSource audioSource;

    [Header("Text Settings")]
    public GameObject openText;
    public GameObject closeText;

    private bool isOpened;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int uninteractableLayer;

    private MeshCollider col;

    [Header("Child Settings")]
    [SerializeField] private GameObject lidPart;
    [SerializeField] private GameObject bucketPart;
    [SerializeField] private Mesh[] lidPartMeshes;
    [SerializeField] private Mesh[] bucketPartMeshes;

    [SerializeField] private GameObject saucePack;
    [SerializeField] private GameObject water;
    [SerializeField] private ParticleSystem smoke;
    [SerializeField] private Color targetWaterColor;
    [SerializeField] private float colorLerpTime = 0.3f;

    private MeshFilter lidMeshFilter;
    private Animator anim;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;
    void Awake()
    {

        isOpened = false;

        audioSource = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();
        col = GetComponent<MeshCollider>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        lidMeshFilter = lidPart.GetComponent<MeshFilter>();
    }

    public void OnFocus()
    {
        HandleText(true);
        ChangeLayer(interactableOutlinedLayer);
    }

    public void OnInteract()
    {
        PlaySound(isOpened);

        isOpened = !isOpened;

        if (isOpened)
        {
            lidMeshFilter.mesh = lidPartMeshes[0];
            col.enabled = false;
            saucePack.GetComponent<Collider>().enabled = true;
        }
        else
        {
            lidMeshFilter.mesh = lidPartMeshes[1];

            var main = smoke.main;
            main.stopAction = ParticleSystemStopAction.Callback;

            // Disable the emission
            var emission = smoke.emission;
            emission.rateOverTime = 0f;
        }

    }

    public void OnLoseFocus()
    {
        HandleText(false);
        ChangeLayer(interactableLayer);
    }

    private void HandleText(bool isFocused)
    {
        if (isFocused)
        {
            openText.SetActive(!isOpened);
            closeText.SetActive(isOpened);
        }
        else
        {
            if (openText.activeSelf) openText.SetActive(false);
            if (closeText.activeSelf) closeText.SetActive(false);
        }
    }

    private void PlaySound(bool isOpen)
    {
        audioSource.Stop();

        if (isOpen)
        {
            audioSource.PlayOneShot(closeSound);
        }
        else
        {
            audioSource.PlayOneShot(openSound);
        }
    }

    private void ChangeLayer(int layerIndex)
    {
        gameObject.layer = layerIndex;
        lidPart.layer = layerIndex;
        bucketPart.layer = layerIndex;
    }

    private void ApplySaucePack()
    {
        Material currentWaterMat = water.GetComponent<MeshRenderer>().material;

        StartCoroutine(LerpColor(currentWaterMat));

        Destroy(saucePack, 0.2f);
    }

    public void PourWater()
    {
        anim.Play("NoodleWater");
    }

    private IEnumerator LerpColor(Material mat)
    {
        Color startColor = mat.color;

        float elapsedTime = 0f;

        while (elapsedTime < colorLerpTime)
        {
            mat.color = Color.Lerp(startColor, targetWaterColor, elapsedTime / colorLerpTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        mat.color = targetWaterColor;
    }
}
