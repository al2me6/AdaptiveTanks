using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveTanks.Utils;

public static class AttachNodeUtils
{
    public static AttachNode New(AttachNode.NodeType type, string id, Vector3 posOrient, Part owner)
    {
        var node = new AttachNode
        {
            id = id,
            nodeType = type,
            attachMethod = type == AttachNode.NodeType.Surface
                ? AttachNodeMethod.HINGE_JOINT
                : AttachNodeMethod.FIXED_JOINT,
            owner = owner
        };
        node.SetPosition(posOrient);
        node.SetOrientation(posOrient);
        return node;
    }

    public static void SetPosition(this AttachNode node, Vector3 position) =>
        node.position = node.originalPosition = position;

    public static void SetOrientation(this AttachNode node, Vector3 orientation) =>
        node.orientation = node.originalOrientation = orientation;

    /// <summary>
    /// Move this node, and any attached parts, to the specified position.
    /// This method will keep the transform of the part fixed in world space, if it is itself
    /// stack-attached.
    /// </summary>
    public static void MoveTo(this AttachNode node, Vector3 dest, bool pushParts)
    {
        var localDelta = dest - node.position;
        if (localDelta.sqrMagnitude < node.nodeDistEpsilon) return;

        node.SetPosition(dest);

        if (!pushParts || node.attachedPart == null) return;

        // Debug.Log($"`{node.id}` attached to {attachedPart.name}[{attachedPart.persistentId}]");

        var part = node.owner;
        var worldDelta = part.transform.TransformDirection(localDelta);

        var attachedPartIsChild = node.attachedPart.parent == part;
        // If the attached part is the child, move the child. Done.
        if (attachedPartIsChild)
        {
            node.attachedPart.PushBy(worldDelta);
        }
        // Else, we are the child. We must push ourselves back relative to the root.
        else
        {
            part.PushBy(-worldDelta);
            // To stay in the same place, we must then shift the entire ship back.
            // But this only makes sense to do if we are stack-attached.
            if (node.nodeType == AttachNode.NodeType.Stack)
            {
                // N.B.: each symmetry-attached copy will be trying to do this. Do our share only.
                part.localRoot.transform.position +=
                    worldDelta / (part.symmetryCounterparts.Count + 1);
            }
        }
    }

    public static void Show(this AttachNode node)
    {
        node.nodeType = AttachNode.NodeType.Stack;
        node.radius = 0.4f;
    }

    // See the stock `ModuleDynamicNodes`.
    public static void Hide(this AttachNode node)
    {
        node.nodeType = AttachNode.NodeType.Dock;
        node.radius = 0.0001f;

        if (node.attachedPart != null)
        {
            const float warnDuration = 5f;
            ScreenMessages.PostScreenMessage(
                $"[{node.owner.partInfo.title}]: part {node.attachedPart.partInfo.title} is attached to a node that has been disabled!",
                warnDuration,
                ScreenMessageStyle.UPPER_RIGHT,
                XKCDColors.Orange);
            node.attachedPart.SetHighlightType(Part.HighlightType.AlwaysOn);
            node.attachedPart.SetHighlightColor(XKCDColors.Orange);
            node.attachedPart.StartCoroutine(node.attachedPart.ResetHighlightDelayed(warnDuration));
        }
    }

    public static bool IsHidden(this AttachNode node) => node.radius < 0.01f;

    public static void SetVisibility(this AttachNode node, bool visible)
    {
        if (visible) node.Show();
        else node.Hide();
    }

    public static IEnumerable<Part> IterSurfaceAttachedChildren(this Part part)
    {
        foreach (var child in part.children)
        {
            if (child.FindAttachNodeByPart(part) is { nodeType: AttachNode.NodeType.Surface })
                yield return child;
        }
    }
}
