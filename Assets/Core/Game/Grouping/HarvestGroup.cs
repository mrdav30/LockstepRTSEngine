using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.Utility;
using RTSLockstep.LSResources;
using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.Grouping
{
    public class HarvestGroup
    {
        private LSAgent _currentGroupTarget;

        public int IndexID { get; set; }

        private byte controllerID;

        private FastList<Harvest> harvesters;

        private bool _calculatedBehaviors;

        private const long gridSpacing = 2 * FixedMath.One;

        public void Initialize(Command com)
        {
            _calculatedBehaviors = false;
            controllerID = com.ControllerID;
            Selection selection = GlobalAgentController.InstanceManagers[controllerID].GetSelection(com);
            harvesters = new FastList<Harvest>(selection.selectedAgentLocalIDs.Count);

            if (com.TryGetData(out DefaultData target) && target.Is(DataType.UShort))
            {
                if (GlobalAgentController.TryGetAgentInstance((ushort)target.Value, out LSAgent tempTarget))
                {
                    if (tempTarget.MyAgentType == AgentType.RawMaterial
                        || tempTarget.MyAgentType == AgentType.Structure)
                    {
                        _currentGroupTarget = tempTarget;
                    }
                }
            }
        }

        public void LocalSimulate()
        {

        }

        public void LateSimulate()
        {
            if (harvesters.IsNull() || harvesters.Count == 0)
            {
                Deactivate();
            }
            else if (harvesters.Count > 0)
            {
                if (!_calculatedBehaviors)
                {
                    _calculatedBehaviors = CalculateAndExecuteBehaviors();
                }
            }
        }

        public void Add(Harvest harvester)
        {
            if (harvester.MyHarvestGroup.IsNotNull())
            {
                harvester.MyHarvestGroup.harvesters.Remove(harvester);
            }

            if (_currentGroupTarget.IsNotNull())
            {
                harvester.MyHarvestGroup = this;
                harvester.MyHarvestGroupID = IndexID;

                harvesters.Add(harvester);

            }
        }

        public void Remove(Harvest harvester)
        {
            if (harvester.MyHarvestGroup.IsNotNull() && harvester.MyHarvestGroupID == IndexID)
            {
                harvesters.Remove(harvester);
                harvester.MyHarvestGroup = null;
                harvester.MyHarvestGroupID = -1;
            }
        }

        private bool CalculateAndExecuteBehaviors()
        {
            if (_currentGroupTarget.MyAgentType == AgentType.RawMaterial)
            {
                ExecuteHarvest();
            }
            else if (_currentGroupTarget.MyAgentType == AgentType.Structure || _currentGroupTarget.IsNull())
            {
                // if current group target is null, execute a null deposit to stop harvest
                ExecuteDeposit();
            }

            return true;
        }

        private void Deactivate()
        {
            Harvest harvester;
            for (int i = 0; i < harvesters.Count; i++)
            {
                harvester = harvesters[i];
                harvester.MyHarvestGroup = null;
                harvester.MyHarvestGroupID = -1;
            }
            harvesters.FastClear();
            _currentGroupTarget = null;
            HarvestGroupHelper.Pool(this);
            _calculatedBehaviors = false;
            IndexID = -1;
        }

        private void ExecuteHarvest()
        {
            for (int i = 0; i < harvesters.Count; i++)
            {
                Harvest harvester = harvesters[i];
                harvester.IsHarvesting = true;
                harvester.IsEmptying = false;
                harvester.OnHarvestGroupProcessed(_currentGroupTarget);
            }
        }

        private void ExecuteDeposit()
        {
            for (int i = 0; i < harvesters.Count; i++)
            {
                Harvest harvester = harvesters[i];
                harvester.IsHarvesting = false;
                harvester.IsEmptying = true;
                harvester.OnHarvestGroupProcessed(_currentGroupTarget);
            }
        }
    }
}