using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations.Rigging;
using Unity.VisualScripting;
using Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FirstPersonController : MonoBehaviour
{
    public bool CanMove = true;
    public bool IsUsingItemX = false;
    public bool IsUsingItemY = false;
    private bool IsSprinting => canSprint && Input.GetKey(sprintKey);
    private bool ShouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;
    private bool ShouldCrouch => Input.GetKey(crouchKey);

    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canUseHeadbob = true;
    [SerializeField] private bool willSlideOnSlopes = true;
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool canGrab = true;
    [SerializeField] private bool useFootsteps = true;

    [Header("Controls")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode interactKey = KeyCode.Mouse0;
    public KeyCode throwKey = KeyCode.Mouse1;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float slopeSpeed = 8.0f;
    [SerializeField] private float pushForce = 1f;
    [HideInInspector] public bool isBeingPushedByACustomer = false;

    [Header("Look Parameters")]
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform cameraFollow;
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 100)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 100)] private float lowerLookLimit = 45.0f;
    private float horizSpeed;
    private float vertSpeed;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;
    [SerializeField] private AudioSource jumpLandAudioSource = default;
    [SerializeField] private AudioClip[] woodJumpClips = default;
    [SerializeField] private AudioClip[] metalJumpClips = default;
    [SerializeField] private AudioClip[] grassJumpClips = default;
    [SerializeField] private AudioClip[] stoneJumpClips = default;
    [SerializeField] private AudioClip[] tileJumpClips = default;
    [SerializeField] private AudioClip[] gravelJumpClips = default;
    private float lastGroundedTime;
    private AudioClip lastPlayedJump;

    [Header("Landing Parameters")]
    [SerializeField] private AudioClip[] woodLandClips = default;
    [SerializeField] private AudioClip[] metalLandClips = default;
    [SerializeField] private AudioClip[] grassLandClips = default;
    [SerializeField] private AudioClip[] stoneLandClips = default;
    [SerializeField] private AudioClip[] tileLandClips = default;
    [SerializeField] private AudioClip[] gravelLandClips = default;
    [SerializeField] private GameObject landGroundCheckPoint;
    [SerializeField] private float noLandSoundCoyoteTime = 0.1f;
    private AudioClip lastPlayedLand;
    private bool justJumped = false;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchingHeight = 0.5f;
    [SerializeField] private float standingHeight = 2.0f;
    [SerializeField] private float timeToCrouchStand = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Vector3 standingCenter = new Vector3(0f, 0f, 0f);
    private bool isCrouching;
    private Coroutine CrouchStandCoroutine = null;

    [Header("Headbob Parameters")]
    [SerializeField] private Transform eyeLevel;

    [Header("Sliding Parameters")]
    private Vector3 hitPointNormal;
    private bool IsSliding
    {
        get
        {
            if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f, groundTypeLayers))
            {
                hitPointNormal = slopeHit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    }

    [Header("Interaction Parameters")]
    [SerializeField] private Vector3 interactionRayPoint = default;
    [SerializeField] private float interactionDistance = default;
    [SerializeField] private Transform crosshairTransform = default;
    [SerializeField] private Vector3 interactableCrosshairSize = default;
    [SerializeField] private LayerMask interactionLayers = default;
    private float interactChargeTimer = 0f;
    private Vector3 defaultCrosshairSize;
    private IInteractable currentInteractable;

    [Header("Grab Parameters")]
    [SerializeField] private LayerMask grabableLayers;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color grabCrosshairColor;
    [SerializeField] private Color useCrosshairColor;
    [SerializeField] private float maxThrowForce = 1.3f;
    [SerializeField] private float minThrowForce = 0.3f;
    [SerializeField] private float throwMaxChargeTime = 1.5f;
    [SerializeField] private float quickTapThreshold = 0.2f;
    private bool isUsingGrabbedItem = false;
    private float throwChargeTimer = 0f;
    private float currentThrowForce = 0f;
    private Color defaultCrosshairColor;
    private IGrabable currentGrabable;
    private IGrabable otherGrabable;

    [Header("Footstep Parameters")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepMultiplier = 1.5f;
    [SerializeField] private float sprintStepMultiplier = 0.6f;
    [SerializeField] private AudioSource footstepAudioSource = default;
    [SerializeField] private AudioClip[] woodClips = default;
    [SerializeField] private AudioClip[] metalClips = default;
    [SerializeField] private AudioClip[] grassClips = default;
    [SerializeField] private AudioClip[] stoneClips = default;
    [SerializeField] private AudioClip[] tileClips = default;
    [SerializeField] private AudioClip[] gravelClips = default;
    [SerializeField] private LayerMask groundTypeLayers;
    [SerializeField] private GameObject groundTypeCheckRayPoint;
    private float footstepTimer = 0f;
    private Material currentGroundMaterial;
    private AudioClip lastPlayedFootstep;

    private float GetCurrentOffset => isCrouching ? baseStepSpeed * crouchStepMultiplier : IsSprinting ? baseStepSpeed * sprintStepMultiplier : baseStepSpeed;

    [Header("Interaction Hand Parameters")]
    [SerializeField] private TwoBoneIKConstraint twoBoneIKConstraintLeftHand;
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Vector3 positionOffsetForLeftHandInteraction;
    [SerializeField] private Vector3 rotationOffsetForLeftHandInteraction;
    private Vector3 currentPositionOffsetForLeftHand;
    private Vector3 currentRotationOffsetForLeftHand;
    private Coroutine leftHandRigLerpCoroutine;
    [Space]
    [SerializeField] private TwoBoneIKConstraint twoBoneIKConstraintRightHand;
    [SerializeField] private Transform rightHandTarget;
    [SerializeField] private Vector3 positionOffsetForRightHandInteraction;
    [SerializeField] private Vector3 rotationOffsetForRightHandInteraction;
    private Vector3 currentPositionOffsetForRightHand;
    private Vector3 currentRotationOffsetForRightHand;
    private Coroutine rightHandRigLerpCoroutine;
    [Space]
    [SerializeField] private float lerpTimeForInteractionHand = 0.2f;

    [Header("Grab Hand Parameters")]
    [SerializeField] private CinemachineVirtualCamera firstPersonCam;
    [SerializeField] private float maxAmplitudeGain = 0.35f;
    [SerializeField] private float maxFrequencyGain = 0.7f;
    [SerializeField] private float maxFOV = 50f;
    private float normalFOV;
    [Space]
    [SerializeField] private Volume postProcessVolume;
    [Space]
    [Header("Grab Parameters")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private Image focusText;
    private Coroutine grabbedUseCoroutine;

    [Header("HandControlSettings")]
    [SerializeField, Range(0, 10)] private float handControlSpeedX = 0.2f;
    [SerializeField, Range(0, 10)] private float handControlSpeedY = 0.2f;
    private Vector3 handUseStartOffset;
    private Vector3 handUseDelta; // mouse hareketlerini biriktirecek

    [Header("UI Settings")]
    [SerializeField] private GameObject interactUseDropThrowUI;
    [SerializeField] private GameObject grabInteractUI;

    private Coroutine singleHandThrowCoroutine;
    private Coroutine throwVisualEffectsCoroutine;
    private CinemachineBasicMultiChannelPerlin perlin;
    private Vignette vignette;

    private Camera mainCamera;
    private CharacterController characterController;
    private Animator anim;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0f;

    private void Awake()
    {

        mainCamera = Camera.main;
        characterController = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        perlin = firstPersonCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        normalFOV = firstPersonCam.m_Lens.FieldOfView;

        defaultCrosshairSize = crosshairTransform.localScale;
        defaultCrosshairColor = crosshairImage.color;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        // Get the effects from the volume
        if (postProcessVolume.profile.TryGet(out Vignette v))
            vignette = v;

        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            smr.updateWhenOffscreen = true; // ensures it's never culled
            smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 5f); // overly large bounds to guarantee visibility
        }

        CameraManager.Instance.InitializeCamera(CameraManager.CameraName.FirstPerson);
    }

    private void Update()
    {
        if (CanMove)
        {
            HandleMovementInput();

            if (canJump)
                HandleJump();

            if (!characterController.isGrounded)
                HandleLand();
            else
            {
                anim.SetBool("isGrounded", true);
                lastGroundedTime = Time.time;
            }
                

            if (canCrouch)
                HandleCrouch();

            if (canInteract)
            {
                HandleInteractionInput();
                HandleInteractionCheck();
            }

            if (canGrab)
            {
                HandleGrabInput();
                HandleGrabCheck();
            }

            if (useFootsteps)
                HandleFootsteps();
                

            ApplyFinalMovements();
        }

        else
        {
            if (!characterController.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
                characterController.Move(moveDirection * Time.deltaTime);

                HandleLand();
            }

            if (currentInteractable != null)
            {
                currentInteractable.OnLoseFocus();
                currentInteractable = null;
                ChangeCrosshairSize(defaultCrosshairSize);
            }

            anim.SetFloat("speed", 0f, 0.15f, Time.deltaTime);

        }
                
    }

    private void LateUpdate()
    {   
        if (CanMove)
        {
            HandleMouseAndHandControl();
            HandleHandTargetPositions();
        }

        if (canUseHeadbob)
            HandleHeadbob();
    }

    private void HandleMouseAndHandControl()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // --- Kamera Hareketi ---
        horizSpeed = lookSpeedX;
        vertSpeed = lookSpeedY;

        if (IsUsingItemX)
        {
            horizSpeed = handControlSpeedX * 5f;
            vertSpeed /= 4f;
        }

        if (IsUsingItemY)
        {
            vertSpeed = handControlSpeedY * 5f;
            horizSpeed /= 4f;
        }

        // Vertical look (Pitch)
        rotationX -= mouseY * vertSpeed;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        headBone.localRotation = Quaternion.Euler(rotationX, 0, 0);
        cameraFollow.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // Horizontal look (Yaw)
        transform.Rotate(Vector3.up * mouseX * horizSpeed);

        // --- El Hareketi ---
        if (IsUsingItemX || IsUsingItemY)
        {
            Vector3 delta = Vector3.zero;
            if (IsUsingItemX) delta.x = mouseX * handControlSpeedX;
            if (IsUsingItemY) delta.y = mouseY * handControlSpeedY;

            handUseDelta += delta;

            // Toplam offset = baþlangýç + delta
            currentPositionOffsetForRightHand = handUseStartOffset + handUseDelta;

            // Clamp lokal eksende
            handUseDelta.x = Mathf.Clamp(handUseDelta.x, -1.5f, 1.5f);
            handUseDelta.y = Mathf.Clamp(handUseDelta.y, -1.5f, 1.5f);
            handUseDelta.z = Mathf.Clamp(handUseDelta.z, 0.1f, 0.3f); // ileri-geri
        }
    }

    private void HandleHandTargetPositions()
    {
        /*if (currentGrabable != null && currentGrabable.IsGrabbed)
        {
            currentPositionOffsetForRightHand = currentGrabable.GrabPositionOffset;
            currentRotationOffsetForRightHand = currentGrabable.GrabRotationOffset;
        }*/

        leftHandTarget.position = mainCamera.transform.position + mainCamera.transform.TransformDirection(currentPositionOffsetForLeftHand);
        leftHandTarget.rotation = mainCamera.transform.rotation * Quaternion.Euler(currentRotationOffsetForLeftHand);
        rightHandTarget.position = mainCamera.transform.position + mainCamera.transform.TransformDirection(currentPositionOffsetForRightHand);
        rightHandTarget.rotation = mainCamera.transform.rotation * Quaternion.Euler(currentRotationOffsetForRightHand);
    }

    private void HandleMovementInput()
    {
        float speed = isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;

        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        currentInput = new Vector2(speed * vertical, speed * horizontal);

        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) +
                        (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;

        // Calculate movement magnitude (ignores Y)
        float horizontalSpeed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude;
        anim.SetFloat("speed", horizontalSpeed, 0.15f, Time.deltaTime);
    }

    private void HandleJump()
    {
        if (ShouldJump)
        {

            anim.SetBool("isGrounded", false);
            anim.SetTrigger("jump");

            FindCurrentGroundMaterial(1.9f);

            if (currentGroundMaterial != null)
            {
                if (currentGroundMaterial.name.Contains("Wood"))
                    PlayJumpLand(woodJumpClips, true);
                else if (currentGroundMaterial.name.Contains("Metal"))
                    PlayJumpLand(metalJumpClips, true);
                else if (currentGroundMaterial.name.Contains("Grass"))
                    PlayJumpLand(grassJumpClips, true);
                else if (currentGroundMaterial.name.Contains("Stone"))
                    PlayJumpLand(stoneJumpClips, true);
                else if (currentGroundMaterial.name.Contains("Tile"))
                    PlayJumpLand(tileJumpClips, true);
                else if (currentGroundMaterial.name.Contains("Gravel"))
                    PlayJumpLand(gravelJumpClips, true);
            }

            moveDirection.y = jumpForce;

            justJumped = true;
        }
            
    }

    private void HandleLand()
    {
        Physics.Raycast(landGroundCheckPoint.transform.position, Vector3.down, out RaycastHit hit, 1f, groundTypeLayers);

        if (hit.collider != null && moveDirection.y <= -0.2f)
        {

            if ((!jumpLandAudioSource.isPlaying && !isBeingPushedByACustomer && Time.time > lastGroundedTime + noLandSoundCoyoteTime) || justJumped)
            {
                FindCurrentGroundMaterial(10f);

                if (currentGroundMaterial != null)
                {
                    if (currentGroundMaterial.name.Contains("Wood"))
                        PlayJumpLand(woodLandClips, false);
                    else if (currentGroundMaterial.name.Contains("Metal"))
                        PlayJumpLand(metalLandClips, false);
                    else if (currentGroundMaterial.name.Contains("Grass"))
                        PlayJumpLand(grassLandClips, false);
                    else if (currentGroundMaterial.name.Contains("Stone"))
                        PlayJumpLand(stoneLandClips, false);
                    else if (currentGroundMaterial.name.Contains("Tile"))
                        PlayJumpLand(tileLandClips, false);
                    else if (currentGroundMaterial.name.Contains("Gravel"))
                        PlayJumpLand(gravelLandClips, false);
                }
            }

            justJumped = false;
        }
    }

    private void HandleCrouch()
    {
        if ((ShouldCrouch && !isCrouching) || (isCrouching && !ShouldCrouch && !Physics.Raycast(mainCamera.transform.position, Vector3.up, standingHeight/1.7f)))
        {
            if (CrouchStandCoroutine != null)
            {
                StopCoroutine(CrouchStandCoroutine);
                CrouchStandCoroutine = null;
            }

            CrouchStandCoroutine = StartCoroutine(CrouchStand(ShouldCrouch));
        }
    }

    private void HandleHeadbob()
    {
        cameraFollow.position = eyeLevel.position;
    }

    private void DecideInteractableOutlineColor()
    {
        if (currentInteractable != null)
        {
            if (isUsingGrabbedItem ||
            (currentGrabable != null && currentGrabable.IsGrabbed && currentInteractable.HandRigType == PlayerManager.HandRigTypes.SingleHandGrab))
            {
                currentInteractable.OutlineShouldBeRed = true;
            }
            else
            {
                currentInteractable.OutlineShouldBeRed = false;
            }

            currentInteractable.OutlineChangeCheck();
        }
        
    }

    private void DecideGrabableOutlineColor()
    {
        if (currentGrabable != null && currentGrabable.IsGrabbed && otherGrabable != null)
        {
            otherGrabable.OutlineShouldBeRed = true;
            otherGrabable.OutlineChangeCheck();
        }
        else
        {
            if (otherGrabable != null)
            {
                otherGrabable.OutlineShouldBeRed = false;
                otherGrabable.OutlineChangeCheck();
            }
                
        }
    }

    private void DecideCrosshairColor()
    {
        if (currentInteractable != null && currentInteractable.OutlineShouldBeRed)
        {
            ChangeCrosshairColor(useCrosshairColor);
        }
        else if (currentGrabable != null)
        {
            ChangeCrosshairColor(!currentGrabable.IsGrabbed ? grabCrosshairColor : otherGrabable != null ? useCrosshairColor : defaultCrosshairColor);
        }
        else
        {
            ChangeCrosshairColor(defaultCrosshairColor);
        }
    }

    private void DecideCrosshairSize()
    {
        if (currentInteractable != null || (currentGrabable != null && (!currentGrabable.IsGrabbed || otherGrabable != null)))
        {
            ChangeCrosshairSize(interactableCrosshairSize);
        }
        else
        {
            ChangeCrosshairSize(defaultCrosshairSize);
        }
    }

    private void DecideFocusText()
    {
        if (currentInteractable != null)
        {
            focusText.sprite = currentInteractable.FocusImage;
            focusText.color = crosshairImage.color;
        }
        else if (currentGrabable != null && !currentGrabable.IsGrabbed)
        {
            focusText.sprite = currentGrabable.FocusImage;
            focusText.color = crosshairImage.color;
        }
        else if (otherGrabable != null)
        {
            focusText.sprite = otherGrabable.FocusImage;
            focusText.color = crosshairImage.color;
        }
        else
        {
            focusText.sprite = null;
            focusText.color = Color.clear;
        }
    }

    private void DecideUIText()
    {
        interactUseDropThrowUI.SetActive(currentGrabable != null && currentGrabable.IsGrabbed);
        grabInteractUI.SetActive((currentGrabable != null && !currentGrabable.IsGrabbed) || (currentInteractable != null && (currentGrabable == null || !currentGrabable.IsGrabbed)));
    }

    private void DecideOutlineAndCrosshair()
    {
        DecideInteractableOutlineColor();
        DecideGrabableOutlineColor();
        DecideCrosshairColor();
        DecideFocusText();
        DecideUIText();
        DecideCrosshairSize();
    }

    private void HandleInteractionCheck()
    {
        if (Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayers))
        {
            if (hit.collider)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlined") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlinedRed"))
                {

                    if (currentInteractable == null)
                    {
                        currentInteractable = hit.collider.gameObject.GetComponent<IInteractable>();

                        if (currentInteractable != null)
                        {
                            DecideOutlineAndCrosshair();
                            currentInteractable.OnFocus();
                        }
                        
                    }
                    else if (currentInteractable != hit.collider.gameObject.GetComponent<IInteractable>())
                    {

                        currentInteractable.OnLoseFocus();
                        currentInteractable = null;

                        currentInteractable = hit.collider.gameObject.GetComponent<IInteractable>();

                        if (currentInteractable != null)
                        {
                            DecideOutlineAndCrosshair();
                            currentInteractable.OnFocus();
                        }
                    }
                }
                else if (currentInteractable != null)
                {
                    currentInteractable.OnLoseFocus();
                    currentInteractable = null;
                    DecideOutlineAndCrosshair();
                }
            }
            else if (currentInteractable != null)
            {
                currentInteractable.OnLoseFocus();
                currentInteractable = null;
                DecideOutlineAndCrosshair();
            }
        }
        else if (currentInteractable != null)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
            DecideOutlineAndCrosshair();
        }
    }

    private void HandleInteractionInput()
    {
        if (currentGrabable == null || !currentGrabable.IsGrabbed || !currentGrabable.IsUseable)
        {
            // EL BOS normal interact (tap)
            if (Input.GetKeyDown(interactKey))
            {
                TryToInteract();
            }
        }
        else if (throwChargeTimer < 0.1f)
        {

            if (Input.GetKey(interactKey))
            {
                interactChargeTimer += Time.deltaTime;

                if (!isUsingGrabbedItem && interactChargeTimer > quickTapThreshold)
                {
                    // threshold’u geçtiði an kullanmaya baþla
                    currentGrabable.OnUseHold();
                    isUsingGrabbedItem = true;

                    DecideOutlineAndCrosshair();
                }
            }

            if (Input.GetKeyUp(interactKey))
            {
                if (!isUsingGrabbedItem)
                {
                    // kýsa týk interact
                    TryToInteract();
                }
                else if (isUsingGrabbedItem)
                {
                    // uzun basma býrakýldý kullaným bitti
                    currentGrabable.OnUseRelease();
                }

                interactChargeTimer = 0f;
                isUsingGrabbedItem = false;

                DecideOutlineAndCrosshair();
            }
        }
        else
        {
            if (Input.GetKeyUp(interactKey))
            {
                TryToInteract();
            }
        }
    }

    private void TryToInteract()
    {
        if (currentInteractable != null && Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayers) && !currentInteractable.OutlineShouldBeRed)
        {
            if (hit.collider.gameObject.GetComponent<IInteractable>() == currentInteractable)
            {
                if (currentInteractable.HandRigType == PlayerManager.HandRigTypes.Talk)
                {
                    if (rightHandRigLerpCoroutine != null) StopCoroutine(rightHandRigLerpCoroutine);
                    if (leftHandRigLerpCoroutine != null) StopCoroutine(leftHandRigLerpCoroutine);

                    anim.SetTrigger("talk");
                }
                else if (currentGrabable == null || !currentGrabable.IsGrabbed)
                {
                    if (rightHandRigLerpCoroutine != null) StopCoroutine(rightHandRigLerpCoroutine);

                    if (currentInteractable.HandRigType == PlayerManager.HandRigTypes.Interaction)
                    {
                        currentPositionOffsetForRightHand = positionOffsetForRightHandInteraction;
                        currentRotationOffsetForRightHand = rotationOffsetForRightHandInteraction;
                    }

                    rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(true, true));
                }
                else
                {
                    if (leftHandRigLerpCoroutine != null) StopCoroutine(leftHandRigLerpCoroutine);

                    if (currentInteractable.HandRigType == PlayerManager.HandRigTypes.Interaction)
                    {
                        currentPositionOffsetForLeftHand = positionOffsetForLeftHandInteraction;
                        currentRotationOffsetForLeftHand = rotationOffsetForLeftHandInteraction;
                    }

                    leftHandRigLerpCoroutine = StartCoroutine(LerpLeftHandRig(true, true));
                }

                currentInteractable.OnInteract();
            }
        }
    }

    private void HandleGrabCheck()
    {

        if (Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, grabableLayers))
        {
            if (hit.collider)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Grabable") || hit.collider.gameObject.layer == LayerMask.NameToLayer("GrabableOutlined") || hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlinedRed"))
                {
                    if (currentGrabable == null)
                    {
                        if (otherGrabable != null)
                        {
                            otherGrabable.OnLoseFocus();
                            otherGrabable = null;
                            DecideOutlineAndCrosshair();
                        }

                        currentGrabable = hit.collider.gameObject.GetComponent<IGrabable>();
                        DecideOutlineAndCrosshair();
                        if (currentGrabable != null)
                            currentGrabable.OnFocus();
                    }
                    else if (currentGrabable.IsGrabbed)
                    {
                        if (otherGrabable != null)
                        {
                            otherGrabable.OnLoseFocus();
                            otherGrabable = null;
                            DecideOutlineAndCrosshair();
                        }

                        otherGrabable = hit.collider.gameObject.GetComponent<IGrabable>();

                        if (otherGrabable != null)
                        {
                            otherGrabable.OnFocus();
                            DecideOutlineAndCrosshair();   
                        }
                    }
                    else if (currentGrabable != hit.collider.gameObject.GetComponent<IGrabable>())
                    {
                        if (otherGrabable != null)
                        {
                            otherGrabable.OnLoseFocus();
                            otherGrabable = null;
                            DecideOutlineAndCrosshair();
                        }

                        currentGrabable.OnLoseFocus();
                        DecideOutlineAndCrosshair();
                        currentGrabable = hit.collider.gameObject.GetComponent<IGrabable>();
                        currentGrabable.OnFocus();
                        DecideOutlineAndCrosshair();
                    }
                }
                else
                {
                    if (currentGrabable != null && !currentGrabable.IsGrabbed)
                    {
                        currentGrabable.OnLoseFocus();
                        currentGrabable = null;
                        DecideOutlineAndCrosshair();
                    }

                    if (otherGrabable != null)
                    {
                        otherGrabable.OnLoseFocus();
                        otherGrabable = null;
                        DecideOutlineAndCrosshair();
                    }
                }

            }
            else
            {
                if (currentGrabable != null && !currentGrabable.IsGrabbed)
                {
                    currentGrabable.OnLoseFocus();
                    currentGrabable = null;
                    DecideOutlineAndCrosshair();
                }

                if (otherGrabable != null)
                {
                    otherGrabable.OnLoseFocus();
                    otherGrabable = null;
                    DecideOutlineAndCrosshair();
                }
            }
        }
        else
        {
            if (currentGrabable != null && !currentGrabable.IsGrabbed)
            {
                currentGrabable.OnLoseFocus();
                currentGrabable = null;
                DecideOutlineAndCrosshair();
            }

            if (otherGrabable != null)
            {
                otherGrabable.OnLoseFocus();
                otherGrabable = null;
                DecideOutlineAndCrosshair();
            }
        }
    }

    private void HandleGrabInput()
    {
        if (currentGrabable != null && currentGrabable.IsGrabbed && !isUsingGrabbedItem)
        {
            if (Input.GetKeyDown(throwKey)) // right click starts
            {
                ResetHandAnim();

                if (throwVisualEffectsCoroutine != null)
                {
                    StopCoroutine(throwVisualEffectsCoroutine);
                    throwVisualEffectsCoroutine = null;
                }

                throwVisualEffectsCoroutine = StartCoroutine(ThrowVisualEffects(true));

                anim.SetBool("chargingThrow", true);

                DecideOutlineAndCrosshair();

                if (singleHandThrowCoroutine != null)
                {
                    StopCoroutine(singleHandThrowCoroutine);
                    singleHandThrowCoroutine = null;
                }

                singleHandThrowCoroutine = StartCoroutine(SingleHandThrow());

            }
            if (Input.GetKeyUp(throwKey))
            {

                if (throwVisualEffectsCoroutine != null)
                {
                    StopCoroutine(throwVisualEffectsCoroutine);
                    throwVisualEffectsCoroutine = null;
                }
                    
                throwVisualEffectsCoroutine = StartCoroutine(ThrowVisualEffects(false));

                SetHandAnimBoolsOff();

                if (singleHandThrowCoroutine != null)
                {
                    StopCoroutine(singleHandThrowCoroutine);
                    singleHandThrowCoroutine = null;
                }

                if (rightHandRigLerpCoroutine != null)
                {
                    StopCoroutine(rightHandRigLerpCoroutine);
                    rightHandRigLerpCoroutine = null;
                }

                rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(false, false));

                ResetHandAnim();

                //TARGET CALCULATION START

                Vector3 targetPoint;
                Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

                if (Physics.Raycast(ray, out RaycastHit hit, 50f))
                    targetPoint = hit.point; // crosshair neye bakýyorsa
                else
                    targetPoint = ray.GetPoint(50f); // boþluða doðru uzaða

                Vector3 throwDir = (targetPoint - grabPoint.position).normalized;

                //TARGET CALCULATION END

                if (throwChargeTimer <= quickTapThreshold)
                {
                    currentGrabable.OnDrop(throwDir, isCrouching ? minThrowForce * 0.5f : IsSprinting ? minThrowForce * 1.5f : minThrowForce);
                }
                else
                {
                    currentGrabable.OnThrow(throwDir, isCrouching ? currentThrowForce * 0.5f : IsSprinting ? currentThrowForce * 1.5f : currentThrowForce);
                }

                throwChargeTimer = 0f;

                anim.SetBool("throw", false);

                DecideOutlineAndCrosshair();
            }
        }
        else if (Input.GetKeyDown(interactKey) && currentGrabable != null && Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, grabableLayers))
        {
            if (hit.collider.gameObject.GetComponent<IGrabable>() == currentGrabable)
            {
                
                DecideOutlineAndCrosshair();
                currentGrabable.OnGrab(grabPoint);
                DecideGrabAnimBool();

                if (rightHandRigLerpCoroutine != null)
                {
                    StopCoroutine(rightHandRigLerpCoroutine);
                    rightHandRigLerpCoroutine = null;
                }

                if (leftHandRigLerpCoroutine != null)
                {
                    StopCoroutine(leftHandRigLerpCoroutine);
                    leftHandRigLerpCoroutine = null;
                }

                currentPositionOffsetForRightHand = currentGrabable.GrabPositionOffset;
                currentRotationOffsetForRightHand = currentGrabable.GrabRotationOffset;

                rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(true, false));
                leftHandRigLerpCoroutine = StartCoroutine(LerpLeftHandRig(false, false));

                DecideOutlineAndCrosshair();
            }
        }
    }

    private void SetHandAnimBoolsOff()
    {
        anim.SetBool("regularGrab", false);
        anim.SetBool("bottleGrab", false);
        anim.SetBool("trashGrab", false);
        anim.SetBool("knifeGrab", false);
        anim.SetBool("thinBurgerIngredientGrab", false);
        anim.SetBool("regularBurgerIngredientGrab", false);
        anim.SetBool("thickBurgerIngredientGrab", false);
        anim.SetBool("noodleGrab", false);
        anim.SetBool("kettleGrab", false);
        anim.SetBool("chargingThrow", false);
    }
    private void DecideGrabAnimBool()
    {
        if (currentGrabable != null && currentGrabable.IsGrabbed)
        {
            anim.SetTrigger("endThrowInstantly");

            switch (currentGrabable.HandGrabType)
            {
                case PlayerManager.HandGrabTypes.RegularGrab:
                    anim.SetBool("regularGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.BottleGrab:
                    anim.SetBool("bottleGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.TrashGrab:
                    anim.SetBool("trashGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.KnifeGrab:
                    anim.SetBool("knifeGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.ThinBurgerIngredientGrab:
                    anim.SetBool("thinBurgerIngredientGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.RegularBurgerIngredientGrab:
                    anim.SetBool("regularBurgerIngredientGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.ThickBurgerIngredientGrab:
                    anim.SetBool("thickBurgerIngredientGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.NoodleGrab:
                    anim.SetBool("noodleGrab", true);
                    break;

                case PlayerManager.HandGrabTypes.KettleGrab:
                    anim.SetBool("kettleGrab", true);
                    break;
            }
        }
    }

    public void ResetGrabAndInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
        }

        if (currentGrabable != null)
        {
            currentGrabable.OnLoseFocus();
            currentGrabable = null;
        }

        DecideOutlineAndCrosshair();
    }

    public void ResetGrab(IGrabable grabable)
    {
        if (currentGrabable != null)
        {
            if(currentGrabable == grabable)
            {
                currentGrabable.OnLoseFocus();
                currentGrabable = null;
            }
        }

        if (otherGrabable != null)
        {
            if (otherGrabable == grabable)
            {
                otherGrabable.OnLoseFocus();
                otherGrabable = null;
            }
        }

        DecideOutlineAndCrosshair();
    }

    public void ChangeCurrentGrabable(IGrabable grabObject)
    {
        currentGrabable = grabObject;
        currentGrabable.OnGrab(grabPoint);
        DecideGrabAnimBool();
        DecideOutlineAndCrosshair();

        if (rightHandRigLerpCoroutine != null)
        {
            StopCoroutine(rightHandRigLerpCoroutine);
            rightHandRigLerpCoroutine = null;
        }

        currentPositionOffsetForRightHand = currentGrabable.GrabPositionOffset;
        currentRotationOffsetForRightHand = currentGrabable.GrabRotationOffset;

        rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(true, false));
    }

    private void HandleFootsteps()
    {
        if (!characterController.isGrounded || currentInput == Vector2.zero) return;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            FindCurrentGroundMaterial(1.9f);

            if(currentGroundMaterial != null)
            {
                if (currentGroundMaterial.name.Contains("Wood"))
                    PlayFootstep(woodClips);
                else if (currentGroundMaterial.name.Contains("Metal"))
                    PlayFootstep(metalClips);
                else if (currentGroundMaterial.name.Contains("Grass"))
                    PlayFootstep(grassClips);
                else if (currentGroundMaterial.name.Contains("Stone"))
                    PlayFootstep(stoneClips);
                else if (currentGroundMaterial.name.Contains("Tile"))
                    PlayFootstep(tileClips);
                else if (currentGroundMaterial.name.Contains("Gravel"))
                    PlayFootstep(gravelClips);

                footstepTimer = GetCurrentOffset;
            }      
        }
    }

    private void ApplyFinalMovements()
    {
        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        if (willSlideOnSlopes && IsSliding)
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void ChangeCrosshairSize(Vector3 size) => crosshairTransform.localScale = size;

    private void ChangeCrosshairColor(Color color) => crosshairImage.color = color;

    private int GetSubMeshIndex(Mesh mesh, int triangleIndex)
    {
        if (!mesh.isReadable) return 0;

        var triangleCounter = 0;
        for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
        {
            var indexCount = mesh.GetSubMesh(subMeshIndex).indexCount;
            triangleCounter += indexCount / 3;
            if (triangleIndex < triangleCounter) return subMeshIndex;
        }

        return 0;
    }

    private void FindCurrentGroundMaterial(float rayDistance)
    {
        Physics.Raycast(groundTypeCheckRayPoint.transform.position, Vector3.down, out RaycastHit hit, rayDistance, groundTypeLayers);

        if (hit.collider)
        {
            hit.collider.TryGetComponent<MeshRenderer>(out var renderer);

            if (renderer != null)
            {
                var materials = renderer.materials;
                var index = hit.triangleIndex;
                var mesh = hit.transform.GetComponent<MeshFilter>().mesh;
                var subMeshIndex = GetSubMeshIndex(mesh, index);
                currentGroundMaterial = materials[subMeshIndex];
            }
        }
    }

    private void PlayFootstep(AudioClip[] audioClips)
    {
        var audio = audioClips[Random.Range(0, audioClips.Length - 1)];

        if (audio == lastPlayedFootstep)
            PlayFootstep(audioClips);
        else
        {
            footstepAudioSource.PlayOneShot(audio);
            lastPlayedFootstep = audio;
        }
    }

    private void PlayJumpLand(AudioClip[] audioClips, bool isJumping)
    {

        if (audioClips.Length > 2)
        {
            var audio = audioClips[Random.Range(0, audioClips.Length - 1)];

            if (audio == lastPlayedJump || audio == lastPlayedLand)
                PlayJumpLand(audioClips, isJumping);
            else
            {
                jumpLandAudioSource.PlayOneShot(audio);

                if (isJumping)
                    lastPlayedJump = audio;
                else
                    lastPlayedLand = audio;
            }
        }
        else if (audioClips.Length == 2)
        {
            if (isJumping)
            {
                if (audioClips[0] == lastPlayedJump)
                {
                    jumpLandAudioSource.PlayOneShot(audioClips[1]);
                    lastPlayedJump = audioClips[1];
                }
                    
                else
                {
                    jumpLandAudioSource.PlayOneShot(audioClips[0]);
                    lastPlayedJump = audioClips[0];
                }
                    
            }
            else
            {
                if (audioClips[0] == lastPlayedLand)
                {
                    jumpLandAudioSource.PlayOneShot(audioClips[1]);
                    lastPlayedLand = audioClips[1];
                }
                    
                else
                {
                    jumpLandAudioSource.PlayOneShot(audioClips[0]);
                    lastPlayedLand = audioClips[0];
                }
                    
            }
        }
        else
        {
            jumpLandAudioSource.PlayOneShot(audioClips[0]);

            if (isJumping)
                lastPlayedJump = audioClips[0];
            else
                lastPlayedLand = audioClips[0];
        }


    }

    public void TryChangingFocusText(IInteractable interactable, Sprite sprite)
    {
        if (currentInteractable != null && currentInteractable == interactable)
        {
            focusText.sprite = sprite;
        }
    }

    public void TryChangingFocusText(IGrabable grabable, Sprite sprite)
    {
        if (currentGrabable != null && !currentGrabable.IsGrabbed && currentGrabable == grabable)
        {
            focusText.sprite = sprite;
        }
    }

    public void SetAnimBool(string boolName, bool value)
    {
        anim.SetBool(boolName, value);
    }

    public void SetUseHandLerp(Vector3 targetPos, Vector3 targetRot, float timeToDo)
    {
        if (grabbedUseCoroutine != null)
        {
            StopCoroutine(grabbedUseCoroutine);
            grabbedUseCoroutine = null;
        }

        grabbedUseCoroutine = StartCoroutine(UseGrabbed(targetPos, targetRot, timeToDo));
    }

    public IInteractable GetCurrentInteractable() { return currentInteractable; }
    public IGrabable GetCurrentGrabable() { return currentGrabable; }

    private void ResetHandAnim()
    {
        interactChargeTimer = 0f;
        isUsingGrabbedItem = false;

        IsUsingItemX = false;
        IsUsingItemY = false;
    }
        

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            rb.velocity += hit.moveDirection * pushForce;
        }
    }

    private IEnumerator LerpLeftHandRig(bool shouldReach, bool shouldGoBack)
    {
        float startWeight = twoBoneIKConstraintLeftHand.weight;
        float targetWeight = shouldReach ? 1.0f : 0.0f;

        float timeElapsed = 0f;

        while (timeElapsed < lerpTimeForInteractionHand)
        {
            twoBoneIKConstraintLeftHand.weight = Mathf.Lerp(startWeight, targetWeight, timeElapsed/lerpTimeForInteractionHand);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        twoBoneIKConstraintLeftHand.weight = targetWeight;
        leftHandRigLerpCoroutine = null;

        if (shouldGoBack)
        {
            yield return new WaitForSeconds(0.1f);
            if (leftHandRigLerpCoroutine == null)
                leftHandRigLerpCoroutine = StartCoroutine(LerpLeftHandRig(false, false));
        }
    }

    private IEnumerator LerpRightHandRig(bool shouldReach, bool shouldGoBack)
    {
        float startWeight = twoBoneIKConstraintRightHand.weight;
        float targetWeight = shouldReach ? 1.0f : 0.0f;

        float timeElapsed = 0f;

        while (timeElapsed < lerpTimeForInteractionHand)
        {
            twoBoneIKConstraintRightHand.weight = Mathf.Lerp(startWeight, targetWeight, timeElapsed / lerpTimeForInteractionHand);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        twoBoneIKConstraintRightHand.weight = targetWeight;
        rightHandRigLerpCoroutine = null;

        if (shouldGoBack)
        {
            yield return new WaitForSeconds(0.1f);
            if (rightHandRigLerpCoroutine == null)
                rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(false, false));
        }
    }

    private IEnumerator SingleHandThrow()
    {
        float startWeight = twoBoneIKConstraintRightHand.weight;
        currentThrowForce = minThrowForce;
        throwChargeTimer = 0f;
        float value;

        while (throwChargeTimer < throwMaxChargeTime)
        {
            value = throwChargeTimer / throwMaxChargeTime;

            if (value > 0.6f)
                anim.SetBool("throw", true);

            currentThrowForce = Mathf.Lerp(minThrowForce, maxThrowForce, value);

            twoBoneIKConstraintRightHand.weight = Mathf.Lerp(startWeight, 0f, value * 2);

            throwChargeTimer += Time.deltaTime;
            yield return null;
        }

        currentThrowForce = maxThrowForce;

        twoBoneIKConstraintRightHand.weight = 0f;
    }

    private IEnumerator ThrowVisualEffects(bool isCharging)
    {
        float startAmplitude = perlin.m_AmplitudeGain;
        float startFrequency = perlin.m_FrequencyGain;
        float targetAmplitude = isCharging ? maxAmplitudeGain : 0f;
        float targetFrequency = isCharging ? maxFrequencyGain : 0f;

        float startVignette = vignette.intensity.value;
        float targetVignette = isCharging ? 0.25f : 0f;

        float startFOV = firstPersonCam.m_Lens.FieldOfView;
        float targetFOV = isCharging ? maxFOV : normalFOV;

        float t = 0f;
        float v = 0f;

        while (t < throwMaxChargeTime)
        {
            v = isCharging ? (t / throwMaxChargeTime) : (t * 4 / throwMaxChargeTime);

            perlin.m_AmplitudeGain = Mathf.Lerp(startAmplitude, targetAmplitude, v);
            perlin.m_FrequencyGain = Mathf.Lerp(startFrequency, targetFrequency, v);

            vignette.intensity.value = Mathf.Lerp(startVignette, targetVignette, v);

            firstPersonCam.m_Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, v);

            t += Time.deltaTime;
            yield return null;
        }

        perlin.m_AmplitudeGain = targetAmplitude;
        perlin.m_FrequencyGain = targetFrequency;

        vignette.intensity.value = targetVignette;

        firstPersonCam.m_Lens.FieldOfView = targetFOV;

    }

    private IEnumerator UseGrabbed(Vector3 targetPos, Vector3 targetRot, float timeToDo)
    {
        handUseStartOffset = currentPositionOffsetForRightHand;
        handUseDelta = Vector3.zero; // sýfýrla

        Vector3 startPos = currentPositionOffsetForRightHand;
        Vector3 startRot = currentRotationOffsetForRightHand;
        float timeElapsed = 0f;
        float value = 0f;

        while (timeElapsed < timeToDo)
        {
            value = timeElapsed / timeToDo;

            currentPositionOffsetForRightHand = Vector3.Lerp(startPos, targetPos, value);
            currentRotationOffsetForRightHand = Vector3.Lerp(startRot, targetRot, value);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        currentPositionOffsetForRightHand = targetPos;
        currentRotationOffsetForRightHand = targetRot;

        grabbedUseCoroutine = null;
    }


    private IEnumerator CrouchStand(bool shouldCrouch)
    {
        isCrouching = shouldCrouch;

        float horizontalSpeed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude;
        anim.SetBool("isCrouching", isCrouching);

        float currentHeight = characterController.height;
        Vector3 currentCenter = characterController.center;

        float targetHeight = shouldCrouch ? crouchingHeight : standingHeight;
        Vector3 targetCenter = shouldCrouch ? crouchingCenter : standingCenter;

        float timeElapsed = 0f;

        while (timeElapsed < timeToCrouchStand)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed/timeToCrouchStand);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed/timeToCrouchStand);

            timeElapsed += Time.deltaTime;

            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter; 
    }
}
