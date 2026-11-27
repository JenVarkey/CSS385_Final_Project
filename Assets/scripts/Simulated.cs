// For objects that have behavior when the level is simulated

using UnityEngine;

public class Simulated : MonoBehaviour
{
    protected bool isInSimulation = false;

    protected virtual void Awake() { }

    // Save initial object state and start simulating
    public virtual void StartSimulation()
    {
        isInSimulation = true;
    }

    // Returned to object state before the simulation began
    public virtual void StopSimulation()
    {
        isInSimulation = false;
    }

    // Called whenever the level's goal is met
    public virtual void OnLevelGoalMet() { }

    // Called when the level is failed
    public virtual void OnLevelFailed() { }
}
