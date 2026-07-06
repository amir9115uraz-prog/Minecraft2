using UnityEngine;
using Photon.Pun;

public class CameraThirdPerson : MonoBehaviourPun
{
    public enum CameraMode { FirstPerson, ThirdPersonBack, ThirdPersonFront }
    [Header("Текущий режим")]
    public CameraMode currentMode = CameraMode.FirstPerson;

    [Header("Настройки дистанции (Приближение)")]
    public float currentDistance = 3.5f;   
    public float minDistance = 1.5f;       
    public float maxDistance = 7.0f;       
    public float scrollSensitivity = 3f;   
    public float touchZoomSensitivity = 0.05f; 

    [Header("Настройки высоты")]
    public float heightOffset = 0.6f;      

    [Header("Вращение от 3-го лица")]
    public float rotationSpeed = 3f;       
    public float touchRotationSensitivity = 0.2f; 
    
    private float orbitX = 0f;
    private float orbitY = 0f;

    [Header("Ссылки на объекты")]
    public GameObject playerMeshObject; 
    public Transform cameraHolder;      

    private Vector3 firstPersonLocalPos;
    private Quaternion firstPersonLocalRot;

    private MobileCameraInputProcessor touchInputProcessor;
    private CameraOrbitPositioner orbitPositioner;
    private PlayerMeshVisibilityToggle visibilityToggler;

    void Start()
    {
        if (photonView != null && !photonView.IsMine)
        {
            enabled = false;
            return;
        }

        if (cameraHolder == null)
        {
            Camera mainCam = GetComponentInChildren<Camera>();
            if (mainCam != null) cameraHolder = mainCam.transform;
        }

        if (cameraHolder != null)
        {
            firstPersonLocalPos = cameraHolder.localPosition;
            firstPersonLocalRot = cameraHolder.localRotation;
        }

        touchInputProcessor = gameObject.AddComponent<MobileCameraInputProcessor>();
        touchInputProcessor.scrollSensitivity = scrollSensitivity;
        touchInputProcessor.touchZoomSensitivity = touchZoomSensitivity;
        touchInputProcessor.rotationSpeed = rotationSpeed;
        touchInputProcessor.touchRotationSensitivity = touchRotationSensitivity;
        touchInputProcessor.minDistance = minDistance;
        touchInputProcessor.maxDistance = maxDistance;

        orbitPositioner = gameObject.AddComponent<CameraOrbitPositioner>();
        orbitPositioner.cameraHolder = cameraHolder;
        orbitPositioner.heightOffset = heightOffset;
        orbitPositioner.firstPersonLocalPos = firstPersonLocalPos;

        visibilityToggler = gameObject.AddComponent<PlayerMeshVisibilityToggle>();
        visibilityToggler.playerMeshObject = playerMeshObject;
        visibilityToggler.InitializeRenderers();

        ApplyCameraMode();
    }

    void Update()
    {
        if (photonView != null && !photonView.IsMine) return;

        if (Input.GetKeyDown(KeyCode.F5))
        {
            CycleCameraMode();
        }

        if (currentMode != CameraMode.FirstPerson && touchInputProcessor != null)
        {
            touchInputProcessor.ProcessInputs(ref currentDistance, ref orbitX, ref orbitY);
        }
    }

    void LateUpdate()
    {
        if (photonView != null && !photonView.IsMine) return;
        if (cameraHolder == null || currentMode == CameraMode.FirstPerson || orbitPositioner == null) return;

        orbitPositioner.RepositionCamera(currentMode, transform, orbitX, orbitY, currentDistance);
    }

    public void CycleCameraMode()
    {
        if (currentMode == CameraMode.FirstPerson)
        {
            currentMode = CameraMode.ThirdPersonBack;
            orbitX = 0f; 
            orbitY = 0f;
        }
        else if (currentMode == CameraMode.ThirdPersonBack)
        {
            currentMode = CameraMode.ThirdPersonFront;
            orbitX = 0f; 
            orbitY = 0f;
        }
        else
        {
            currentMode = CameraMode.FirstPerson;
        }

        ApplyCameraMode();
    }

    void ApplyCameraMode()
    {
        if (cameraHolder == null) return;

        if (currentMode == CameraMode.FirstPerson)
        {
            cameraHolder.localPosition = firstPersonLocalPos;
            cameraHolder.localRotation = firstPersonLocalRot;
            if (playerMeshObject != null)
            {
                playerMeshObject.transform.localRotation = Quaternion.identity;
                if (visibilityToggler != null) visibilityToggler.ToggleRenderers(false);
            }
        }
        else
        {
            if (visibilityToggler != null) visibilityToggler.ToggleRenderers(true);
        }
    }
}

public class MobileCameraInputProcessor : MonoBehaviour
{
    public float scrollSensitivity;   
    public float touchZoomSensitivity; 
    public float rotationSpeed;       
    public float touchRotationSensitivity;
    public float minDistance;
    public float maxDistance;

    public void ProcessInputs(ref float distance, ref float orbitX, ref float orbitY)
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            distance += deltaMagnitudeDiff * touchZoomSensitivity;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            return; 
        }

        #if !UNITY_ANDROID && !UNITY_IOS
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            distance -= scrollInput * scrollSensitivity;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        orbitX += Input.GetAxis("Mouse X") * rotationSpeed;
        orbitY -= Input.GetAxis("Mouse Y") * rotationSpeed;
        #endif

        int count = Input.touchCount;
        for (int i = 0; i < count; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.position.x > Screen.width / 2f)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    orbitX += touch.deltaPosition.x * touchRotationSensitivity;
                    orbitY -= touch.deltaPosition.y * touchRotationSensitivity;
                }
            }
        }

        orbitY = Mathf.Clamp(orbitY, -50f, 75f); 
    }
}

public class CameraOrbitPositioner : MonoBehaviour
{
    public Transform cameraHolder;
    public float heightOffset;
    public Vector3 firstPersonLocalPos;

    public void RepositionCamera(CameraThirdPerson.CameraMode mode, Transform player, float orbitX, float orbitY, float distance)
    {
        Vector3 targetPivotPoint = player.position + player.up * (firstPersonLocalPos.y + heightOffset);

        if (mode == CameraThirdPerson.CameraMode.ThirdPersonBack)
        {
            Quaternion rotation = Quaternion.Euler(orbitY, player.eulerAngles.y + orbitX, 0f);
            Vector3 targetPosition = targetPivotPoint - (rotation * Vector3.forward * distance);
            cameraHolder.position = targetPosition;
            cameraHolder.rotation = rotation;
        }
        else if (mode == CameraThirdPerson.CameraMode.ThirdPersonFront)
        {
            Quaternion rotation = Quaternion.Euler(orbitY, player.eulerAngles.y + orbitX + 180f, 0f);
            Vector3 targetPosition = targetPivotPoint - (rotation * Vector3.forward * distance);
            cameraHolder.position = targetPosition;
            cameraHolder.rotation = rotation;
        }
    }
}

public class PlayerMeshVisibilityToggle : MonoBehaviour
{
    public GameObject playerMeshObject;
    private Renderer[] cachedRenderers;

    public void InitializeRenderers()
    {
        if (playerMeshObject != null)
        {
            cachedRenderers = playerMeshObject.GetComponentsInChildren<Renderer>(true);
        }
    }

    public void ToggleRenderers(bool visible)
    {
        if (cachedRenderers == null) return;
        int count = cachedRenderers.Length;
        for (int i = 0; i < count; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].enabled = visible;
            }
        }
    }
}
