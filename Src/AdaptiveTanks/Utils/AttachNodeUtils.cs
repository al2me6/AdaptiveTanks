﻿using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveTanks.Utils;

public static class AttachNodeUtils
{
    public static AttachNode New(
        AttachNode.NodeType type, string id, Vector3 posOrient, Part owner) => new()
    {
        id = id,
        nodeType = type,
        attachMethod = type == AttachNode.NodeType.Surface
            ? AttachNodeMethod.HINGE_JOINT
            : AttachNodeMethod.FIXED_JOINT,
        owner = owner,
        position = posOrient,
        originalPosition = posOrient,
        orientation = posOrient,
        originalOrientation = posOrient
    };

    public static IEnumerable<Part> IterSurfaceAttachedChildren(this Part part)
    {
        foreach (var child in part.children)
        {
            if (child.FindAttachNodeByPart(part) is { nodeType: AttachNode.NodeType.Surface })
                yield return child;
        }
    }

    /// <summary>
    /// Move this node, and any attached parts, to the specified position.
    /// This method will keep the transform of the part fixed in world space, if it is itself
    /// stack-attached.
    /// </summary>
    public static void MoveTo(this AttachNode node, Vector3 dest, bool pushParts)
    {
        var localDelta = dest - node.position;
        if (localDelta == Vector3.zero) return;

        node.position = node.originalPosition = dest;

        if (!pushParts) return;

        var attachedPart = node.attachedPart;
        if (attachedPart == null) return;

        // Debug.Log($"`{node.id}` attached to {attachedPart.name}[{attachedPart.persistentId}]");

        var part = node.owner;
        var worldDelta = part.transform.TransformDirection(localDelta);

        var attachedPartIsChild = attachedPart.parent == part;
        // If the attached part is the child, move the child. Done.
        if (attachedPartIsChild)
        {
            attachedPart.PushBy(worldDelta);
        }
        // Else, we are the child. We must push ourselves back relative to the root.
        else
        {
            part.PushBy(-worldDelta);
            // To stay in the same place, we must then shift the entire ship back.
            // But this only makes sense to do if we are stack-attached.
            if (node.nodeType == AttachNode.NodeType.Stack)
            {
                // Caveat: each symmetry-attached copy will be trying to do this. Do our share only.
                part.localRoot.transform.position +=
                    worldDelta / (part.symmetryCounterparts.Count + 1);
            }
        }
    }
}
