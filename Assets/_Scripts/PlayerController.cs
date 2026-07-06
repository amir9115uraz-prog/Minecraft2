using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviourPun
{
    [Header("Настройки движения")]
    public float moveSpeed = 8f;
    public float crouchSpeed = 3.5f; 
    public float gravity = -18f;
    public float jumpHeight = 2.5f;

    [Header("Настройки камеры")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    [Tooltip("Чувствительность сенсорного экрана на телефоне")]
    public float touchSensitivity = 0.2f; 
    public float upDownRange = 80f;

    [Header("Настройки взаимодействия")]
    public float reachDistance = 5f; 

    private CharacterController characterController;
    private Vector3 moveDirection;
    private float verticalRotation = 0;
    
    private float originalHeight;
    private float crouchHeight = 1.2f;
    private Vector3 cameraOriginalLocalPos;
    private bool isCrouching = false;

    private bool isMobileJumping = false;

    public float VerticalVelocity => moveDirection.y;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController != null) originalHeight = characterController.height;

        if (photonView != null && !photonView.IsMine)
        {
            if (cameraTransform != null && cameraTransform.GetComponent<Camera>() != null)
            {
                cameraTransform.GetComponent<Camera>().enabled = false;
                cameraTransform.GetComponent<AudioListener>().enabled = false;
            }
            enabled = false; 
            return;
        }

        #if !UNITY_ANDROID && !UNITY_IOS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        #endif

        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }
        
        if (cameraTransform != null) cameraOriginalLocalPos = cameraTransform.localPosition;
    }

    void Update()
    {
        if (photonView != null && !photonView.IsMine) return;

        if (characterController == null || !characterController.enabled) return;

        HandleCrouch();
        HandleRotation();
        HandleMovement();
        HandleInteraction(); 
    }

    void HandleCrouch()
    {
        #if UNITY_ANDROID || UNITY_IOS
        isCrouching = false; 
        #else
        if (Input.GetKey(KeyCode.LeftShift))
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }
        #endif

        if (isCrouching == true)
        {
            characterController.height = Mathf.Lerp(characterController.height, crouchHeight, Time.deltaTime * 10f);
            if (cameraTransform != null && IsFirstPersonMode())
            {
                Vector3 targetCamPos = new Vector3(cameraOriginalLocalPos.x, crouchHeight - 0.2f, cameraOriginalLocalPos.z);
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetCamPos, Time.deltaTime * 10f);
            }
        }
        else
        {
            characterController.height = Mathf.Lerp(characterController.height, originalHeight, Time.deltaTime * 10f);
            
            if (cameraTransform != null && IsFirstPersonMode())
            {
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, cameraOriginalLocalPos, Time.deltaTime * 10f);
            }
        }
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        foreach (Touch touch in Input.touches)
        {
            if (touch.position.x > Screen.width / 2)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    mouseX += touch.deltaPosition.x * touchSensitivity;
                    mouseY += touch.deltaPosition.y * touchSensitivity;
                }
            }
        }

        transform.Rotate(0, mouseX, 0);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
        if (cameraTransform != null && IsFirstPersonMode())
        {
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        }
    }

        void HandleMovement()
    {
        if (characterController == null || !characterController.enabled) return;

        float currentY = moveDirection.y;

        float forwardMovement = Input.GetAxis("Vertical") + MinecraftJoystick.InputDirection.y;
        float sideMovement = Input.GetAxis("Horizontal") + MinecraftJoystick.InputDirection.x;

        Vector3 moveInput = new Vector3(sideMovement, 0f, forwardMovement);

        if (characterController.isGrounded)
        {
            CameraThirdPerson camScript = GetComponent<CameraThirdPerson>();
            
            if (!IsFirstPersonMode() && camScript != null && camScript.cameraHolder != null)
            {
                Vector3 camForward = camScript.cameraHolder.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = camScript.cameraHolder.right;
                camRight.y = 0f;
                camRight.Normalize();
                moveDirection = (camForward * forwardMovement) + (camRight * sideMovement);
            }
            else
            {
                moveDirection = (transform.forward * forwardMovement) + (transform.right * sideMovement);
            }
            
            float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
            moveDirection *= currentSpeed;
            
            if (Input.GetButton("Jump") || isMobileJumping)
            {
                currentY = Mathf.Sqrt(jumpHeight * -2f * gravity);
                isMobileJumping = false; 
            }
            else
            {
                currentY = -2f; 
            }
        }
        
        moveDirection.y = currentY + gravity * Time.deltaTime;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(moveDirection * Time.deltaTime);
        }
        if (!IsFirstPersonMode() && moveInput.magnitude > 0.05f)
        {
            CameraThirdPerson camScript = GetComponent<CameraThirdPerson>();
            if (camScript != null && camScript.playerMeshObject != null)
            {
                Vector3 movementDirection = new Vector3(moveDirection.x, 0f, moveDirection.z);
                
                if (movementDirection != Vector3.zero)
                {
                    Quaternion targetMeshRotation = Quaternion.LookRotation(movementDirection);
                    camScript.playerMeshObject.transform.rotation = Quaternion.Slerp(
                        camScript.playerMeshObject.transform.rotation, 
                        targetMeshRotation, 
                        Time.deltaTime * 12f
                    );
                }
            }
        }
    }


    void HandleInteraction()
    {
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(0).position.x > Screen.width / 2))
        {
            if (cameraTransform == null) return;
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, reachDistance))
            {
                Chunk hitChunk = hit.transform.GetComponent<Chunk>();
                if (hitChunk != null)
                {
                    Vector3 point = hit.point - hit.normal * 0.1f;
                    Vector3Int blockPos = new Vector3Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y), Mathf.FloorToInt(point.z));

                    int localX = blockPos.x - (hitChunk.chunkPosition.x * 16);
                    int localZ = blockPos.z - (hitChunk.chunkPosition.y * 16);
                    int localY = blockPos.y;

                    if (WorldGenerator.Instance != null)
                    {
                        BlockType type = WorldGenerator.Instance.GetBlockType(blockPos.x, blockPos.y, blockPos.z);

                        if (type == BlockType.Bedrock || localY == 0)
                        {
                            Debug.Log("[PlayerController] Клик заблокирован! Бедрок ломать нельзя.");
                            return;
                        }
                    }
                }
            }
        }
    }

    private bool IsFirstPersonMode()
    {
        CameraThirdPerson camThirdPerson = GetComponent<CameraThirdPerson>();
        if (camThirdPerson != null)
        {
            return camThirdPerson.currentMode == CameraThirdPerson.CameraMode.FirstPerson;
        }
        return true; 
    }

    public void MobileJump()
    {
        if (characterController != null && characterController.enabled && characterController.isGrounded)
        {
            isMobileJumping = true;
        }
    }
}
