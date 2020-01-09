using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.Utility;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RTSLockstep.Player.Utility
{
    public static class SelectionManager
    {
        public static bool CanBox = true;

        public const int MaximumSelection = 512;

        public static bool IsGathering { get; set; }

        public static LSAgent MousedAgent { get; private set; }

        public static Vector2 MousePosition;
        public static Vector2 MouseWorldPosition;
        public static Vector2 BoxStart;
        public static Vector2 BoxEnd;
        public static Vector2 Box_TopLeft;
        public static Vector2 Box_TopRight;
        public static Vector2 Box_BottomLeft;
        public static Vector2 Box_BottomRight;
        public static bool Boxing;
        private const float MinBoxSqrDist = 4;

        private static Vector2 agentPos;
        private static readonly FastSorter<LSAgent> bufferSelectedAgents = new FastSorter<LSAgent>(((source, other) => source.BoxPriority - other.BoxPriority));
        private static readonly FastList<LSAgent> SelectedAgents = new FastList<LSAgent>();

        private static LSAgent curAgent;
        private static RaycastHit hit;
        private static Ray ray;
        private static Vector2 Point;
        private static Vector2 Edge;
        private static Vector2 dif;

        private static bool _selectionLocked;

        public static void Initialize()
        {
            ClearSelection();
            PlayerInputHelper.OnSingleLeftTapDown += HandleSingleLeftClick;
            PlayerInputHelper.OnLeftTapHoldDown += HandleLeftClickHoldDown;
            PlayerInputHelper.OnDoubleLeftTapDown += HandleDoubleLeftClickDown;
            PlayerInputHelper.OnLeftTapUp += HandleLeftClickUp;
            _selectionLocked = true;
        }

        public static void Update()
        {
            MousePosition = Input.mousePosition;
            MouseWorldPosition = RTSInterfacing.GetWorldPos(MousePosition);
            GetMousedAgent();
            if (Boxing && CanBox)
            {
                //if (CanBox)
                //{
                if (MousePosition != BoxEnd)
                {
                    Vector2 RaycastTopLeft;
                    Vector2 RaycastTopRight;
                    Vector2 RaycastBotLeft;
                    Vector2 RaycastBotRight;

                    BoxEnd = MousePosition;
                    if (BoxStart.x < BoxEnd.x)
                    {
                        RaycastTopLeft.x = BoxStart.x;
                        RaycastBotLeft.x = BoxStart.x;
                        RaycastTopRight.x = BoxEnd.x;
                        RaycastBotRight.x = BoxEnd.x;
                    }
                    else
                    {
                        RaycastTopLeft.x = BoxEnd.x;
                        RaycastBotLeft.x = BoxEnd.x;
                        RaycastTopRight.x = BoxStart.x;
                        RaycastBotRight.x = BoxStart.x;
                    }
                    if (BoxStart.y < BoxEnd.y)
                    {
                        RaycastBotLeft.y = BoxStart.y;
                        RaycastBotRight.y = BoxStart.y;
                        RaycastTopLeft.y = BoxEnd.y;
                        RaycastTopRight.y = BoxEnd.y;
                    }
                    else
                    {
                        RaycastBotLeft.y = BoxEnd.y;
                        RaycastBotRight.y = BoxEnd.y;
                        RaycastTopLeft.y = BoxStart.y;
                        RaycastTopRight.y = BoxStart.y;
                    }
                    Box_TopLeft = RTSInterfacing.GetWorldPos(RaycastTopLeft);
                    Box_TopRight = RTSInterfacing.GetWorldPos(RaycastTopRight);
                    Box_BottomLeft = RTSInterfacing.GetWorldPos(RaycastBotLeft);
                    Box_BottomRight = RTSInterfacing.GetWorldPos(RaycastBotRight);
                }
                ClearBox();
                //int lecount = 0;
                if ((BoxEnd - BoxStart).sqrMagnitude >= MinBoxSqrDist)
                {
                    bufferSelectedAgents.Clear();
                    for (int i = 0; i < GlobalAgentController.LocalAgentControllersCount; i++)
                    {
                        var agentController = GlobalAgentController.GetAgentController(i);
                        for (int j = 0; j < LocalAgentController.MaxAgents; j++)
                        {
                            if (agentController.LocalAgentActive[j])
                            {
                                curAgent = agentController.LocalAgents[j];
                                if (curAgent.CanSelect)
                                {
                                    //always add mousedagent
                                    if (curAgent.RefEquals(MousedAgent))
                                    {
                                        bufferSelectedAgents.Add(curAgent);
                                    }
                                    else if (curAgent.IsOwnedBy(PlayerManager.CurrentPlayerController))
                                    {
                                        agentPos = curAgent.Position2;
                                        Edge = Box_TopRight - Box_TopLeft;
                                        Point = agentPos - Box_TopLeft;
                                        if (DotEdge() < 0)
                                        {
                                            Edge = Box_BottomRight - Box_TopRight;
                                            Point = agentPos - Box_TopRight;
                                            if (DotEdge() < 0)
                                            {
                                                Edge = Box_BottomLeft - Box_BottomRight;
                                                Point = agentPos - Box_BottomRight;
                                                if (DotEdge() < 0)
                                                {
                                                    Edge = Box_TopLeft - Box_BottomLeft;
                                                    Point = agentPos - Box_BottomLeft;
                                                    if (DotEdge() < 0)
                                                    {
                                                        bufferSelectedAgents.Add(curAgent);
                                                        continue;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (bufferSelectedAgents.Count > 0)
                    {
                        int peakBoxPriority = bufferSelectedAgents.PeekMax().BoxPriority;
                        while (bufferSelectedAgents.Count > 0)
                        {
                            LSAgent agent = bufferSelectedAgents.PopMax();
                            if (agent.BoxPriority < peakBoxPriority)
                            {
                                break;
                            }
                            BoxAgent(agent);
                        }
                    }
                }
                else
                {
                    BoxAgent(MousedAgent);
                }
                //   }

                //if (Input.GetMouseButtonUp(0))
                //{
                //    if (!_selectionLocked && !Input.GetKey(KeyCode.LeftShift))
                //    {
                //        ClearSelection();
                //    }

                //    if (!IsGathering)
                //    {
                //        SelectSelectedAgents();
                //    }

                //    Boxing = false;
                //}
            }
        }

        // do left click things
        public static void HandleSingleLeftClick()
        {
            QuickSelect();
        }

        // do left click hold things
        public static void HandleLeftClickHoldDown()
        {
            StartBoxing(MousePosition);
        }

        public static void HandleLeftClickUp()
        {
            if (Boxing)
            {
                if (!_selectionLocked && !Input.GetKey(KeyCode.LeftShift))
                {
                    ClearSelection();
                }

                if (!IsGathering)
                {
                    SelectSelectedAgents();
                }

                Boxing = false;
            }
        }

        // do double click things
        public static void HandleDoubleLeftClickDown()
        {
            Boxing = false;
            if (MousedAgent)
            {
                bufferSelectedAgents.Clear();
                for (int i = 0; i < GlobalAgentController.LocalAgentControllersCount; i++)
                {
                    var agentController = GlobalAgentController.GetAgentController(i);
                    for (int j = 0; j < LocalAgentController.MaxAgents; j++)
                    {
                        if (agentController.LocalAgentActive[j])
                        {
                            curAgent = agentController.LocalAgents[j];
                            if (curAgent.CanSelect)
                            {
                                if (curAgent.IsVisible
                                    && curAgent.ObjectName == MousedAgent.ObjectName
                                    && curAgent.IsOwnedBy(PlayerManager.CurrentPlayerController))
                                {
                                    bufferSelectedAgents.Add(curAgent);
                                }
                            }
                        }
                    }
                }

                if (bufferSelectedAgents.Count > 0)
                {
                    int peakBoxPriority = bufferSelectedAgents.PeekMax().BoxPriority;
                    while (bufferSelectedAgents.Count > 0)
                    {
                        LSAgent agent = bufferSelectedAgents.PopMax();
                        if (agent.BoxPriority < peakBoxPriority)
                            break;
                        BoxAgent(agent);
                    }

                    SelectSelectedAgents();
                }
            }
        }

        public static void StartBoxing(Vector2 boxStart)
        {
            Boxing = true;
            BoxStart = MousePosition;
            BoxEnd = MousePosition;
        }

        public static void BoxAgent(LSAgent agent)
        {
            if (agent is null)
            {
                return;
            }

            SelectedAgents.Add(agent);
            agent.IsHighlighted = true;
        }

        public static void QuickSelect()
        {
            if (!_selectionLocked && !Input.GetKey(KeyCode.LeftShift))
            {
                ClearSelection();
            }

            SelectAgent(MousedAgent);
        }

        public static void SelectAgent(LSAgent agent)
        {
            if (agent.IsNotNull())
            {
                Selector.Add(agent);
            }
        }

        public static void UnselectAgent(LSAgent agent)
        {
            if (agent.IsNotNull())
            {
                Selector.Remove(agent);
            }
        }

        private static void CullSelectedAgents()
        {

        }

        private static void SelectSelectedAgents()
        {
            for (int i = 0; i < SelectedAgents.Count; i++)
            {
                SelectAgent(SelectedAgents.innerArray[i]);
            }
        }

        public static void ClearSelection()
        {
            Selector.Clear();
        }

        public static void SetSelectionLock(bool lockState)
        {
            _selectionLocked = lockState ? true : false;
        }

        public static void ClearBox()
        {
            for (int i = 0; i < SelectedAgents.Count; i++)
            {
                SelectedAgents.innerArray[i].IsHighlighted = false;
            }
            SelectedAgents.FastClear();
        }

        public static void DrawRealWorldBox()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(new Vector3(Box_TopLeft.x, 0, Box_TopLeft.y), Vector3.one);
            Gizmos.color = Color.green;
            Gizmos.DrawCube(new Vector3(Box_TopRight.x, 0, Box_TopRight.y), Vector3.one);
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(new Vector3(Box_BottomRight.x, 0, Box_BottomRight.y), Vector3.one);
            Gizmos.color = Color.white;
            Gizmos.DrawCube(new Vector3(Box_BottomLeft.x, 0, Box_BottomLeft.y), Vector3.one);
        }

        public static void DrawBox(GUIStyle style)
        {
            if (Boxing)
            {
                Vector2 Size = BoxEnd - BoxStart;
                GUI.Box(new Rect(BoxStart.x, Screen.height - BoxStart.y, Size.x, -Size.y), "", style);
            }
        }

        public static float DotEdge()
        {
            return Point.x * -Edge.y + Point.y * Edge.x;
        }

        private static void GetMousedAgent()
        {
            if (EventSystem.current.IsNotNull() && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            //reimplement commander manager check, all controllers should have commander
            MouseOver(RTSInterfacing.GetScreenAgent(Input.mousePosition, (agent) =>
            {
                return agent.CanSelect; // && PlayerManager.ContainsController(agent.Controller);
            }));
        }

        private static void MouseOver(LSAgent agent)
        {
            if (MousedAgent.RefEquals(agent))
            {
                return;
            }

            if (MousedAgent.IsNotNull())
            {
                MousedAgent.IsHighlighted = false;
            }

            MousedAgent = agent;

            if (agent.IsNotNull())
            {
                if (!Boxing)
                {
                    agent.IsHighlighted = true;
                }
            }
        }
    }
}