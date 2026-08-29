using UnityEngine;

public class GravitySource : MonoBehaviour
{
    private void OnEnable()
    {
        CustomGravity.Register(this);
    }

    private void OnDisable()
    {
        CustomGravity.Unregister(this);
    }

    public virtual Vector3 GetGravity(Vector3 position)
    {
        return Physics.gravity;
    }
}
