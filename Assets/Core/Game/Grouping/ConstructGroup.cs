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
        public Queue<QStructure> ConstructionQueue;

        public RTSAgent CurrentProject;
        public Structure ProjectStructure { get { return CurrentProject.GetAbility<Structure>(); } }

        public int indexID { get; set; }

        private byte controllerID;

        public FastList<Construct> constructors;

        private bool calculatedBehaviors;

        public void Initialize(Command com)
        {
            // check if we're queueing structures to construct
            if (com.ContainsData<QueueStructure>())
            {
                QueueStructure[] queuedStructures = com.GetDataArray<QueueStructure>();
                ConstructionQueue = new Queue<QStructure>();

                for (int i = 0; i < queuedStructures.Length; i++)
                {
                    ConstructionQueue.Enqueue(queuedStructures[i].Value);
                }
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

            calculatedBehaviors = false;
            controllerID = com.ControllerID;
            Selection selection = AgentController.InstanceManagers[controllerID].GetSelection(com);
            constructors = new FastList<Construct>(selection.selectedAgentLocalIDs.Count);
        }

        public void LocalSimulate()
        {

        }

        public void LateSimulate()
        {
            if (constructors.IsNotNull())
            {
                if (constructors.Count > 0 && !calculatedBehaviors)
                {
                    calculatedBehaviors = CalculateAndExecuteBehaviors();
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
        }

        public void Remove(Construct constructor)
        {
            if (constructor.MyConstructGroup.IsNotNull() && constructor.MyConstructGroupID == indexID)
            {
                constructors.Remove(constructor);
            }
        }

        public bool CalculateAndExecuteBehaviors()
        {
            if (ConstructionQueue.IsNotNull())
            {
                while (ConstructionQueue.Count > 0)
                {
                    QStructure qStructure = ConstructionQueue.Dequeue();
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
                                //Set the current project is we don't have one
                                CurrentProject = newRTSAgent;
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

            if (CurrentProject.IsNotNull())
            {
                ExecuteConstruction();
            }

            return true;
        }

        public void Deactivate()
        {
            Construct constructor;
            for (int i = 0; i < constructors.Count; i++)
            {
                constructor = constructors[i];
                constructor.MyConstructGroup = null;
                constructor.MyConstructGroupID = -1;
            }
            constructors.FastClear();
            if (ConstructionQueue.IsNotNull())
            {
                ConstructionQueue.Clear();
            }
            CurrentProject = null;
            ConstructionGroupHelper.Pool(this);
            calculatedBehaviors = false;
            indexID = -1;
        }

        private void ExecuteConstruction()
        {
            // create a movement group for constructors based on the current project
            Command moveCommand = new Command(AbilityDataItem.FindInterfacer(typeof(Move)).ListenInputID);
            moveCommand.ControllerID = controllerID;
            moveCommand.Add(CurrentProject.Body.Position);

            MovementGroupHelper.StaticExecute(moveCommand);

            for (int i = 0; i < constructors.Count; i++)
            {
                Construct constructor = constructors[i];
                constructor.IsGroupConstructing = true;
                constructor.OnConstructGroupProcessed();
            }
        }
    }
}
