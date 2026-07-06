using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class BlockPlacer : MonoBehaviourPun
{
    public float placeDistance = 5f;

    private Inventory playerInventory;
    private int selectedSlot = 0; 

    private HotbarInputController hotbarController;
    private CraftingInteractionHandler craftingHandler;
    private VoxelPlacementCalculator placementCalculator;

    void Start()
    {
        if (photonView != null && !photonView.IsMine) { enabled = false; return; }
        
        playerInventory = GetComponentInParent<Inventory>();
        
        hotbarController = gameObject.AddComponent<HotbarInputController>();
        craftingHandler = gameObject.AddComponent<CraftingInteractionHandler>();
        placementCalculator = gameObject.AddComponent<VoxelPlacementCalculator>();
        
        UpdateSlotSelectionUI(); 
    }

    void Update()
    {
        if (photonView != null && !photonView.IsMine) return;

        if (hotbarController != null)
        {
            selectedSlot = hotbarController.ProcessHotbarSelection(selectedSlot);
            if (hotbarController.HasSlotChanged)
            {
                UpdateSlotSelectionUI();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, placeDistance))
            {
                Chunk chunk = hit.transform.GetComponent<Chunk>();
                if (chunk != null && craftingHandler != null)
                {
                    bool isInteractable = craftingHandler.CheckSpecialBlockInteraction(hit.point, hit.normal);
                    if (isInteractable) return;
                }
            }

            TryPlaceBlock();
        }
    }

    void TryPlaceBlock()
    {
        if (playerInventory == null || placementCalculator == null) return;

        if (selectedSlot >= playerInventory.slots.Count) return;
        Inventory.InventorySlot currentSlot = playerInventory.slots[selectedSlot];
        if (currentSlot == null || currentSlot.item == null || currentSlot.count <= 0) return;

        if (!currentSlot.item.isBlock) return;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, placeDistance))
        {
            Chunk chunk = hit.transform.GetComponent<Chunk>();
            if (chunk != null)
            {
                BlockType typeToPlace = currentSlot.item.blockType;
                bool isPlaced = placementCalculator.CalculateAndPlaceBlock(chunk, hit.point, hit.normal, typeToPlace);

                if (isPlaced)
                {
                    currentSlot.count--;
                    if (currentSlot.count <= 0)
                    {
                        currentSlot.Clear();
                    }

                    playerInventory.UpdateAllUISlots();
                }
            }
        }
    }

    public void TryBreakBlock()
    {
        if (placementCalculator == null) return;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, placeDistance))
        {
            Chunk chunk = hit.transform.GetComponent<Chunk>();
            if (chunk != null)
            {
                placementCalculator.ExecuteBlockDestruction(chunk, hit.point, hit.normal, playerInventory);
            }
        }
    }

    void UpdateSlotSelectionUI()
    {
        MinecraftSlotUI[] uiSlots = FindObjectsByType<MinecraftSlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var uiSlot in uiSlots)
        {
            if (uiSlot == null) continue;
            Outline outline = uiSlot.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = (uiSlot.slotIndex == selectedSlot);
            }
        }
    }
}

public class HotbarInputController : MonoBehaviour
{
    public bool HasSlotChanged { get; private set; }

    public int ProcessHotbarSelection(int currentSlot)
    {
        HasSlotChanged = false;
        int previousSlot = currentSlot;

        for (int i = 0; i < 8; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                currentSlot = i;
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            currentSlot--; 
            if (currentSlot < 0) currentSlot = 7; 
        }
        else if (scroll < 0f)
        {
            currentSlot++; 
            if (currentSlot > 7) currentSlot = 0; 
        }

        if (currentSlot != previousSlot)
        {
            HasSlotChanged = true;
        }

        return currentSlot;
    }
}

public class CraftingInteractionHandler : MonoBehaviour
{
    public bool CheckSpecialBlockInteraction(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (WorldGenerator.Instance == null) return false;

        Vector3 blockPosVector = hitPoint - hitNormal * 0.1f;
        int globalX = Mathf.FloorToInt(blockPosVector.x);
        int globalY = Mathf.FloorToInt(blockPosVector.y);
        int globalZ = Mathf.FloorToInt(blockPosVector.z);

        BlockType clickedBlock = WorldGenerator.Instance.GetBlockType(globalX, globalY, globalZ);

        if (clickedBlock.ToString().Contains("CraftingTable"))
        {
            MinecraftCraftingWindowUI craftingUI = Object.FindAnyObjectByType<MinecraftCraftingWindowUI>(FindObjectsInactive.Include);
            if (craftingUI != null)
            {
                craftingUI.gameObject.SetActive(true);
                Transform grid3x3 = craftingUI.transform.Find("Grid3x3");
                if (grid3x3 != null) grid3x3.gameObject.SetActive(true);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return true;
            }
        }

        return false;
    }
}

public class VoxelPlacementCalculator : MonoBehaviour
{
    public bool CalculateAndPlaceBlock(Chunk chunk, Vector3 hitPoint, Vector3 hitNormal, BlockType typeToPlace)
    {
        if (chunk == null || WorldGenerator.Instance == null) return false;

        Vector3 placePos = hitPoint + hitNormal * 0.45f;

        int bx = Mathf.FloorToInt(placePos.x) - chunk.chunkPosition.x * 16;
        int bz = Mathf.FloorToInt(placePos.z) - chunk.chunkPosition.y * 16;
        int by = Mathf.FloorToInt(placePos.y);

        int maxW = WorldGenerator.Instance.chunkWidth;
        int maxH = WorldGenerator.Instance.chunkHeight;

        if (bx < 0 || bx >= maxW || bz < 0 || bz >= maxW || by < 0 || by >= maxH)
        {
            return false;
        }

        chunk.ModifyBlock(bx, by, bz, typeToPlace);
        return true;
    }

    public void ExecuteBlockDestruction(Chunk chunk, Vector3 hitPoint, Vector3 hitNormal, Inventory playerInventory)
    {
        if (chunk == null || WorldGenerator.Instance == null) return;

        Vector3 breakPos = hitPoint - hitNormal * 0.1f;

        int globalX = Mathf.FloorToInt(breakPos.x);
        int globalY = Mathf.FloorToInt(breakPos.y);
        int globalZ = Mathf.FloorToInt(breakPos.z);

        BlockType blockToBreak = WorldGenerator.Instance.GetBlockType(globalX, globalY, globalZ);
        if (blockToBreak == BlockType.Air || blockToBreak == BlockType.Bedrock) return;

        bool broke = false;

        if (BlockPhysicsManager.Instance != null)
        {
            broke = BlockPhysicsManager.Instance.TryModifyBlockWithPhysics(globalX, globalY, globalZ, BlockType.Air);
        }
        else
        {
            int bx = globalX - chunk.chunkPosition.x * 16;
            int bz = globalZ - chunk.chunkPosition.y * 16;
            chunk.ModifyBlock(bx, globalY, bz, BlockType.Air);
            broke = true;
        }

        if (broke && playerInventory != null)
        {
            string targetID = blockToBreak.ToString();
            Debug.Log($"[BlockPlacer] Успешно сломали блок: '{targetID}'");

            if (blockToBreak == BlockType.Grass) targetID = "Dirt";
            
            string lowerName = targetID.ToLower();
            if (lowerName.Contains("log") || lowerName.Contains("wood") || lowerName.Contains("tree") || blockToBreak == BlockType.OakLog)
            {
                targetID = "OakLog";
            }

            ItemData dropItem = playerInventory.FindItemInCache(targetID);

            if (dropItem != null)
            {
                playerInventory.AddItem(dropItem, 1);
                Debug.Log($"[BlockPlacer] Предмет '{targetID}' добавлен в ячейку инвентаря!");
            }
            else
            {
                Debug.LogWarning($"[BlockPlacer] Внимание: В папке Resources нет файла ItemData с именем '{targetID}'!");
            }
        }
    }
}
