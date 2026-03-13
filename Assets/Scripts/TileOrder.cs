using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class OrderNode
{
    public int order = 0;
    public int level = 0;
    public int range = 1;
    public OrderNode prev = null;
    public List<OrderNode> nexts = new();

    public Vector2Int head { get { return GetPos(); } }
    public Vector2Int tail { get { return GetPos() + GetSize() - Vector2Int.one; } }

    int orderIdx = 0;

    public void SetOrder() => SetOrder(order);
    public void SetLevel() => SetLevel(level);

    public void SetLevel(int level)
    {
        this.level = level;
        foreach (var next in nexts)
            next.SetLevel(level + 1);
    }
    public int GetRangeOrder()
    {
        if (orderIdx == range) orderIdx = 0;
        return order + orderIdx++;
    }

    public abstract Vector2Int GetPos();
    public abstract Vector2Int GetSize();
    public abstract void SetOrder(int order);
}
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
    public override void SetOrder(int order)
    {
        this.order = order;
        foreach (var next in nexts)
            next.SetOrder(order + range);
    }
}
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
public class OrderTree
{
    public OrderNode root;
    public List<OrderNode> nodes = new();

    public OrderTree()
    {
        root = new PointOrder(-1, -1);
        nodes.Add(root);
    }

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

[RequireComponent(typeof(TileObject))]
public class TileOrder : MonoBehaviour
{
    public int orderRange = 1;
    public Renderer otherRenderer;

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
