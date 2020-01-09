using System;
using System.Collections.Generic;
using UnityEngine;

using RTSLockstep.Agents;
using RTSLockstep.Data;
using RTSLockstep.LSResources;
using RTSLockstep.Player.Commands;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Simulation.LSPhysics;
using RTSLockstep.Utility;

namespace RTSLockstep.Player
{
    public static class RTSInterfacing
    {
        public static LSAgent MousedAgent { get; private set; }
        public static Ray CachedRay { get; private set; }
        public static Transform MousedObject { get { return CachedHit.transform; } }
        public static RaycastHit CachedHit;
        public static bool CachedDidHit { get; private set; }

        private static bool agentFound;
        private static float heightDif;
        private static float closestDistance;
        private static LSAgent closestAgent;

        private static Vector3 checkDir;
        private static Vector3 checkOrigin;

        public static void Initialize()
        {
            CachedDidHit = false;
        }

        public static void Visualize()
        {
            if (PlayerInputHelper.GUIManager.MainCam.IsNotNull())
            {
                CachedRay = PlayerInputHelper.GUIManager.MainCam.ScreenPointToRay(Input.mousePosition);
                CachedDidHit = NDRaycast.Raycast(CachedRay, out CachedHit);

                MousedAgent = GetScreenAgent(Input.mousePosition);
            }
        }

        public static Command GetProcessInterfacer(AbilityDataItem facer)
        {
            if (facer.IsNull())
            {
                Debug.LogError("Interfacer does not exist. Can't generate command.");
                return null;
            }

            Command curCom = null;
            switch (facer.InformationGather)
            {
                case InformationGatherType.Position:
                    curCom = new Command(facer.ListenInputID);
                    curCom.Add(GetWorldPosD(Input.mousePosition));
                    break;
                case InformationGatherType.Target:
                    curCom = new Command(facer.ListenInputID);
                    if (MousedAgent.IsNotNull())
                    {
                        curCom.Add(new DefaultData(DataType.UShort, MousedAgent.LocalID));
                    }
                    break;
                case InformationGatherType.PositionOrTarget:
                    curCom = new Command(facer.ListenInputID);
                    if (MousedAgent.IsNotNull())
                    {
                        curCom.Add(new DefaultData(DataType.UShort, MousedAgent.GlobalID));
                    }
                    break;
                case InformationGatherType.PositionOrAction:
                    curCom = new Command(facer.ListenInputID);
                    curCom.Add(GetWorldPosD(Input.mousePosition));
                    break;
                case InformationGatherType.None:
                    curCom = new Command(facer.ListenInputID);
                    break;
            }

            return curCom;
        }

        //change screenPos to Vector3?
        public static LSAgent GetScreenAgent(Vector2 screenPos, Func<LSAgent, bool> conditional = null)
        {
            if (conditional.IsNull())
            {
                conditional = (agent) =>
                {
                    return true;
                };
            }

            agentFound = false;
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            checkDir = ray.direction;
            checkOrigin = ray.origin;

            //Raycast to plane Z-0
            Vector3d start = new Vector3d(checkOrigin);
            Vector3d end = new Vector3d(checkOrigin + checkDir * 5000);

            if (checkDir.y < -.05f)
            {
                float planeDist = checkOrigin.y / -checkDir.y;
                if (planeDist < 100)
                {
                    end = new Vector3d(checkOrigin + checkDir * planeDist);
                }
            }
            IEnumerable<LSBody> raycast = Raycaster.RaycastAll(start, end);

            foreach (var body in raycast)
            {
                if (body.Agent.IsNull())
                {
                    continue;
                }

                LSAgent agent = body.Agent;

                if (agent.IsVisible)
                {
                    if (conditional(agent))
                    {
                        return agent;
                    }
                }
            }

            return null;
        }

        public static Vector2d GetWorldPosHeight(Vector2 screenPos, float height = 0)
        {
            if (PlayerInputHelper.GUIManager.MainCam.IsNull())
            {
                return Vector2d.zero;
            }
            Ray ray = PlayerInputHelper.GUIManager.MainCam.ScreenPointToRay(screenPos);
            //RaycastHit hit;

            Vector3 hitPoint = ray.origin - ray.direction * ((ray.origin.y - height) / ray.direction.y);
            //return new Vector2d(hitPoint.x * LockstepManager.InverseWorldScale, hitPoint.z * LockstepManager.InverseWorldScale);
            return new Vector2d(hitPoint.x, hitPoint.z);
        }

        public static Vector2d GetWorldPosD(Vector2 screenPos)
        {
            if (PlayerInputHelper.GUIManager.MainCam.IsNull()) {
                return Vector2d.zero;
            }
            Ray ray = PlayerInputHelper.GUIManager.MainCam.ScreenPointToRay(screenPos);
            if (NDRaycast.Raycast(ray, out RaycastHit hit))
            {
                //return new Vector2d(hit.point.x * LockstepManager.InverseWorldScale, hit.point.z * LockstepManager.InverseWorldScale);
                return new Vector2d(hit.point.x, hit.point.z);
            }

            Vector3 hitPoint = ray.origin - ray.direction * (ray.origin.y / ray.direction.y);
            //return new Vector2d(hitPoint.x * LockstepManager.InverseWorldScale, hitPoint.z * LockstepManager.InverseWorldScale);
            return new Vector2d(hitPoint.x, hitPoint.z);
        }

        public static Vector2 GetWorldPos(Vector2 screenPos)
        {
            if (PlayerInputHelper.GUIManager.MainCam.IsNull())
            {
                return default;
            }
            Ray ray = PlayerInputHelper.GUIManager.MainCam.ScreenPointToRay(screenPos);
            if (NDRaycast.Raycast(ray, out RaycastHit hit))
            {
                //return new Vector2(hit.point.x * LockstepManager.InverseWorldScale, hit.point.z * LockstepManager.InverseWorldScale);
                return new Vector2(hit.point.x, hit.point.z);
            }
            Vector3 hitPoint = ray.origin - ray.direction * (ray.origin.y / ray.direction.y);
            //    return new Vector2(hitPoint.x * LockstepManager.InverseWorldScale, hitPoint.z * LockstepManager.InverseWorldScale);
            return new Vector2(hitPoint.x, hitPoint.z);
        }

        public static Vector3 GetWorldPos3(Vector2 screenPos)
        {
            if (PlayerInputHelper.GUIManager.MainCam.IsNull())
            {
                return default(Vector2);
            }
            Ray ray = PlayerInputHelper.GUIManager.MainCam.ScreenPointToRay(screenPos);
            if (NDRaycast.Raycast(ray, out RaycastHit hit))
            {
                //return new Vector2(hit.point.x * LockstepManager.InverseWorldScale, hit.point.z * LockstepManager.InverseWorldScale);
                return hit.point;
            }
            Vector3 hitPoint = ray.origin - ray.direction * (ray.origin.y / ray.direction.y);
            //return new Vector2(hitPoint.x * LockstepManager.InverseWorldScale, hitPoint.z * LockstepManager.InverseWorldScale);
            return hitPoint;
        }

        public static bool HitPointIsGround(Vector3 origin)
        {
            if (PlayerInputHelper.GUIManager.MainCam.IsNull())
            {
                return false;
            }
            Ray ray = PlayerInputHelper.GUIManager.MainCam.ScreenPointToRay(origin);
            if (NDRaycast.Raycast(ray, out RaycastHit hit))
            {
                GameObject obj = hit.collider.gameObject;
                //  did we hit the defined ground layer?
                if (obj && obj.layer == LayerMask.NameToLayer("Ground"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AgentIntersects(LSAgent agent)
        {
            if (agent.IsVisible)
            {
                Vector3 agentPos = agent.VisualCenter.position;
                heightDif = checkOrigin.y - agentPos.y;
                float scaler = heightDif / checkDir.y;
                Vector2 levelPos;
                levelPos.x = (checkOrigin.x - (checkDir.x * scaler)) - agentPos.x;
                levelPos.y = (checkOrigin.z - (checkDir.z * scaler)) - agentPos.z;

                if (levelPos.sqrMagnitude <= agent.SelectionRadiusSquared)
                {
                    return true;
                }
            }

            return false;
        }
    }
}