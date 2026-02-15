using UnityEngine;

public class MouseController : MonoBehaviour
{

    public Transform mouseCenter;
    public float maxXDistance = 0.5f;
    public float maxZDistance = 0.5f;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Hand"))
        {       
            //get position based on hand position       
            Vector3 newPosition = new Vector3(other.transform.position.x, transform.position.y, other.transform.position.z);
            newPosition.x = Mathf.Clamp(newPosition.x, mouseCenter.position.x - maxXDistance, mouseCenter.position.x + maxXDistance);
            newPosition.z = Mathf.Clamp(newPosition.z, mouseCenter.position.z - maxZDistance, mouseCenter.position.z + maxZDistance);

            //update hand position
            transform.position = newPosition;
        }
    }
}
