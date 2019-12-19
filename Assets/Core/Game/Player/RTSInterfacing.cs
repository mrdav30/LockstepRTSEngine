using RTSLockstep.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public static class RTSInterfacing
    {
        public static RTSAgent MousedAgent { get; private set; }
        public static Ray CachedRay { get; private set; }
        public static Transform MousedObject { get { return CachedHit.transform; } }
        public static RaycastHit CachedHit;
        public static bool CachedDidHit { get; private set; }

        private static bool agentFound;
        private static float heightDif;
        private static float closestDistance;
        private static RTSAgent closestAgent;

        private static Vector3 checkDir;
        private static Vector3 checkOrigin;

        public static void Initialize()
        {
            CachedDidHit = false;
        }

        public static void Visualize()
        {
            if (UserInputHelper.GUIManager.MainCam.IsNotNull())
            {
                CachedRay = UserInputHelper.GUIManager.MainCam.ScreenPointToRay(Input.mousePosition);
                CachedDidHit = NDRaycast.Raycast(CachedRay, out CachedHit);

                MousedAgent = GetScreenAgent(Input.mousePosition, (agent) => { return true; });
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
                        curCom.SetData(new DefaultData(DataType.UShort, MousedAgent.LocalID));
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
        public static RTSAgent GetScreenAgent(Vector2 screenPos, Func<RTSAgent, bool> conditional = null)
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
            var start = new Vector3d(ray.origin);
            Vector3d end;
            end = new Vector3d(ray.origin + ray.direction * 50);

            if (ray.direction.y < -.05f)
            {
                float planeDist = ray.origin.y / -ray.direction.y;
                if (planeDist < 100)
                {
                    end = new Vector3d(ray.origin + ray.direction * planeDist);
                }
            }
            IEnumerable<LSBody> raycast = Raycaster.RaycastAll(start, end);

            foreach (var body in raycast)
            {
                if (body.Agent.IsNull())
                {
                    continue;
                }

                RTSAgent agent = body.Agent;

                if (agent.IsVisible)
                {
                    if (conditional(agent))
                    {
                        if (AgentIntersects(agent))
                        {
                            if (agentFound)
                            {
                                if (heightDif < closestDistance)
                                {
                                    closestDistance = heightDif;
                                    closestAgent = agent;
                                }
                            }
                            else
                            {
                                agentFound = true;
                                closestAgent = agent;
                                closestDistance = heightDif;
                            }
                        }
                    }
                }
            }
            if (agentFound)
            {
                return closestAgent;
            }

            return null;
        }

        public static Vector2d GetWorldPosHeight(Vector2 screenPos, float height = 0)
        {
            if (UserInputHelper.GUIManager.MainCam == null) return Vector2d.zero;
            Ray ray = UserInputHelper.GUIManager.MainCam.ScreenPointToRay(screenPos);
            //RaycastHit hit;

            Vector3 hitPoint = ray.origin - ray.direction * ((ray.origin.y - height) / ray.direction.y);
            //return new Vector2d(hitPoint.x * LockstepManager.InverseWorldScale, hitPoint.z * LockstepManager.InverseWorldScale);
            return new Vector2d(hitPoint.x, hitPoint.z);
        }

        public static Vector2d GetWorldPosD(Vector2 screenPos)
        {
            if (UserInputHelper.GUIManager.MainCam == null) return Vector2d.zero;
            Ray ray = UserInputHelper.GUIManager.MainCam.ScreenPointToRay(screenPos);
            RaycastHit hit;
            if (NDRaycast.Raycast(ray, out hit))
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
            if (UserInputHelper.GUIManager.MainCam == null) return default(Vector2);
            Ray ray = UserInputHelper.GUIManager.MainCam.ScreenPointToRay(screenPos);
            RaycastHit hit;
            if (NDRaycast.Raycast(ray, out hit))
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
            if (UserInputHelper.GUIManager.MainCam == null) return default(Vector2);
            Ray ray = UserInputHelper.GUIManager.MainCam.ScreenPointToRay(screenPos);
            RaycastHit hit;
            if (NDRaycast.Raycast(ray, out hit))
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
            if (UserInputHelper.GUIManager.MainCam == null) return false;
            Ray ray = UserInputHelper.GUIManager.MainCam.ScreenPointToRay(origin);
            RaycastHit hit;
            if (NDRaycast.Raycast(ray, out hit))
            {
                GameObject obj = hit.collider.gameObject;
                //  did we hit the defined ground layer?
                if (obj
                    && obj.layer == LayerMask.NameToLayer("Ground"))
                {
                    return true;
                }

            }
            return false;
        }

        private static bool AgentIntersects(RTSAgent agent)
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