using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.Data;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.Utility;

namespace RTSLockstep.Grouping
{
    public class AttackGroup
    {
        private MovementGroup attackMoveGroup;
        private LSAgent currentGroupTarget;

        public int IndexID { get; set; }

        private byte controllerID;

        private FastList<Attack> attackers;

        private bool _calculatedBehaviors;

        public void Initialize(Command com)
        {
            _calculatedBehaviors = false;
            controllerID = com.ControllerID;
            Selection selection = GlobalAgentController.InstanceManagers[controllerID].GetSelection(com);
            attackers = new FastList<Attack>(selection.selectedAgentLocalIDs.Count);

            if (com.TryGetData(out DefaultData target) && target.Is(DataType.UShort))
            {
                if (GlobalAgentController.TryGetAgentInstance((ushort)target.Value, out LSAgent tempTarget))
                {
                    currentGroupTarget = tempTarget;
                }
            }

            if (currentGroupTarget.IsNotNull() && MovementGroupHelper.CheckValidAndAlert())
            {
                // create a movement group for attackers based on the current project
                Command moveCommand = new Command(AbilityDataItem.FindInterfacer(typeof(Move)).ListenInputID)
                {
                    ControllerID = controllerID
                };

                moveCommand.Add(currentGroupTarget.Body.Position);

                attackMoveGroup = MovementGroupHelper.CreateGroup(moveCommand);
            }
        }

        public void LocalSimulate()
        {

        }

        public void LateSimulate()
        {
            if (attackers.IsNotNull())
            {
                if (attackers.Count > 0)
                {
                    if (!_calculatedBehaviors)
                    {
                        _calculatedBehaviors = CalculateAndExecuteBehaviors();
                    }
                }

                if (attackers.Count == 0)
                {
                    Deactivate();
                }
            }
        }

        public void Add(Attack attacker)
        {
            if (attacker.MyAttackGroup.IsNotNull())
            {
                attacker.MyAttackGroup.attackers.Remove(attacker);
            }

            if (currentGroupTarget.IsNotNull())
            {
                attacker.MyAttackGroup = this;
                attacker.MyAttackGroupID = IndexID;

                attackers.Add(attacker);

                if (attackMoveGroup.IsNotNull())
                {
                    // add the attacker to our attacker move group too!
                    attackMoveGroup.Add(attacker.Agent.MyStats.CachedMove);
                }
            }
        }

        public void Remove(Attack attacker)
        {
            if (attacker.MyAttackGroup.IsNotNull() && attacker.MyAttackGroupID == IndexID)
            {
                attackers.Remove(attacker);
                attacker.MyAttackGroup = null;
                attacker.MyAttackGroupID = -1;

                if (attackMoveGroup.IsNotNull())
                {
                    // Remove the attacker from our attacker move group too!
                    attackMoveGroup.Remove(attacker.Agent.MyStats.CachedMove);
                }
            }
        }

        private bool CalculateAndExecuteBehaviors()
        {
            ExecuteAttack();
            return true;
        }

        private void Deactivate()
        {
            Attack attacker;
            for (int i = 0; i < attackers.Count; i++)
            {
                attacker = attackers[i];
                attacker.MyAttackGroup = null;
                attacker.MyAttackGroupID = -1;
            }
            attackers.FastClear();
            currentGroupTarget = null;
            attackMoveGroup = null;
            AttackGroupHelper.Pool(this);
            _calculatedBehaviors = false;
            IndexID = -1;
        }

        private void ExecuteAttack()
        {
            for (int i = 0; i < attackers.Count; i++)
            {
                Attack attacker = attackers[i];
                attacker.OnAttackGroupProcessed(currentGroupTarget);
            }
        }
    }
}
