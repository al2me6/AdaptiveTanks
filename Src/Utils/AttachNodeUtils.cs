using System;
using UnityEngine;

namespace AdaptiveTanks;

public static class TransformNodeUtils
{
    public const string SurfaceAttachNodeId = "srfAttach";

    public static AttachNode AddStackAttachNode(this Part part, string id, Vector3 position,
        Vector3 orientation, int size = 1)
    {
        var node = new AttachNode
        {
            id = id,
            owner = part,
            nodeType = AttachNode.NodeType.Stack,
            attachMethod = AttachNodeMethod.FIXED_JOINT,
            position = position,
            originalPosition = position,
            orientation = orientation,
            originalOrientation = orientation,
            size = size
        };
        part.attachNodes.Add(node);
        return node;
    }

    public static void AddSurfaceAttachNode(this Part part, Vector3 position, Vector3 orientation)
    {
        var node = new AttachNode
        {
            id = SurfaceAttachNodeId,
            owner = part,
            nodeType = AttachNode.NodeType.Stack,
            attachMethod = AttachNodeMethod.HINGE_JOINT,
            position = position,
            originalPosition = position,
            orientation = orientation,
            originalOrientation = orientation,
            size = 1
        };
        part.srfAttachNode = node;
    }

    public static void MoveTo(this AttachNode node, Vector3 dest)
    {
        if (node.nodeType != AttachNode.NodeType.Stack)
            throw new ArgumentException("the node must be a stack node", nameof(node));

        var localDelta = dest - node.position;
        node.position = node.originalPosition = dest;

        if (node.attachedPart == null) return;

        var part = node.owner;
        var worldDelta = part.transform.TransformDirection(localDelta);

        // If the attached part is the child, move the child. Otherwise, we are the child and
        // move ourselves.
        var attachedPartIsChild = node.attachedPart.parent == part;
        var translateTarget = attachedPartIsChild ? node.attachedPart : part;
        // If we move ourselves, then we get pushed back.
        var translationDirection = attachedPartIsChild ? 1 : -1;

        // Gotmachine (private communication): `orgPos` is the part position relative to the root
        // part and is what is relied on generally. `attPos` and `attPos0` are legacy things that
        // aren't used and shouldn't be relied upon.
        translateTarget.orgPos =
            translateTarget.transform.position += worldDelta * translationDirection;
    }
}
