// Lets players drag around objects in the level
using UnityEngine;
using UnityEngine.InputSystem;

public class Draggable : MonoBehaviour
{
    protected Rigidbody2D rigidbody;
    SpriteRenderer spriteRenderer;
    private Color defaultColor;
    public Color selectColor;
    protected static Draggable currentlySelected; // One selected draggable at a time
    private static int lastFrameSetSelected = -1;
    Vector3 clickOffset; // Offset of mouse click and object coordinates
    Vector3 spotToMoveNext; // Stores the location to move to
    bool shouldMoveRigidBody;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();

        // A draggable should never be static (for performence)
        if (rigidbody.bodyType == RigidbodyType2D.Static)
        {
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
        }

        spotToMoveNext = rigidbody.transform.position;
        shouldMoveRigidBody = false;

        // Set sprite renderer and color
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        // do nothing if simulation is running
        if (LevelManager.isRunning)
        {
            if (this == currentlySelected) ClearSelected();
            return;
        }

        // get mouse coordinates
        Vector3 mouseCoord = GetMouseCoordinates();
        // if mouse was just clicked
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Using static frame counter to run exactly once per click among draggables, set selected
            if (lastFrameSetSelected != Time.frameCount)
            {
                lastFrameSetSelected = Time.frameCount;
                SetSelected();
            }

            // If I am being clicked on, save offset between mouse pointer and object coordinates
            if (this == currentlySelected)
            {
                clickOffset = transform.position - mouseCoord;
            }
        }

        // if dragging is active and left click is being held, change board positions
        if (this == currentlySelected && Mouse.current.leftButton.isPressed)
        {
            Move(mouseCoord + clickOffset);
        }
    }

    // Move the rigidbody directly so that the rigidbody position updates immediately
    protected virtual void FixedUpdate()
    {
        if (shouldMoveRigidBody)
        {
            rigidbody.MovePosition((Vector2)spotToMoveNext);
            shouldMoveRigidBody = false;
        }
    }

    protected virtual void Move(Vector3 destination)
    {
        spotToMoveNext = destination;
        shouldMoveRigidBody = true;
    }

    // Updates the color based on the given value of selected
    protected void UpdateColor(bool selected)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = selected == this ? selectColor : defaultColor;
        }
    }

    // Returns whether this object is currently being dragged
    public bool IsBeingDragged()
    {
        return this == currentlySelected && Mouse.current.leftButton.isPressed;
    }

    // Returns whether this object is currently selected
    public bool IsSelected()
    {
        return this == currentlySelected;
    }

    // Returns whether the currently selected draggable is actively being dragged
    public static bool SomethingIsBeingDragged()
    {
        return currentlySelected != null && currentlySelected.IsBeingDragged();
    }

    // select 1 board based on mouse coordinates
    static protected void SetSelected()
    {
        Physics2D.SyncTransforms();
        Collider2D[] hits = Physics2D.OverlapPointAll(GetMouseCoordinates());
        ClearSelected();
        // Iterate all the collideres
        for (int i = 0; i < hits.Length; i++)
        {
            // If the collider is a draggable, choose that one as selected
            if (hits[i].TryGetComponent<Draggable>(out Draggable draggable))
            {
                currentlySelected = draggable;
                currentlySelected.UpdateColor(true);
                return;
            }
        }
    }

    // clear the selected color
    static protected void ClearSelected()
    {
        if (currentlySelected != null)
        {
            currentlySelected.UpdateColor(false);
        }
        currentlySelected = null;
    }

    // Returns mouse click as a Vector3 so it's compatible with transform.position
    static protected Vector3 GetMouseCoordinates()
    {
        Vector3 coord = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        // prevents vector from going off coordinates
        coord.z = 0;
        return coord;
    }
}
