using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoodleShelf : MonoBehaviour, IInteractable
{
    private GameObject[] childs;

    [Header("Text Settings")]
    public GameObject grabNoodleText;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    [Header("Noodle Settings")]
    [SerializeField] private GameObject noodle;
    [SerializeField] private Transform pointToSpawnNoodle;
    [SerializeField] private GameObject storeBlocker;

    private GameObject instantiatedNoodle;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    private void Awake()
    {

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        childs = new GameObject[transform.childCount];

        for (int i = 0; i < childs.Length; i++)
        {
            childs[i] = transform.GetChild(i).gameObject;
        }

        instantiatedNoodle = null;
    }
    public void OnFocus()
    {
        grabNoodleText.SetActive(true);
        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnInteract()
    {
        if (instantiatedNoodle != null)
        {
            Destroy(instantiatedNoodle);
            instantiatedNoodle = null;
        }

        instantiatedNoodle = Instantiate(noodle, pointToSpawnNoodle.position, Quaternion.Euler(0f, 0f, 0f), null);
        GameManager.Instance.ResetPlayerGrabAndInteract();
        GameManager.Instance.ChangePlayerCurrentGrabable(instantiatedNoodle.GetComponent<IGrabable>());

        storeBlocker.SetActive(true);
    }

    public void OnLoseFocus()
    {
        grabNoodleText.SetActive(false);
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

    public void ChangeLayer(int layerIndex)
    {
        gameObject.layer = layerIndex;
        foreach (GameObject child in childs)
            child.layer = layerIndex;
    }
}
