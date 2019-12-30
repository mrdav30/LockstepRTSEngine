using UnityEngine;

namespace RTSLockstep.Player
{
    public class RallyPoint : MonoBehaviour
    {
        public bool ActiveStatus { get; private set; }

        public void Enable()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) renderer.enabled = true;
            ActiveStatus = true;
        }

        public void Disable()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) renderer.enabled = false;
            ActiveStatus = false;
        }
    }
}
