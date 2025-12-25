using UnityEngine;

public class Rotate : MonoBehaviour
{
    void Update()
    {
       transform.Rotate (new Vector3 (0, 0, 180) * (Time.deltaTime)/3); 
    }
}
