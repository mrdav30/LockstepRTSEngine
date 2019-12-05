using UnityEngine;

namespace RTSLockstep
{
    public class AgentStats : Ability
    {
        public Move CachedMove { get; private set; }
        public Turn CachedTurn { get; private set; }
        public Attack CachedAttack { get; private set; }
        public Health CachedHealth { get; private set; }
        public ResourceDeposit CachedResourceDeposit { get; private set; }

        public Construct CachedConstruct { get; private set; }
        public Harvest CachedHarvest { get; private set; }

        public long CurrentHealth
        {
            get
            {
                return CachedHealth ? CachedHealth.CurrentHealth : 0;
            }
            private set { }
        }

        public long MaxHealth
        {
            get
            {
                return CachedHealth ? CachedHealth.MaxHealth : 0;
            }
            private set { }
        }

        public long AmountLeft
        {
            get
            {
                return CachedResourceDeposit ? CachedResourceDeposit.AmountLeft : 0;
            }
            private set { }
        }

        public long Capacity
        {
            get
            {
                return CachedResourceDeposit ? CachedResourceDeposit.Capacity : 0;
            }
            private set { }
        }

        [FixedNumber, SerializeField, Tooltip("Distance that the agent can strike from; i.e. attack, harvest")]
        protected long _actionRange = FixedMath.One * 6;
        public virtual long ActionRange { get { return _actionRange; } }

        [FixedNumber, SerializeField, Tooltip("Approximate radius that's scanned for targets")]
        protected long _sight = FixedMath.One * 10;
        public virtual long Sight { get { return _sight; } }

        public long MovementSpeed
        {
            get
            {
                return CachedMove ? CachedMove.MovementSpeed : 0;
            }
            private set { }
        }

        // Damage of attack
        public long Damage
        {
            get
            {
                return CachedAttack ? CachedAttack.Damage : 0;
            }
            private set { }
        }

        public long DPS
        {
            get
            {
                return CachedAttack ? CachedAttack.Damage.Div(CachedAttack.AttackSpeed) : 0;
            }
            private set { }
        }

        public bool CanMove { get { return CachedMove.IsNotNull(); } private set { } }
        public bool CanTurn { get { return CachedTurn.IsNotNull(); } private set { } }

        protected override void OnLateSetup()
        {
            CachedMove = Agent.GetAbility<Move>();
            CachedTurn = Agent.GetAbility<Turn>();
            CachedAttack = Agent.GetAbility<Attack>();
            CachedHealth = Agent.GetAbility<Health>();
            CachedResourceDeposit = Agent.GetAbility<ResourceDeposit>();

            CachedConstruct = Agent.GetAbility<Construct>();
            CachedHarvest = Agent.GetAbility<Harvest>();

            if (Sight < ActionRange)
            {
                _sight = ActionRange + FixedMath.One * 5;
            }
        }
    }
}