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
    private Vector3 defaultCrosshairSize;
    private IInteractable currentInteractable;

    [Header("Grab Parameters")]
    [SerializeField] private LayerMask grabableLayers;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color grabCrosshairColor;
    [SerializeField] private float maxThrowForce = 1.3f;
    [SerializeField] private float minThrowForce = 0.3f;
    [SerializeField] private float throwMaxChargeTime = 1.5f;
    [SerializeField] private float quickTapThreshold = 0.2f;
    private float throwChargeTimer = 0f;
    private float currentThrowForce = 0f;
    private Color defaultCrosshairColor;
    private IGrabable currentGrabable;

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
    [SerializeField] private Vector3 positionOffsetForSingleHandGrab;
    [SerializeField] private Vector3 rotationOffsetForSingleHandGrab;
    [SerializeField] private Vector3 positionOffsetForSingleHandGrabRotated;
    [SerializeField] private Vector3 rotationOffsetForSingleHandGrabRotated;


    [SerializeField] private float handPoseScrollSpeed = 0.5f; // how fast scroll adjusts the lerp
    private float handPoseLerpValue = 0f;
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

    private float mouseX, mouseY;
    private float xRotation;

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

        CameraManager.Instance.SwitchToCamera(firstPersonCam);
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

                if (currentGrabable == null || !currentGrabable.IsGrabbed)
                {
                    HandleGrabCheck();
                }
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

        }
                
    }

    private void LateUpdate()
    {
        

        if (canUseHeadbob)
            HandleHeadbob();

        if (CanMove)
        {

            HandleMouseLook();



            HandleHandPoseScrollLerp();
            HandleHandTargetPositions();

        }
        
    }

    private void HandleHandPoseScrollLerp()
    {
        if (currentGrabable != null && currentGrabable.IsGrabbed)
        {
            float scroll = -Input.GetAxis("Mouse ScrollWheel");

            if (scroll != 0f)
            {
                handPoseLerpValue += scroll * handPoseScrollSpeed;
                handPoseLerpValue = Mathf.Clamp01(handPoseLerpValue); // clamp between 0 and 1
            }

            currentPositionOffsetForRightHand = Vector3.Lerp(positionOffsetForSingleHandGrab, positionOffsetForSingleHandGrabRotated, handPoseLerpValue);
            currentRotationOffsetForRightHand = Vector3.Lerp(rotationOffsetForSingleHandGrab, rotationOffsetForSingleHandGrabRotated, handPoseLerpValue);

            currentGrabable.HandLerp = handPoseLerpValue;
        }
    }

    private void HandleHandTargetPositions()
    {
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

    private void HandleMouseLook()
    {
        // Vertical look (Pitch) – apply only to the head bone
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        headBone.localRotation = Quaternion.Euler(rotationX, 0, 0);  // Only up/down
        cameraFollow.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // Horizontal look (Yaw) – rotate the whole player
        float mouseX = Input.GetAxis("Mouse X") * lookSpeedX;
        transform.Rotate(Vector3.up * mouseX);  // Full body turns left/right

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

    private void HandleInteractionCheck()
    {
        if (Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayers))
        {
            if (hit.collider)
            {
                if ((hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable") || hit.collider.gameObject.layer == LayerMask.NameToLayer("InteractableOutlined")) && (currentGrabable == null || !currentGrabable.IsGrabbed || hit.collider.gameObject.GetComponent<IInteractable>().HandRigType != GameManager.HandRigTypes.SingleHandGrab))
                {

                    if (currentInteractable == null)
                    {
                        currentInteractable = hit.collider.gameObject.GetComponent<IInteractable>();
                        ChangeCrosshairSize(interactableCrosshairSize);
                        currentInteractable.OnFocus();
                    }
                    else if (currentInteractable != hit.collider.gameObject.GetComponent<IInteractable>())
                    {
                        currentInteractable.OnLoseFocus();
                        currentInteractable = hit.collider.gameObject.GetComponent<IInteractable>();
                        currentInteractable.OnFocus();
                    }
                }
                else if (currentInteractable != null)
                {
                    currentInteractable.OnLoseFocus();
                    currentInteractable = null;
                    ChangeCrosshairSize(defaultCrosshairSize);
                }
            }
            else if (currentInteractable != null)
            {
                currentInteractable.OnLoseFocus();
                currentInteractable = null;
                ChangeCrosshairSize(defaultCrosshairSize);
            }
        }
        else if (currentInteractable != null)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
            ChangeCrosshairSize(defaultCrosshairSize);
        }
    }

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (currentInteractable != null && Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayers))
            {
                if (hit.collider.gameObject.GetComponent<IInteractable>() == currentInteractable)
                {
                    if (currentInteractable.HandRigType == GameManager.HandRigTypes.Talk)
                    {
                        if (rightHandRigLerpCoroutine != null) StopCoroutine(rightHandRigLerpCoroutine);
                        if (leftHandRigLerpCoroutine != null) StopCoroutine(leftHandRigLerpCoroutine);

                        anim.SetTrigger("talk");
                    }
                    else if (currentGrabable == null || !currentGrabable.IsGrabbed)
                    {
                        if (rightHandRigLerpCoroutine != null) StopCoroutine(rightHandRigLerpCoroutine);

                        if (currentInteractable.HandRigType == GameManager.HandRigTypes.Interaction)
                        {
                            currentPositionOffsetForRightHand = positionOffsetForRightHandInteraction;
                            currentRotationOffsetForRightHand = rotationOffsetForRightHandInteraction;
                        }

                        rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(true, true));
                    }
                    else
                    {
                        if (leftHandRigLerpCoroutine != null) StopCoroutine(leftHandRigLerpCoroutine);

                        if (currentInteractable.HandRigType == GameManager.HandRigTypes.Interaction)
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
    }

    private void HandleGrabCheck()
    {

        if (Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, grabableLayers))
        {
            if (hit.collider)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Grabable") || hit.collider.gameObject.layer == LayerMask.NameToLayer("GrabableOutlined"))
                {
                    if (currentGrabable == null)
                    {
                        currentGrabable = hit.collider.gameObject.GetComponent<IGrabable>();
                        ChangeCrosshairColor(grabCrosshairColor);
                        currentGrabable.OnFocus();
                    }
                    else if (currentGrabable != hit.collider.gameObject.GetComponent<IGrabable>())
                    {
                        currentGrabable.OnLoseFocus();
                        currentGrabable = hit.collider.gameObject.GetComponent<IGrabable>();
                        currentGrabable.OnFocus();
                    }
                }
                else if (currentGrabable != null)
                {
                    currentGrabable.OnLoseFocus();
                    currentGrabable = null;
                    ChangeCrosshairColor(defaultCrosshairColor);
                }

            }
            else if (currentGrabable != null)
            {
                currentGrabable.OnLoseFocus();
                currentGrabable = null;
                ChangeCrosshairColor(defaultCrosshairColor);
            }
        }
        else if (currentGrabable != null)
        {
            currentGrabable.OnLoseFocus();
            currentGrabable = null;
            ChangeCrosshairColor(defaultCrosshairColor);
        }
    }

    private void HandleGrabInput()
    {
        if (currentGrabable != null && currentGrabable.IsGrabbed)
        {
            if (Input.GetKeyDown(throwKey)) // right click starts
            {

                if (throwVisualEffectsCoroutine != null)
                {
                    StopCoroutine(throwVisualEffectsCoroutine);
                    throwVisualEffectsCoroutine = null;
                }

                throwVisualEffectsCoroutine = StartCoroutine(ThrowVisualEffects(true));

                anim.SetBool("chargingThrowRight", true);

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

                anim.SetBool("grabbingRight", false);
                anim.SetBool("chargingThrowRight", false);

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

                if (throwChargeTimer <= quickTapThreshold)
                {
                    currentGrabable.OnDrop((mainCamera.transform.forward * 10 + mainCamera.transform.up * 6 + -mainCamera.transform.right * 3).normalized, isCrouching ? minThrowForce * 0.5f : IsSprinting ? minThrowForce * 1.5f : minThrowForce);
                }
                else
                {
                    currentGrabable.OnThrow((mainCamera.transform.forward * 10 + -mainCamera.transform.right * maxThrowForce / (currentThrowForce * 2)).normalized, isCrouching ? currentThrowForce * 0.5f : IsSprinting ? currentThrowForce * 1.5f : currentThrowForce);
                }
            }
        }
        else if (Input.GetKeyDown(interactKey) && currentGrabable != null && Physics.Raycast(mainCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, grabableLayers))
        {
            if (hit.collider.gameObject.GetComponent<IGrabable>() == currentGrabable)
            {
                anim.SetBool("grabbingRight", true);

                currentGrabable.OnGrab(grabPoint);

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

                // her yeni grab iþleminde sýfýrla
                handPoseLerpValue = 0f;

                currentPositionOffsetForRightHand = positionOffsetForSingleHandGrab;
                currentRotationOffsetForRightHand = rotationOffsetForSingleHandGrab;

                rightHandRigLerpCoroutine = StartCoroutine(LerpRightHandRig(true, false));
                leftHandRigLerpCoroutine = StartCoroutine(LerpLeftHandRig(false, false));
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

        ChangeCrosshairSize(defaultCrosshairSize);
        ChangeCrosshairColor(defaultCrosshairColor);
    }

    public void ResetGrab(IGrabable grabable)
    {
        if (currentGrabable != null)
        {
            if(currentGrabable == grabable)
            {
                currentGrabable.OnLoseFocus();
                currentGrabable = null;
                ChangeCrosshairColor(defaultCrosshairColor);
            }
        }
        else
        {
            ChangeCrosshairColor(defaultCrosshairColor);
        }

        if (currentInteractable != null)
            ChangeCrosshairSize(interactableCrosshairSize);
        else
            ChangeCrosshairSize(defaultCrosshairSize);

        
    }

    public void ChangeCurrentGrabable(IGrabable grabObject)
    {
        anim.SetBool("grabbingRight", true);

        currentGrabable = grabObject;
        currentGrabable.OnGrab(grabPoint);
        ChangeCrosshairColor(grabCrosshairColor);

        if (rightHandRigLerpCoroutine != null)
        {
            StopCoroutine(rightHandRigLerpCoroutine);
            rightHandRigLerpCoroutine = null;
        }

        currentPositionOffsetForRightHand = positionOffsetForSingleHandGrab;
        currentRotationOffsetForRightHand = rotationOffsetForSingleHandGrab;

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
                anim.SetTrigger("throw");

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
