using FastCollections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RTSLockstep
{
    public static class SelectionManager
    {
        private static bool _canBox = true;

        public static bool CanBox { get { return _canBox; } set { _canBox = value; } }

        public const int MaximumSelection = 512;

        public static bool IsGathering { get; set; }

        public static RTSAgent MousedAgent { get; private set; }

        public static Vector2 MousePosition;
        public static Vector2 MouseWorldPosition;
        public static Vector2 BoxStart;
        public static Vector2 BoxEnd;
        public static Vector2 Box_TopLeft;
        public static Vector2 Box_TopRight;
        public static Vector2 Box_BottomLeft;
        public static Vector2 Box_BottomRight;
        public static bool Boxing;
        private static float BoxingTime;

        private static bool _checkBoxDistance;
        private static bool CheckBoxDistance { get { return _checkBoxDistance; } }

        private const float MinBoxSqrDist = 4;

        private static Vector2 agentPos;
        private static readonly FastSorter<RTSAgent> bufferBoxedAgents = new FastSorter<RTSAgent>(((source, other) => source.BoxPriority - other.BoxPriority));
        private static readonly FastList<RTSAgent> BoxedAgents = new FastList<RTSAgent>();

        private static bool _selectionLocked = false;

        private static bool one_click;
        private static float delay = 0.5f;//max time in seconds for it to be considered a double click
        private static float timeCounter;

        public static void Initialize()
        {
            timeCounter = delay;
            ClearSelection();
        }

        public static void Update()
        {
            MousePosition = Input.mousePosition;
            MouseWorldPosition = RTSInterfacing.GetWorldPos(MousePosition);
            GetMousedAgent();
            if (Boxing)
            {
                if (CanBox)
                {
                    BoxingTime += Time.deltaTime;
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
                        bufferBoxedAgents.Clear();
                        for (int i = 0; i < PlayerManager.AgentControllerCount; i++)
                        {
                            var agentController = PlayerManager.GetAgentController(i);
                            for (int j = 0; j < AgentController.MaxAgents; j++)
                            {
                                if (agentController.LocalAgentActive[j])
                                {
                                    curAgent = agentController.LocalAgents[j];
                                    if (curAgent.CanSelect)
                                    {
                                        if (curAgent.RefEquals(MousedAgent))
                                        {
                                            bufferBoxedAgents.Add(curAgent);
                                        }
                                        else
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
                                                            bufferBoxedAgents.Add(curAgent);
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
                        bufferBoxable.FastClear();
                        //bool noneBoxable = true;
                        if (bufferBoxedAgents.Count > 0)
                        {
                            int peakBoxPriority = bufferBoxedAgents.PeekMax().BoxPriority;
                            while (bufferBoxedAgents.Count > 0)
                            {
                                RTSAgent agent = bufferBoxedAgents.PopMax();
                                if (agent.BoxPriority < peakBoxPriority)
                                    break;
                                BoxAgent(agent);
                            }
                        }

                    }
                    else
                    {
                        BoxAgent(MousedAgent);
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    if (_selectionLocked && !Input.GetKey(KeyCode.LeftShift))
                    {
                        ClearSelection();
                    }

                    if (IsGathering == false)
                    {
                        SelectBoxedAgents();
                    }

                    Boxing = false;
                }
            }
            else
            {
                if (IsGathering == false && Input.GetMouseButtonDown(0))
                {
                    if (!one_click) // first click no previous clicks
                    {
                        // do single click things
                        one_click = true;

                        _checkBoxDistance = true;
                        StartBoxing(MousePosition);
                    }
                    else
                    {
                        // found a double click, now reset
                        one_click = false;
                        // do double click things

                        if (MousedAgent)
                        {
                            SelectAgent(MousedAgent);
                        }
                    }
                }

                if (one_click)
                {
                    timeCounter -= Time.deltaTime;
                    if (timeCounter <= 0)
                    {
                        //if thats true its been too long and we want to reset so 
                        //the next click is simply a single click and not a double click.
                        one_click = false;
                        timeCounter = delay; 
                    }
                }
            }
        }

        public static void StartBoxing(Vector2 boxStart)
        {
            Boxing = true;
            BoxingTime = 0f;
            BoxStart = MousePosition;
            BoxEnd = MousePosition;
        }

        public static void QuickSelect()
        {
            if (_selectionLocked)
                ClearSelection();
            SelectAgent(MousedAgent);
        }

        public static void SelectAgent(RTSAgent agent)
        {

            if (agent.IsNotNull())
            {
                Selector.Add(agent);
            }
        }

        public static void UnselectAgent(RTSAgent agent)
        {
            if (agent.IsNotNull())
            {
                Selector.Remove(agent);
            }
        }

        public static void BoxAgent(RTSAgent agent)
        {
            if (System.Object.ReferenceEquals(agent, null))
                return;
            BoxedAgents.Add(agent);
            agent.IsHighlighted = true;
        }

        static readonly FastList<RTSAgent> bufferBoxable = new FastList<RTSAgent>();

        private static void CullBoxedAgents()
        {

        }

        private static void SelectBoxedAgents()
        {
            for (int i = 0; i < BoxedAgents.Count; i++)
            {
                SelectAgent(BoxedAgents.innerArray[i]);
            }
        }

        public static void ClearSelection()
        {
            Selector.Clear();
        }

        public static void SetSelectionLock(bool lockState)
        {
            if (lockState)
            {
                _selectionLocked = true;
            }
            else
            {
                _selectionLocked = false;
            }
        }

        public static void ClearBox()
        {
            for (int i = 0; i < BoxedAgents.Count; i++)
            {
                BoxedAgents.innerArray[i].IsHighlighted = false;
            }
            BoxedAgents.FastClear();
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

        static RTSAgent curAgent;
        static RaycastHit hit;
        static Ray ray;
        static Vector2 Point;
        static Vector2 Edge;

        public static float DotEdge()
        {
            return Point.x * -Edge.y + Point.y * Edge.x;
        }

        static Vector2 dif;

        private static void GetMousedAgent()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            //reimplement commander manager check, all controllers should have commander
            MouseOver(RTSInterfacing.GetScreenAgent(Input.mousePosition, (agent) =>
            {
                return agent.CanSelect; // && PlayerManager.ContainsController(agent.Controller);
            }));

        }

        private static void MouseOver(RTSAgent agent)
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
                if (SelectionManager.Boxing == false)
                    agent.IsHighlighted = true;
            }
        }
    }
}