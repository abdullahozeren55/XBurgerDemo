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
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    private MeshCollider col;

    [Header("Child Settings")]
    [SerializeField] private GameObject lidPart;
    [SerializeField] private GameObject bucketPart;
    [SerializeField] private Mesh[] lidPartMeshes;
    [SerializeField] private Mesh[] bucketPartMeshes;

    [SerializeField] private ParticleSystem smoke;

    private MeshFilter lidMeshFilter;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;
    void Awake()
    {

        isOpened = false;

        audioSource = GetComponent<AudioSource>();
        col = GetComponent<MeshCollider>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        lidMeshFilter = lidPart.GetComponent<MeshFilter>();
    }

    public void OnFocus()
    {
        HandleText(true);
        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnInteract()
    {
        PlaySound(isOpened);

        isOpened = !isOpened;

        if (isOpened)
        {
            lidMeshFilter.mesh = lidPartMeshes[0];
            col.enabled = false;

            if (NoodleManager.Instance.currentSaucePackGO != null)
                NoodleManager.Instance.currentSaucePackGO.GetComponent<Collider>().enabled = true;
        }
        else
        {
            lidMeshFilter.mesh = lidPartMeshes[1];
            col.enabled = false;

            var main = smoke.main;
            main.stopAction = ParticleSystemStopAction.Callback;

            // Disable the emission
            var emission = smoke.emission;
            emission.rateOverTime = 0f;

            NoodleManager.Instance.currentNoodleStatus = NoodleManager.NoodleStatus.Prepared;

            if (GameManager.Instance.DayCount == 1)
                GameManager.Instance.HandleAfterFirstNoodle();
        }

    }

    public void OnLoseFocus()
    {
        HandleText(false);
        ChangeLayer(interactableLayer);
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedRedLayer);
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedLayer);
        }
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
}
