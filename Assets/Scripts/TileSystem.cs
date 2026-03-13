using UnityEngine;

/*
 * 마름모 형태의 타일구조에서 중앙상단의 원점을 기준으로
 * 오른쪽이 x축, 왼쪽을 y축으로 정의
 */

public class TileSystem : MonoBehaviour
{
    //타일 크기 (마름모의 가로 대각선, 세로 대각선)
    public static Vector2 tileSize = new Vector2(2.56f, 1.28f);
    //맵 크기 (타일 수)
    public static Vector2Int mapSize = new Vector2Int(60, 60);
    //(0, 0) 타일 위치
    public static Vector2 tileZeroPos = new Vector2(0, 23.4f);

    //디버그 표시
    [Header("DEBUG")]
    public bool drawDebug = false;
    public Color drawColor = Color.white;

    /// <summary> 타일좌표를 월드좌표로 변환 (pivot 기준) </summary>
    public static Vector2 TilexyToPos(Vector2Int tilexy, Vector2 pivot)
    {
        Vector2 pos;
        pos.x = pivot.x + (tileSize.x / 2) * (tilexy.x - tilexy.y);
        pos.y = pivot.y - (tileSize.y / 2) * (tilexy.x + tilexy.y);

        return pos;
    }
    /// <summary> 타일좌표를 월드좌표로 변환 (원점타일 기준) </summary>
    public static Vector2 TilexyToPos(Vector2Int tilexy) => TilexyToPos(tilexy, tileZeroPos);

    /// <summary> 월드좌표를 타일좌표로 변환 </summary>
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

    /// <summary> 타일의 맵 포함 여부 </summary>
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
