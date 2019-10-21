using UnityEngine;

public static class NDRaycast
{
    public static bool Raycast(Ray ray, out RaycastHit hit)
    {
        return Physics.Raycast(ray, out hit);
    }

    public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
    {
        return Physics.Raycast(origin, direction, out hitInfo, maxDistance);
    }
}
