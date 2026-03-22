using UnityEngine;

/// <summary>
/// Core system for managing isometric tile-based spatial logic.
/// Handles coordinate conversion between tile space and world space,
/// enabling consistent object placement and grid-based interactions.
/// </summary>

public class TileSystem : MonoBehaviour
{
    //Size of a single isometric tile (width and height of the diamond shape).
    public static Vector2 tileSize = new Vector2(2.56f, 1.28f);
    //Total size of the tile map in tile units.
    public static Vector2Int mapSize = new Vector2Int(60, 60);
    //World position of the origin tile (0,0).
    public static Vector2 tileZeroPos = new Vector2(0, 23.4f);

    [Header("DEBUG")]
    public bool drawDebug = false;
    public Color drawColor = Color.white;

    /// <summary> Converts tile coordinates to world position using a given pivot. </summary>
    public static Vector2 TilexyToPos(Vector2Int tilexy, Vector2 pivot)
    {
        Vector2 pos;
        pos.x = pivot.x + (tileSize.x / 2) * (tilexy.x - tilexy.y);
        pos.y = pivot.y - (tileSize.y / 2) * (tilexy.x + tilexy.y);

        return pos;
    }
    /// <summary> Converts tile coordinates to world position using a fixed pivot. </summary>
    public static Vector2 TilexyToPos(Vector2Int tilexy) => TilexyToPos(tilexy, tileZeroPos);

    /// <summary>
    /// Converts world position to tile coordinates in an isometric grid.
    /// This method resolves ambiguity caused by diamond-shaped tiles by
    /// determining the correct tile through grid subdivision and boundary checks.
    /// </summary>
    public static Vector2Int PosToTilexy(Vector2 pos)
    {
        Vector2 gridSize = tileSize / 2;

        Vector2 pivot = tileZeroPos + new Vector2(0, gridSize.y);
        Vector2 pb_pos = pivot + new Vector2(pos.x, -pos.y);

        int grid_unit_x = (int)(pb_pos.x / gridSize.x);
        int grid_unit_y = (int)(pb_pos.y / gridSize.y);
        float grid_x = pb_pos.x % gridSize.x;
        float grid_y = pb_pos.y % gridSize.y;

        int tile_unit_x, tile_unit_y;
        if ((grid_unit_x + grid_unit_y) % 2 == 0)
        {
            if (-grid_x * gridSize.y + grid_y * gridSize.x < 0)
            {
                tile_unit_x = grid_unit_x + 1;
                tile_unit_y = grid_unit_y - 1;
            }
            else
            {
                tile_unit_x = grid_unit_x;
                tile_unit_y = grid_unit_y;
            }
        }
        else
        {
            if (grid_x * gridSize.y + grid_y * gridSize.x < gridSize.x * gridSize.y)
            {
                tile_unit_x = grid_unit_x;
                tile_unit_y = grid_unit_y - 1;
            }
            else
            {
                tile_unit_x = grid_unit_x + 1;
                tile_unit_y = grid_unit_y;
            }
        }

        Vector2Int tilexy = Vector2Int.zero;
        tilexy.x = (tile_unit_x + tile_unit_y) / 2;
        tilexy.y = (tile_unit_y - tile_unit_x) / 2;

        return tilexy;
    }

    /// <summary> Checks whether a tile area is within map bounds. </summary>
    public static bool IsTilesInMap(int tile_x, int tile_y, int tile_w, int tile_h)
    {
        return tile_x >= 0 && tile_x + tile_w <= mapSize.x && tile_y >= 0 && tile_y + tile_h <= mapSize.y;
    }

    public static void DrawTile(Vector2Int pos, Vector2Int size, Color col)
    {
        Gizmos.color = col;
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                DrawOneTile(pos.x + i, pos.y + j);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (drawDebug)
        {
            DrawTile(Vector2Int.zero, mapSize, drawColor);
        }
    }
    static void DrawOneTile(int tile_x, int tile_y)
    {
        Vector2 tilePos = TilexyToPos(new Vector2Int(tile_x, tile_y), tileZeroPos);
        Gizmos.DrawLine(tilePos + Vector2.up * tileSize.y / 2, tilePos + Vector2.right * tileSize.x / 2);
        Gizmos.DrawLine(tilePos + Vector2.right * tileSize.x / 2, tilePos - Vector2.up * tileSize.y / 2);
        Gizmos.DrawLine(tilePos - Vector2.up * tileSize.y / 2, tilePos - Vector2.right * tileSize.x / 2);
        Gizmos.DrawLine(tilePos - Vector2.right * tileSize.x / 2, tilePos + Vector2.up * tileSize.y / 2);
    }
}
