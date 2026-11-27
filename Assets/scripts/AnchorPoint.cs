using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class AnchorPoint : PhysicsSimulated
{
    Collider2D attachedTo;
    Collider2D myCollider;
    // Filters out objects when detecting collisions
    ContactFilter2D contactFilter;
    HingeJoint2D attachmentJoint;
    Draggable myDraggable;

    protected override void Awake()
    {
        base.Awake();

        myCollider = GetComponent<Collider2D>();
        contactFilter = new ContactFilter2D
        {
            // Do not try to anchor to triggers
            useTriggers = false
        };

        // Get a draggable component if I have one
        if (TryGetComponent<Draggable>(out Draggable draggable))
        {
            myDraggable = draggable;
        }
    }

    public override void StartSimulation()
    {
        base.StartSimulation();
        CheckAttachment();
    }

    // Attach myself to the given collider
    private void Attach(Collider2D toAttachTo)
    {
        Assert.NotNull(toAttachTo);
        attachedTo = toAttachTo;
        Detatch();

        // Make a HingeJoint2D for the attachment
        attachmentJoint = gameObject.AddComponent<HingeJoint2D>();
        attachmentJoint.autoConfigureConnectedAnchor = false;
        attachmentJoint.connectedBody = toAttachTo.attachedRigidbody;
        attachmentJoint.anchor = Vector2.zero;
        attachmentJoint.connectedAnchor = toAttachTo.transform.InverseTransformPoint(body.position);
    }

    // If I am attached to something, detatch
    private void Detatch()
    {
        Destroy(attachmentJoint);
        attachedTo = null;
    }

    // Checks if the anchor is still overlapping with its attachment
    // If not, destroy connection and try to find a new attachment
    private void CheckAttachment()
    {
        bool stillOverlaps = attachedTo != null && attachedTo.OverlapPoint(body.position);
        if (!stillOverlaps)
        {
            Detatch();
            TryAttachAnchor();
        }
    }

    // Attempts to find an overlapping object to attach the anchor to, prioritizing physics simulated obejcts
    private void TryAttachAnchor()
    {
        List<Collider2D> hits = new List<Collider2D>();
        Physics2D.OverlapCollider(myCollider, contactFilter, hits);

        // If no collisions do nothing
        if (hits.Count == 0)
        {
            return;
        }

        // Look for objects to attach to, prioritizing PhysicsSimulateds
        for (int i = 0; i < hits.Count; i++)
        {
            // If the collider is a PhysicsSimulated, choose that one as selected
            if (hits[i].TryGetComponent<PhysicsSimulated>(out PhysicsSimulated otherPhysicsObject))
            {
                Attach(hits[i]);
                return;
            }
        }

        // If no attachments are physics simulated but there are other ones, choose one arbitrarily
        Attach(hits[0]);
    }
}
