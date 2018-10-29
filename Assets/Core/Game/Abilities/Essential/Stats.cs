using UnityEngine;
using System;

namespace RTSLockstep {
    public class Stats : Ability {

        protected Move cachedMove;
        protected Attack cachedAtack;
        protected Health cachedHealth;
        protected override void OnLateSetup()
        {
            cachedMove = Agent.GetAbility<Move> ();
            cachedAtack = Agent.GetAbility<Attack> ();
            cachedHealth = Agent.GetAbility<Health> ();
        }

        public long Speed {
            get {
                return cachedMove.Speed;
            }
        }

        public long Range {
            get {
                return cachedAtack.Range;
            }
        }

        public long Damage {
            get {
                return cachedAtack.Damage;
            }
        }

        public long Health {
            get {
                return cachedHealth.MaxHealth;
            }
        }

        public long DPS {
            get {
				return cachedAtack.Damage.Div(cachedAtack.AttackInterval);
            }
        }

    }
}