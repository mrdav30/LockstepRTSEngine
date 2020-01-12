using RTSLockstep.Simulation.LSMath;
using UnityEngine;

namespace RTSLockstep.BuildSystem
{
    public static class StructurePositionHelper
    {
        private static int granularity = 1;

        public static void SnapObjectToGrid(Transform obj)
        {
            obj.transform.position = GetSnappedPosition(obj.position);
        }

        public static void SnapObjectToGrid(GameObject obj)
        {
            SnapObjectToGrid(obj.transform);
        }

        public static Vector3 GetSnappedPosition(Transform obj)
        {
            Vector3 pos = obj.position;
            return GetSnappedPosition(pos);
        }

        public static Vector2d GetSnappedPositionD(Transform obj)
        {
            Vector3 pos = obj.position;
            return GetSnappedPositionD(pos);
        }

        public static Vector3 GetSnappedPosition(Vector3 pos)
        {
            float xPos = Mathf.Floor(pos.x / granularity) * granularity; // Mathf.Round(pos.x) + 0.5f;
            float yPos = pos.y; // Mathf.Round(pos.y);
            float zPos = Mathf.Floor(pos.z / granularity) * granularity;// Mathf.Round(pos.z) + 0.5f;

            return new Vector3(xPos, yPos, zPos);
        }

        public static Vector2d GetSnappedPositionD(Vector3 pos)
        {
            float xPos = Mathf.Floor(pos.x / granularity) * granularity;
            float yPos = pos.y;

            return new Vector2d(xPos, yPos);
        }

    }
}