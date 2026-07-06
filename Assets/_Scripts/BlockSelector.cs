using UnityEngine;

public class BlockSelector : MonoBehaviour
{
    public Camera playerCamera;            
    public float maxDistance = 5f;          
    public GameObject highlightPrefab;     

    private VoxelRaycastCalculator raycastCalculator;
    private SelectionHighlightVisualizer highlightVisualizer;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        raycastCalculator = gameObject.AddComponent<VoxelRaycastCalculator>();
        raycastCalculator.playerCamera = playerCamera;
        raycastCalculator.maxDistance = maxDistance;

        highlightVisualizer = gameObject.AddComponent<SelectionHighlightVisualizer>();
        highlightVisualizer.highlightPrefab = highlightPrefab;
        highlightVisualizer.InitializeVisualizer();
    }

    void Update()
    {
        Vector3 targetWorldPosition;
        if (raycastCalculator.TryFindTargetBlock(out targetWorldPosition))
        {
            highlightVisualizer.UpdateHighlightPosition(targetWorldPosition);
        }
        else
        {
            highlightVisualizer.HideHighlight();
        }
    }
}

public class VoxelRaycastCalculator : MonoBehaviour
{
    public Camera playerCamera;
    public float maxDistance;
    private RaycastHit hit;

    public bool TryFindTargetBlock(out Vector3 worldBlockPos)
    {
        worldBlockPos = Vector3.zero;

        // ПРЯМОЙ СТРЕМИТЕЛЬНЫЙ ЛУЧ: убраны все лишние условия
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            // Математика вычисления центра вокселя без вызова GetBlockType
            Vector3 point = hit.point - hit.normal * 0.5f;
            worldBlockPos.x = Mathf.FloorToInt(point.x) + 0.5f;
            worldBlockPos.y = Mathf.FloorToInt(point.y) + 0.5f;
            worldBlockPos.z = Mathf.FloorToInt(point.z) + 0.5f;
            return true;
        }

        return false;
    }
}

public class SelectionHighlightVisualizer : MonoBehaviour
{
    public GameObject highlightPrefab;     
    private GameObject currentHighlight;

    public void InitializeVisualizer()
    {
        if (highlightPrefab != null)
        {
            currentHighlight = Instantiate(highlightPrefab);
            currentHighlight.name = "AutoBlockHighlightBox";
            currentHighlight.SetActive(false);
        }
    }

    public void UpdateHighlightPosition(Vector3 targetPosition)
    {
        currentHighlight.transform.position = targetPosition;
        if (!currentHighlight.activeSelf)
        {
            currentHighlight.SetActive(true);
        }
    }

    public void HideHighlight()
    {
        if (currentHighlight.activeSelf)
        {
            currentHighlight.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (currentHighlight != null)
        {
            Destroy(currentHighlight);
        }
    }
}
