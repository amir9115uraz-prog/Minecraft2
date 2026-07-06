using UnityEngine;

public static class StructureGenerator
{
    public static void CreateTree(BlockType[,,] map, int localX, int localY, int localZ, BlockType logType, BlockType leavesType, int width, int height)
    {
        if (logType == BlockType.Air) return;

        int treeHeight = Random.Range(4, 7);

        for (int i = 0; i < treeHeight; i++)
        {
            if (localY + i < height)
            {
                map[localX, localY + i, localZ] = logType;
            }
        }

        int leavesLevel = localY + treeHeight - 2;

        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                for (int y = 0; y <= 2; y++)
                {
                    int targetX = localX + x;
                    int targetY = leavesLevel + y;
                    int targetZ = localZ + z;

                    if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height && targetZ >= 0 && targetZ < width)
                    {
                        if (Mathf.Abs(x) == 2 && Mathf.Abs(z) == 2 && y == 2) continue;

                        if (map[targetX, targetY, targetZ] == BlockType.Air)
                        {
                            map[targetX, targetY, targetZ] = leavesType;
                        }
                    }
                }
            }
        }
    }
}
