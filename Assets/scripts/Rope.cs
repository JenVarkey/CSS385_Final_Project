using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Rope : Simulated
{
    // Rope ends
    public Rigidbody2D startAnchor;
    public Rigidbody2D endAnchor;

    // Rope properties
    public float maxRopeLength = 8;
    private int segmentCount = 15;
    private int segmentsPerUnit = 4;
    private float sagAmount = 1.5f;
    private float segmentRadius = 0.08f;
    private float segmentMass = 0.2f;
    private float linearDamping = 0.5f;

    // Makes the rope visible
    private LineRenderer lineRenderer;
    // List of rope segments
    private List<Rigidbody2D> segments = new List<Rigidbody2D>();
    private List<HingeJoint2D> joints = new List<HingeJoint2D>();
    private List<Vector2> segmentPositions = new List<Vector2>();
    private bool wasSimulatingLastTime = false;

    protected override void Awake()
    {
        base.Awake();
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        RecalculateRopePosition();
    }

    private void LateUpdate()
    {
        // If just stopped simulating, recalculate the rope position 1 time
        // (cannot be in stopSimulation because must run after the anchors stopSimulation)
        if (!isInSimulation && wasSimulatingLastTime)
        {
            RecalculateRopePosition();
        }
        wasSimulatingLastTime = isInSimulation;

        if (isInSimulation)
        {
            // If simulating, update line renderer based on segments
            lineRenderer.positionCount = segments.Count + 2;
            lineRenderer.SetPosition(0, startAnchor.position);
            for (int i = 0; i < segments.Count; i++)
            {
                lineRenderer.SetPosition(i + 1, segments[i].position);
            }
            lineRenderer.SetPosition(segments.Count + 1, endAnchor.position);

        }
        else
        {
            // Update line renderer based on segment locations
            lineRenderer.positionCount = segmentPositions.Count + 2;
            lineRenderer.SetPosition(0, startAnchor.position);
            for (int i = 0; i < segmentPositions.Count; i++)
            {
                lineRenderer.SetPosition(i + 1, segmentPositions[i]);
            }
            lineRenderer.SetPosition(segmentPositions.Count + 1, endAnchor.position);
        }
    }

    // Sets the ends of the rope
    public void SetAnchors(Rigidbody2D startAnchor_, Rigidbody2D endAnchor_)
    {
        startAnchor = startAnchor_;
        endAnchor = endAnchor_;
    }

    // Set max rope length
    public void SetMaxRopeLength(float length)
    {
        maxRopeLength = length;
    }

    // Create the rope
    public override void StartSimulation()
    {
        base.StartSimulation();

        // Create the physical rope
        Assert.IsTrue(startAnchor != null && endAnchor != null);
        RecalculateRopePosition();
        lineRenderer.positionCount = segmentCount + 2;

        // Loop of generating rope points and connecting each to the previous
        Rigidbody2D previousBody = startAnchor; // Store the rope's previous connection
        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 segmentPos = segmentPositions[i];

            // Create the segment
            GameObject segment = CreateSegment(segmentPos, previousBody, "RopeSegment_" + i, i == 0);
            Rigidbody2D segmentRB = segment.GetComponent<Rigidbody2D>();
            segments.Add(segmentRB);
            previousBody = segmentRB;
        }

        // Add the hinge from the last object to the end
        AddHingeJoint(endAnchor, previousBody, false);
    }

    // Get the points that the rope segments will take
    public void RecalculateRopePosition()
    {
        Assert.IsTrue(startAnchor != null && endAnchor != null);
        ClearRope();
        // Update segment count
        segmentCount = Mathf.Max(1, Mathf.RoundToInt(GetParabolaLength(sagAmount) * segmentsPerUnit));
        for (int i = 0; i < segmentCount; i++)
        {
            // Calculate the segment position
            float t = (float)(i + 1) / (segmentCount + 1); // Number between 0 and 1 for segment index
            Vector2 linearPos = Vector2.Lerp(startAnchor.position, endAnchor.position, t); // Position on the line between points
            float sagOffset = 4f * sagAmount * t * (1f - t); // Y offset in segment position caused by sag
            Vector2 segmentPos = linearPos + Vector2.down * sagOffset; // Segment position
            segmentPositions.Add(segmentPos);
        }
    }

    // Use keyboard input to change the sag in the rope
    public void UpdateRopeOnInput(bool anchorsMoved)
    {
        if (isInSimulation)
        {
            return; // Cannot update rope starting position when simulating
        }

        // Get keyboard input (for sag changing)
        Keyboard keys = Keyboard.current;
        float input = (keys.downArrowKey.isPressed ? 0.05f : 0f) - (keys.upArrowKey.isPressed ? 0.05f : 0f);

        // If dragged to be longer than max length
        if (anchorsMoved && GetParabolaLength(sagAmount) > maxRopeLength)
        {
            float newSagAmount = GetParabolaSag(maxRopeLength) * (sagAmount < 0 ? -1 : 1);
            if (Mathf.Abs(newSagAmount) < Mathf.Abs(sagAmount))
            {
                // Since there is error only actually update if new sag amount is less absolute
                sagAmount = newSagAmount;
            }
        }

        // If rope length is less than max OR sag is decreasing (sagAmount * input <= 0), update sag amount
        // (this is because if sag is decreasing the rope is getting shorter so max doesn't matter)
        float lengthIfChanged = GetParabolaLength(sagAmount + input);
        if (lengthIfChanged <= maxRopeLength || sagAmount * input <= 0)
        {
            sagAmount += input;
        }

        RecalculateRopePosition();
    }

    private GameObject CreateSegment(Vector2 SegmentPos, Rigidbody2D connectTo, string name, bool isFirstSegment)
    {
        // Create the segment
        GameObject segmentObject = new GameObject(name);
        segmentObject.transform.SetParent(transform);
        segmentObject.transform.position = SegmentPos;
        segmentObject.layer = LayerMask.NameToLayer("Standable"); // Allow the player to stand on it

        // Create the segment's rigid body
        Rigidbody2D rb = segmentObject.AddComponent<Rigidbody2D>();
        rb.mass = segmentMass;
        rb.linearDamping = linearDamping;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Add a collider
        CircleCollider2D collider = segmentObject.AddComponent<CircleCollider2D>();
        collider.radius = segmentRadius;

        // Attach the segment to the rope using a joint
        AddHingeJoint(rb, connectTo, isFirstSegment);
        return segmentObject;
    }

    // Make a joint from the given game object to the given rigid body
    private void AddHingeJoint(Rigidbody2D connectFrom, Rigidbody2D connectTo, bool isFirstSegment)
    {
        HingeJoint2D joint = connectFrom.gameObject.AddComponent<HingeJoint2D>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = connectTo;

        // Set connectedAnchor to the exact spawn offset so the rope doesn't snap/jitter on frame 1
        if (isFirstSegment)
        {
            joint.anchor = connectTo.transform.InverseTransformPoint(connectTo.position);
            joint.connectedAnchor = Vector2.zero;
        }
        else
        {
            joint.anchor = Vector2.zero;
            joint.connectedAnchor = connectTo.transform.InverseTransformPoint(connectFrom.position);
        }
        joints.Add(joint);
    }

    // Delete the rope by deleting all rope segments
    public void ClearRope()
    {
        // Clear all rope segments
        foreach (var segment in segments)
        {
            if (segment != null)
            {
                Destroy(segment.gameObject);
            }
        }
        segments.Clear();

        // Clear all rope joints
        foreach (var joint in joints)
        {
            if (joint != null)
            {
                Destroy(joint);
            }
        }
        joints.Clear();

        // Clear all segment positions
        segmentPositions.Clear();
    }

    // Uses complicated math stuff to get the parabola length
    private float GetParabolaLength(float sag)
    {
        Vector2 start = startAnchor.position;
        Vector2 end = endAnchor.position;

        float w = Mathf.Abs(end.x - start.x);
        float dy = end.y - start.y;

        // Straight line fallback if anchors are vertically aligned or sag is ~0
        if (w < 0.0001f || Mathf.Abs(sag) < 0.0001f)
            return Vector2.Distance(start, end);

        // Slopes at start (x=0), midpoint (x=w/2), and end (x=w)
        float mStart = (dy - 4f * sag) / w;
        float mMid = dy / w;
        float mEnd = (dy + 4f * sag) / w;

        // Simpson's 1/3 Rule approximation (within ~99.9% of exact integral)
        float lStart = Mathf.Sqrt(1f + mStart * mStart);
        float lMid = Mathf.Sqrt(1f + mMid * mMid);
        float lEnd = Mathf.Sqrt(1f + mEnd * mEnd);

        return (w / 6f) * (lStart + 4f * lMid + lEnd);
    }

    // Uses super complicated math stuff to get sag given length
    private float GetParabolaSag(float targetLength)
    {
        Vector2 start = startAnchor.position;
        Vector2 end = endAnchor.position;

        float w = Mathf.Abs(end.x - start.x);
        float dy = end.y - start.y;
        float minLengthSq = w * w + dy * dy;

        // Taut fallback (no division by w, so vertical anchors work natively)
        if (targetLength * targetLength <= minLengthSq)
            return 0f;

        float lSq = targetLength * targetLength;

        // Smoothly transition scale from exact small-sag parabola (0.375) to deep-sag V-shape (0.25)
        float t = Mathf.Clamp01((lSq - minLengthSq) / (minLengthSq + 0.0001f));
        float scale = Mathf.Lerp(0.375f, 0.25f, t);

        float sagSq = scale * (lSq - w * w - (dy * dy * w * w) / lSq);

        return Mathf.Sqrt(Mathf.Max(0f, sagSq));
    }
}
