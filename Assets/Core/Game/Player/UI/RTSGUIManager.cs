using RTSLockstep.Agents.Visuals;
using UnityEngine;

namespace RTSLockstep.Player.UI
{
    public class RTSGUIManager : GUIManager
    {
        public override bool CameraChanged
        {
            get
            {
                return Camera.main.transform.hasChanged;
            }
        }

        public override float CameraScale
        {
            get
            {
                return Camera.main.transform.position.y;
            }
        }

        public override bool CanHUD
        {
            get
            {
                return true;
            }
        }

        public override bool CanInteract
        {
            get
            {
                return true;
            }
        }

        public override void InformationDown()
        {

        }

        public override Camera MainCam
        {
            get
            {
                return Camera.main;
            }
        }

        public override bool ShowHealthWhenFull
        {
            get
            {
                return true;
            }
        }
    }
}
