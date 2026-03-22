using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base node representing an element in the render order dependency graph.
/// Each node defines a spatial footprint (position + size) on the tile grid,
/// and maintains relationships with other nodes based on tile-space ordering rules.
/// </summary>
public abstract class OrderNode
{
    public int order = 0;       // Final sorting order value applied to renderer
    public int level = 0;       // Depth in dependency graph (used for ordering)
    public int range = 1;       // Spacing between sorting orders to prevent rendering conflicts
    public OrderNode prev = null;
    public List<OrderNode> nexts = new();

    /// <summary> Minimum tile coordinate occupied by this node. </summary>
    public Vector2Int head { get { return GetPos(); } }
    /// <summary> Maximum tile coordinate occupied by this node. </summary>
    public Vector2Int tail { get { return GetPos() + GetSize() - Vector2Int.one; } }

    int orderIdx = 0;

    public void SetOrder() => SetOrder(order);
    public void SetLevel() => SetLevel(level);

    /// <summary> Propagates level (depth) through child nodes. </summary>
    public void SetLevel(int level)
    {
        this.level = level;
        foreach (var next in nexts)
            next.SetLevel(level + 1);
    }
    /// <summary>
    /// Returns a unique order value within the node's range.
    /// This allows multiple renderers within the same node
    /// to avoid z-fighting by slightly offsetting their sorting order.
    /// </summary>
    public int GetRangeOrder()
    {
        if (orderIdx == range) orderIdx = 0;
        return order + orderIdx++;
    }

    public abstract Vector2Int GetPos();
    public abstract Vector2Int GetSize();
    public abstract void SetOrder(int order);
}

/// <summary>
/// A lightweight node representing a single tile position.
/// Used as a dynamic ordering anchor for moving entities,
/// helping maintain correct render order during traversal.
/// Also serves as the root node.
/// </summary>
public class PointOrder : OrderNode
{
    public Vector2Int pos;

    public PointOrder(int x, int y) => pos = new Vector2Int(x, y);

    public override Vector2Int GetPos()
    {
        return pos;
    }
    public override Vector2Int GetSize()
    {
        return Vector2Int.one;
    }
    /// <summary>
    /// Assigns order and propagates it to child nodes, offset by range to maintain separation.
    /// </summary>
    public override void SetOrder(int order)
    {
        this.order = order;
        foreach (var next in nexts)
            next.SetOrder(order + range);
    }
}

/// <summary>
/// Node representing a TileObject with a Renderer.
/// Applies calculated sorting order directly to the renderer,
/// and optionally triggers callbacks when order changes.
/// </summary>
public class ObjectOrder : OrderNode
{
    public UnityAction onOrder = null;

    public TileObject tileObject;
    public Renderer rnd;

    public override Vector2Int GetPos()
    {
        return tileObject.pos;
    }
    public override Vector2Int GetSize()
    {
        return tileObject.size;
    }
    /// <summary>
    /// Applies sorting order to the associated renderer,
    /// then propagates order to child nodes.
    /// Also invokes callback for additional behaviors
    /// dependent on order changes.
    /// </summary>
    public override void SetOrder(int order)
    {
        this.order = order;

        if (tileObject == null) return;

        rnd.sortingOrder = order;

        if (onOrder != null) onOrder.Invoke();

        foreach (var next in nexts)
            next.SetOrder(order + range);
    }
}

/// <summary>
/// Manages a collection of OrderNodes and builds ordering relationships.
/// Nodes are organized based on tile-space spatial relationships,
/// forming a dependency graph that determines relative front/back order.
/// </summary>
public class OrderTree
{
    public OrderNode root;
    public List<OrderNode> nodes = new();

    public OrderTree()
    {
        root = new PointOrder(-1, -1);
        nodes.Add(root);
    }

    /// <summary>
    /// Inserts a node into the order tree based on spatial comparison.
    /// The node is attached to the closest valid parent,
    /// and may reparent existing nodes if necessary to maintain correct ordering.
    ///
    /// After insertion:
    /// - Levels are recalculated
    /// - Orders are reassigned
    /// - Node list is sorted by level
    /// </summary>
    public void AddNode(OrderNode node)
    {
        int pIdx = nodes.FindLastIndex((n) => CompareOrder(node, n) == 1);
        OrderNode pnode = nodes[pIdx];

        node.level = pnode.level + 1;
        node.prev = pnode;
        pnode.nexts.Add(node);

        for (int i = pIdx + 1; i < nodes.Count; i++)
        {
            if (CompareOrder(node, nodes[i]) == -1 && node.level > nodes[i].prev.level)
            {
                OrderNode cnode = nodes[i];
                cnode.prev.nexts.Remove(cnode);
                cnode.prev = node;
                node.nexts.Add(cnode);
            }
        }

        nodes.Add(node);
        pnode.SetLevel();
        pnode.SetOrder();
        nodes.Sort((a, b) => a.level.CompareTo(b.level));
    }

    /// <summary>
    /// Removes a node from the tree and reassigns its children.
    /// Child nodes are reinserted to preserve correct ordering relationships.
    /// </summary>
    public void RemoveNode(OrderNode node)
    {
        if (node == null) return;

        //node is not root
        OrderNode pnode = node.prev;
        pnode.nexts.Remove(node);
        nodes.Remove(node);

        foreach (var n in node.nexts)
        {
            n.prev = null;
            nodes.Remove(n);
            AddNode(n);
        }

        pnode.SetLevel();
        pnode.SetOrder();
        nodes.Sort((a, b) => a.level.CompareTo(b.level));
    }

    /// <summary>
    /// Compares two nodes based on their tile-space bounding boxes.
    ///
    /// Returns:
    ///  1  ¡æ nodeA should be rendered after nodeB
    /// -1  ¡æ nodeA should be rendered before nodeB
    ///  0  ¡æ no strict ordering (overlapping, containment, or ambiguous case)
    /// </summary>
    static int CompareOrder(OrderNode nodeA, OrderNode nodeB)
    {
        if ((nodeA.head.x > nodeB.tail.x || nodeA.head.y > nodeB.tail.y) &&
            (nodeA.tail.x >= nodeB.head.x && nodeA.tail.y >= nodeB.head.y))
            return 1;
        else if ((nodeB.head.x > nodeA.tail.x || nodeB.head.y > nodeA.tail.y) &&
            (nodeB.tail.x >= nodeA.head.x && nodeB.tail.y >= nodeA.head.y))
            return -1;
        else return 0;
    }
}

/// <summary>
/// MonoBehaviour that connects a TileObject to the OrderTree system.
///
/// Responsible for:
/// - Creating and managing an ObjectOrder node
/// - Registering/unregistering the node to the OrderTree
/// - Updating render order when the object's position changes
///
/// Acts as the bridge between Unity components and the ordering system.
/// </summary>
[RequireComponent(typeof(TileObject))]
public class TileOrder : MonoBehaviour
{
    public int orderRange = 1;          // Range used to separate sortingOrder values
    public Renderer otherRenderer;      // Optional renderer override

    public int order { get { return node.order; } }

    public UnityAction onOrder = null;

    ObjectOrder node;
    OrderTree tree = TileOrderSystem.main;

    TileObject tileObject;

    private void Awake()
    {
        tileObject = GetComponent<TileObject>();

        node = new ObjectOrder();
        node.tileObject = tileObject;
        node.rnd = (otherRenderer == null) ? GetComponent<Renderer>() : otherRenderer;
        node.range = orderRange;
        node.onOrder = OnOrder;
    }
    private void Start()
    {
        tree.AddNode(node);
    }
    private void OnDestroy()
    {
        tree.RemoveNode(node);
    }

    public void UpdateOrder()
    {
        tree.RemoveNode(node);
        tree.AddNode(node);
    }
    public int GetRangeOrder()
    {
        return node.GetRangeOrder();
    }

    void OnOrder()
    {
        if (onOrder != null) onOrder.Invoke();
    }
}
