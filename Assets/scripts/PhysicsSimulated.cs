// For objects that move in the level simulation

using UnityEngine;

public class PhysicsSimulated : Simulated
{
    protected Rigidbody2D body;
    protected Vector2 startPosition;
    protected Quaternion startRotation;
    public bool freezeRotation;

    protected override void Awake()
    {
        base.Awake();

        // get object components and starting position
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        startPosition = transform.position;
        body.freezeRotation = freezeRotation;
    }

    // Save initial object state and start simulating physics
    public override void StartSimulation()
    {
        base.StartSimulation();

        // set object physics to moveable
        startPosition = transform.position;
        startRotation = transform.rotation;
        body.bodyType = RigidbodyType2D.Dynamic;
    }

    // Stop simulating physics and return to saved object state
    public override void StopSimulation()
    {
        base.StopSimulation();

        // reset object physics, position, and rotation
        body.bodyType = RigidbodyType2D.Kinematic;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.position = startPosition;
        body.rotation = startRotation.eulerAngles.z;
        transform.position = startPosition;
        transform.rotation = startRotation;
    }

    public Rigidbody2D GetRigidbody()
    {
        return body;
    }
}
