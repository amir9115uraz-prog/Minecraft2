using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public ItemData CheckRecipe(BlockType?[,] matrix)
    {
        int tempCount;
        return CheckRecipe(matrix, out tempCount);
    }

    public ItemData CheckRecipe(BlockType?[,] matrix, out int craftCount)
    {
        craftCount = 1;
        if (matrix == null) return null;

        int size = matrix.GetLength(0);
        string[,] stringMatrix = new string[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (matrix[x, y] != null && matrix[x, y] != BlockType.Air)
                {
                    string name = matrix[x, y].ToString().ToLower();
                    if (matrix[x, y] == BlockType.Grass) name = "dirt";
                    if (name.Contains("log") || name.Contains("wood") || name.Contains("tree")) name = "oaklog";
                    stringMatrix[x, y] = name;
                }
                else
                {
                    stringMatrix[x, y] = "";
                }
            }
        }

        return CheckRecipe(stringMatrix, out craftCount);
    }

    public ItemData CheckRecipe(string[,] matrix, out int craftCount)
    {
        craftCount = 1;
        if (matrix == null) return null;

        int size = matrix.GetLength(0);

        if (size == 2)
        {
            int logCount = 0;
            string foundLog = "";
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    if (matrix[x, y].Contains("log") || matrix[x, y].Contains("wood"))
                    {
                        logCount++;
                        foundLog = matrix[x, y];
                    }
                }
            }

            if (logCount == 1)
            {
                if (foundLog.Contains("birch")) return GetItemFromDatabase("BirchPlanks", out craftCount, 4);
                if (foundLog.Contains("spruce")) return GetItemFromDatabase("SprucePlanks", out craftCount, 4);
                if (foundLog.Contains("jungle")) return GetItemFromDatabase("JunglePlanks", out craftCount, 4);
                return GetItemFromDatabase("OakPlanks", out craftCount, 4);
            }

            bool isP00 = IsPlankStr(matrix[0, 0]);
            bool isP10 = IsPlankStr(matrix[1, 0]);
            bool isP01 = IsPlankStr(matrix[0, 1]);
            bool isP11 = IsPlankStr(matrix[1, 1]);

            if (isP00 && isP10 && isP01 && isP11)
            {
                return GetItemFromDatabase("CraftingTable_Block", out craftCount, 1);
            }

            if ((isP00 && isP01 && !isP10 && !isP11) || (isP10 && isP11 && !isP00 && !isP01))
            {
                return GetItemFromDatabase("Stick", out craftCount, 4);
            }
        }
        else if (size == 3)
        {
            bool isDia00 = matrix[0, 0] == "diamond";
            bool isDia10 = matrix[1, 0] == "diamond";
            bool isDia20 = matrix[2, 0] == "diamond";
            bool isStick11 = matrix[1, 1] == "stick";
            bool isStick12 = matrix[1, 2] == "stick";

            if (isDia00 && isDia10 && isDia20 && isStick11 && isStick12 &&
                matrix[0, 1] == "" && matrix[2, 1] == "" && matrix[0, 2] == "" && matrix[2, 2] == "")
            {
                return GetItemFromDatabase("Diamond_Pickaxe", out craftCount, 1);
            }

            bool isDia10_M = matrix[1, 0] == "diamond";
            bool isDia11_M = matrix[1, 1] == "diamond";
            bool isStick12_M = matrix[1, 2] == "stick";

            if (isDia10_M && isDia11_M && isStick12_M &&
                matrix[0, 0] == "" && matrix[2, 0] == "" && matrix[0, 1] == "" && matrix[2, 1] == "" &&
                matrix[0, 2] == "" && matrix[2, 2] == "")
            {
                return GetItemFromDatabase("Diamond_Sword", out craftCount, 1);
            }
        }

        return null;
    }

    public void CraftItem(BlockType?[,] matrix)
    {
        if (matrix == null) return;
        int size = matrix.GetLength(0);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                matrix[x, y] = null;
            }
        }
    }

    public void CraftItem(BlockType?[,] matrix, object secondParam)
    {
        CraftItem(matrix);
    }

    private bool IsPlankStr(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return id.Contains("plank");
    }

    private ItemData GetItemFromDatabase(string idName, out int craftCount, int countValue)
    {
        craftCount = countValue;
        ItemData[] allItems = Resources.LoadAll<ItemData>("");
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i] != null && allItems[i].itemID.ToLower() == idName.ToLower())
            {
                return allItems[i];
            }
        }
        return null;
    }
}
