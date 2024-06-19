using System.Collections.Generic;
using UnityEngine;

namespace AdaptiveTanks.Extensions;

public static class AttachNodeUtils
{
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
    public static void MoveTo(this AttachNode node, Vector3 dest)
    {
        var localDelta = dest - node.position;
        if (localDelta == Vector3.zero) return;

        node.position = node.originalPosition = dest;

        var attachedPart = node.attachedPart;
        if (attachedPart == null) return;

        Debug.Log($"`{node.id}` attached to {attachedPart.name}[{attachedPart.persistentId}]");

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

    public static void PushBy(this Part part, Vector3 worldDelta)
    {
        // Gotmachine (private communication): `orgPos` is the part position relative to the root
        // part and is what is relied on generally. `attPos` and `attPos0` are legacy things that
        // aren't used and shouldn't be relied upon.
        part.orgPos = part.transform.position += worldDelta;
    }
}
