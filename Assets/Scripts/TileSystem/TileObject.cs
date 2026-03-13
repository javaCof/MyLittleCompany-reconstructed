using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TileObject : MonoBehaviour
{
    [Header("Tile Setting")]
    public Vector2Int size;
    public Vector2Int pos;
    public Transform pivot;

    [Header("Tile DEBUG")]
    public bool drawWorldTile = false;      //월드(TileSystem) 기준 타일 그리기
    public bool drawLocalTile = false;      //로컬(GameObject) 기준 타일 그리기

    [HideInInspector] public bool isFliped = false;

    /// <summary> 오브젝트 위치설정 </summary>
    public void SetTilexy(Vector2Int tilexy)
    {
        Vector2 pivot = GetPivotLocalPos();
        transform.position = TileSystem.TilexyToPos(tilexy) - pivot;
        pos = tilexy;
    }
    /// <summary> 오브젝트 위치설정 (현재 타일좌표) </summary>
    public void SetTilexy() => SetTilexy(pos);
    /// <summary> 오브젝트 위치설정 (현재 월드좌표) </summary>
    public void SetTileHere() => SetTilexy(TileSystem.PosToTilexy(transform.position));
    /// <summary> 오브젝트 타일좌표 설정 </summary>
    public void UpdatePos(Vector2Int tilexy) => pos = tilexy;

    /// <summary> 오브젝트 Flip </summary>
    public virtual void FlipObject()
    {
        Vector3 sc = transform.localScale;
        sc.x = -sc.x;
        transform.localScale = sc;

        size = new Vector2Int(size.y, size.x);
        isFliped = !isFliped;
    }

    /// <summary> pivot 로컬좌표 계산 </summary>
    public Vector2 GetPivotLocalPos()
    {
        return Vector3.Scale(pivot.localPosition, transform.localScale);
    }
    /// <summary> tail 타일좌표 계산 </summary>
    public Vector2Int GetTailTilexy()
    {
        return pos + size - Vector2Int.one;
    }

    private void OnDrawGizmos()
    {
        if (drawWorldTile)
        {
            TileSystem.DrawTile(pos, size, Color.white);
            TileSystem.DrawTile(pos, Vector2Int.one, Color.red);
        }

        if (drawLocalTile && pivot != null)
        {
            Gizmos.color = Color.white;
            for (int i = 0; i < size.x; i++)
            {
                for (int j = 0; j < size.y; j++)
                {
                    DrawLocalTile(i, j);
                }
            }

            Gizmos.color = Color.red;
            DrawLocalTile(0, 0);
        }
    }

    void DrawLocalTile(int tile_x, int tile_y)
    {
        Vector2 tilePos = TileSystem.TilexyToPos(new Vector2Int(tile_x, tile_y), pivot.position);
        Gizmos.DrawLine(tilePos + Vector2.up * TileSystem.tileSize.y / 2, tilePos + Vector2.right * TileSystem.tileSize.x / 2);
        Gizmos.DrawLine(tilePos + Vector2.right * TileSystem.tileSize.x / 2, tilePos - Vector2.up * TileSystem.tileSize.y / 2);
        Gizmos.DrawLine(tilePos - Vector2.up * TileSystem.tileSize.y / 2, tilePos - Vector2.right * TileSystem.tileSize.x / 2);
        Gizmos.DrawLine(tilePos - Vector2.right * TileSystem.tileSize.x / 2, tilePos + Vector2.up * TileSystem.tileSize.y / 2);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TileObject), true)]
[CanEditMultipleObjects]
public class TileObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TileObject obj = (TileObject)target;

        if (GUILayout.Button("Update Position"))
        {
            obj.SetTilexy();
        }
        if (GUILayout.Button("Position Here"))
        {
            obj.SetTileHere();
        }
        if (GUILayout.Button("Flip"))
        {
            obj.FlipObject();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif