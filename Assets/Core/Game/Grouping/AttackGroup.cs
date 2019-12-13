using FastCollections;
using RTSLockstep.Data;

namespace RTSLockstep
{
    public class AttackGroup
    {
        public MovementGroup AttackMoveGroup;
        public RTSAgent CurrentGroupTarget;

        public int IndexID { get; set; }

        private byte controllerID;

        private FastList<Attack> attackers;

        private bool _calculatedBehaviors;

        public void Initialize(Command com)
        {
            _calculatedBehaviors = false;
            controllerID = com.ControllerID;
            Selection selection = AgentController.InstanceManagers[controllerID].GetSelection(com);
            attackers = new FastList<Attack>(selection.selectedAgentLocalIDs.Count);

           if (com.TryGetData(out DefaultData target) && target.Is(DataType.UShort))
            {
                if (AgentController.TryGetAgentInstance((ushort)target.Value, out RTSAgent tempTarget))
                {
                    CurrentGroupTarget = tempTarget;
                }
            }

            if (CurrentGroupTarget.IsNotNull() && MovementGroupHelper.CheckValidAndAlert())
            {
                // create a movement group for attackers based on the current project
                Command moveCommand = new Command(AbilityDataItem.FindInterfacer(typeof(Move)).ListenInputID)
                {
                    ControllerID = controllerID
                };

                moveCommand.Add(CurrentGroupTarget.Body.Position);

                AttackMoveGroup = MovementGroupHelper.CreateGroup(moveCommand);
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
            attacker.MyAttackGroup = this;
            attacker.MyAttackGroupID = IndexID;

            attackers.Add(attacker);

            // add the attacker to our attacker move group too!
            AttackMoveGroup.Add(attacker.Agent.MyStats.CachedMove);
        }

        public void Remove(Attack attacker)
        {
            if (attacker.MyAttackGroup.IsNotNull() && attacker.MyAttackGroupID == IndexID)
            {
                attackers.Remove(attacker);
                attacker.MyAttackGroup = null;
                attacker.MyAttackGroupID = -1;

                // Remove the attacker from our attacker move group too!
                AttackMoveGroup.Remove(attacker.Agent.MyStats.CachedMove);
            }
        }

        private bool CalculateAndExecuteBehaviors()
        {
            if (CurrentGroupTarget.IsNotNull())
            {
                ExecuteAttack();
            }

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
            CurrentGroupTarget = null;
            AttackMoveGroup = null;
            AttackGroupHelper.Pool(this);
            _calculatedBehaviors = false;
            IndexID = -1;
        }

        private void ExecuteAttack()
        {
            for (int i = 0; i < attackers.Count; i++)
            {
                Attack attacker = attackers[i];
                attacker.OnAttackGroupProcessed(CurrentGroupTarget);
            }
        }
    }
}
