/// <summary>
/// Put this on any script whose state should rewind to the last checkpoint when the player dies.
/// CaptureState() returns a snapshot, RestoreState() puts the object back exactly as that snapshot.
/// </summary>
public interface ICheckpointable
{
    object CaptureState();
    void RestoreState(object state);
}