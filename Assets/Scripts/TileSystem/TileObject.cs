using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Represents an object placed on a tile-based grid.
/// This class abstracts tile-related properties such as position and size,
/// and provides a unified way to convert between tile space and world space.
/// </summary>

public class TileObject : MonoBehaviour
{
    [Header("Tile Setting")]
    public Vector2Int size;
    public Vector2Int pos;
    public Transform pivot; //Reference point used to align the object to the tile grid

    [Header("Tile DEBUG")]
    public bool drawWorldTile = false;
    public bool drawLocalTile = false;

    [HideInInspector] public bool isFliped = false;

    /// <summary> Sets the object's world position based on the given tile coordinates. </summary>
    public void SetTilexy(Vector2Int tilexy)
    {
        Vector2 pivot = GetPivotLocalPos();
        transform.position = TileSystem.TilexyToPos(tilexy) - pivot;
        pos = tilexy;
    }
    /// <summary> Repositions the object using its current tile coordinates. </summary>
    public void SetTilexy() => SetTilexy(pos);
    /// <summary> 
    /// Updates the object's tile position based on its current world position,
    /// then snaps it back to the grid.
    /// </summary>
    public void SetTileHere() => SetTilexy(TileSystem.PosToTilexy(transform.position));
    /// <summary> Updates the internal tile position without applying any transform changes. </summary>
    public void UpdatePos(Vector2Int tilexy) => pos = tilexy;

    /// <summary> 
    /// Flips the object horizontally by inverting its local scale.
    /// Also swaps tile size (width/height) to maintain correct grid occupancy, 
    /// and updates the flip state.
    /// </summary>
    public virtual void FlipObject()
    {
        Vector3 sc = transform.localScale;
        sc.x = -sc.x;
        transform.localScale = sc;

        size = new Vector2Int(size.y, size.x);
        isFliped = !isFliped;
    }

    /// <summary> Returns the pivot position in local space, adjusted by the object's scale. </summary>
    public Vector2 GetPivotLocalPos()
    {
        return Vector3.Scale(pivot.localPosition, transform.localScale);
    }
    /// <summary> Returns the bottom-right (max) tile coordinate occupied by this object. </summary>
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