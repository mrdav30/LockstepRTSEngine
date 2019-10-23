using FastCollections;
using RTSLockstep.Data;
using RTSLockstep.Grid;
using RTSLockstep.Pathfinding;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public class ConstructGroup
    {
        public MovementGroup ConstructMoveGroup;
        public Queue<RTSAgent> ConstructionQueue = new Queue<RTSAgent>();
        public RTSAgent CurrentProject;

        public int indexID { get; set; }

        private byte controllerID;

        private FastList<Construct> constructors;

        private bool _calculatedBehaviors;

        public void Initialize(Command com)
        {
            _calculatedBehaviors = false;
            controllerID = com.ControllerID;
            Selection selection = AgentController.InstanceManagers[controllerID].GetSelection(com);
            constructors = new FastList<Construct>(selection.selectedAgentLocalIDs.Count);

            // check if we're queueing structures to construct
            if (com.ContainsData<QueueStructure>())
            {
                QueueStructure[] queuedStructures = com.GetDataArray<QueueStructure>();

                ProcessConstructionQueue(queuedStructures);
            }
            // otherwise were going to help construct
            else if (com.TryGetData(out DefaultData targetValue, 1) && targetValue.Is(DataType.UShort))
            {
                if (AgentController.TryGetAgentInstance((ushort)targetValue.Value, out RTSAgent tempTarget))
                {
                    if (tempTarget && tempTarget.GetAbility<Structure>().NeedsConstruction)
                    {
                        CurrentProject = tempTarget;
                    }
                }
            }

            if (CurrentProject.IsNotNull() && MovementGroupHelper.CheckValidAndAlert())
            {
                // create a movement group for constructors based on the current project
                Command moveCommand = new Command(AbilityDataItem.FindInterfacer(typeof(Move)).ListenInputID)
                {
                    ControllerID = controllerID
                };

                moveCommand.Add(CurrentProject.Body.Position);

                ConstructMoveGroup = MovementGroupHelper.CreateGroup(moveCommand);
            }
        }

        public void LocalSimulate()
        {

        }

        public void LateSimulate()
        {
            if (constructors.IsNotNull())
            {
                if (constructors.Count > 0)
                {
                    if (!_calculatedBehaviors)
                    {
                        _calculatedBehaviors = CalculateAndExecuteBehaviors();
                    }
                    else if (!CurrentProject.GetAbility<Structure>().NeedsConstruction && ConstructionQueue.Count > 0)
                    {
                        CurrentProject = ConstructionQueue.Dequeue();
                    }
                }

                if (constructors.Count == 0)
                {
                    Deactivate();
                }
            }
        }

        public void Add(Construct constructor)
        {
            if (constructor.MyConstructGroup.IsNotNull())
            {
                constructor.MyConstructGroup.constructors.Remove(constructor);
            }
            constructor.MyConstructGroup = this;
            constructor.MyConstructGroupID = indexID;

            constructors.Add(constructor);

            ConstructMoveGroup.Add(constructor.CachedMove);
        }

        public void Remove(Construct constructor)
        {
            if (constructor.MyConstructGroup.IsNotNull() && constructor.MyConstructGroupID == indexID)
            {
                constructors.Remove(constructor);

                ConstructMoveGroup.Remove(constructor.CachedMove);
            }
        }

        private bool CalculateAndExecuteBehaviors()
        {
            if (CurrentProject.IsNotNull())
            {
                ExecuteConstruction();
            }

            return true;
        }

        private void ProcessConstructionQueue(QueueStructure[] _queueStructures)
        {
            for (int i = 0; i < _queueStructures.Length; i++)
            {
                QStructure qStructure = _queueStructures[i].Value;
                if (qStructure.IsNotNull())
                {
                    RTSAgent newRTSAgent = AgentController.InstanceManagers[controllerID].CreateAgent(qStructure.StructureName, qStructure.BuildPoint, qStructure.RotationPoint) as RTSAgent;
                    Structure newStructure = newRTSAgent.GetAbility<Structure>();

                    if (newStructure.StructureType == StructureType.Wall)
                    {
                        newRTSAgent.transform.localScale = qStructure.LocalScale.ToVector3();
                        newStructure.IsOverlay = true;
                    }

                    newRTSAgent.Body.HalfWidth = qStructure.HalfWidth;
                    newRTSAgent.Body.HalfLength = qStructure.HalfLength;

                    newStructure.BuildSizeLow = (newRTSAgent.Body.HalfWidth.CeilToInt() * 2);
                    newStructure.BuildSizeHigh = (newRTSAgent.Body.HalfLength.CeilToInt() * 2);

                    if (GridBuilder.Place(newRTSAgent.GetAbility<Structure>(), newRTSAgent.Body.Position))
                    {
                        // build structures bounds so the blocker can update the covered nodes
                        newRTSAgent.Body.BuildBounds();
                        newRTSAgent.GetAbility<DynamicBlocker>().UpdateNodeObstacles();

                        AgentController.InstanceManagers[controllerID].Commander.CachedResourceManager.RemoveResources(newRTSAgent);

                        newRTSAgent.SetCommander(AgentController.InstanceManagers[controllerID].Commander);

                        newRTSAgent.gameObject.name = newRTSAgent.objectName;
                        newRTSAgent.transform.parent = newStructure.StructureType == StructureType.Wall ? WallPositioningHelper.OrganizerWalls.transform
                            : ConstructionHandler.OrganizerStructures.transform;

                        newStructure.AwaitConstruction();
                        // Set to transparent material until constructor is in range to start
                        ConstructionHandler.SetTransparentMaterial(newStructure.gameObject, GameResourceManager.AllowedMaterial, true);

                        if (CurrentProject.IsNull())
                        {
                            //Set the current project if we don't have one
                            CurrentProject = newRTSAgent;
                        }
                        else
                        {
                            ConstructionQueue.Enqueue(newRTSAgent);
                        }
                    }
                    else
                    {
                        Debug.Log("Couldn't place building!");
                        newRTSAgent.Die();
                    }
                }
            }
        }

        private void Deactivate()
        {
            Construct constructor;
            for (int i = 0; i < constructors.Count; i++)
            {
                constructor = constructors[i];
                constructor.MyConstructGroup = null;
                constructor.MyConstructGroupID = -1;
            }
            constructors.FastClear();
            ConstructionQueue.Clear();
            CurrentProject = null;
            ConstructMoveGroup = null;
            ConstructionGroupHelper.Pool(this);
            _calculatedBehaviors = false;
            indexID = -1;
        }

        private void ExecuteConstruction()
        {
            for (int i = 0; i < constructors.Count; i++)
            {
                Construct constructor = constructors[i];
                constructor.OnConstructGroupProcessed();
            }
        }
    }
}
