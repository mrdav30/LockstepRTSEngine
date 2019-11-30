using FastCollections;
using RTSLockstep.Data;
using RTSLockstep.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public class AttackGroup
    {
        public MovementGroup AttackMoveGroup;
        public RTSAgent CurrentTarget;

        public int indexID { get; set; }

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
                    CurrentTarget = tempTarget;
                }
            }

            if (CurrentTarget.IsNotNull() && MovementGroupHelper.CheckValidAndAlert())
            {
                // create a movement group for constructors based on the current project
                Command moveCommand = new Command(AbilityDataItem.FindInterfacer(typeof(Move)).ListenInputID)
                {
                    ControllerID = controllerID
                };

                moveCommand.Add(CurrentTarget.Body.Position);

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
            attacker.MyAttackGroupID = indexID;

            attackers.Add(attacker);

            // add the constructor to our contructor move group too!
            AttackMoveGroup.Add(attacker.CachedMove);
        }

        public void Remove(Attack attacker)
        {
            if (attacker.MyAttackGroup.IsNotNull() && attacker.MyAttackGroupID == indexID)
            {
                attackers.Remove(attacker);

                // Remove the constructor to our contructor move group too!
                AttackMoveGroup.Remove(attacker.CachedMove);
            }
        }

        private bool CalculateAndExecuteBehaviors()
        {
            if (CurrentTarget.IsNotNull())
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
            CurrentTarget = null;
            AttackMoveGroup = null;
            AttackGroupHelper.Pool(this);
            _calculatedBehaviors = false;
            indexID = -1;
        }

        private void ExecuteAttack()
        {
            for (int i = 0; i < attackers.Count; i++)
            {
                Attack attacker = attackers[i];
                attacker.OnAttackGroupProcessed();
            }
        }
    }
}
