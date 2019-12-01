using System;
using Newtonsoft.Json;
using UnityEngine;

namespace RTSLockstep
{
    public class Health : Ability
    {
        [SerializeField, FixedNumber]
        private long _maxHealth = FixedMath.One * 100;
        public long MaxHealth
        {
            get { return _maxHealth + MaxHealthModifier; }
        }

        public long BaseHealth { get { return _maxHealth; } }

        private long _maxHealthModifier;
        [Lockstep(true)]
        public long MaxHealthModifier
        {
            get
            {
                return _maxHealthModifier;
            }
            set
            {
                if (value != _maxHealthModifier)
                {
                    long dif = _maxHealthModifier - value;
                    if (dif > 0)
                    {
                        this.TakeDamage(-dif);
                        _maxHealthModifier = value;
                    }
                }
            }
        }

        public event Action OnHealthChange;
        public event Action<long> OnHealthDelta;
        public event Action<Health, AttackerInfo> OnDie;
        public event Action<LSProjectile> OnTakeProjectile;

        public bool CanLose
        {
            get
            {
                return CurrentHealth > 0;
            }
        }

        public bool CanGain
        {
            get
            {
                return CurrentHealth < MaxHealth;
            }
        }

        [SerializeField, FixedNumber]
        private long _currentHealth;

        public long CurrentHealth
        {
            get
            {
                return _currentHealth;
            }
            set
            {
                long delta = value - _currentHealth;
                _currentHealth = value;
                OnHealthChange?.Invoke();
                OnHealthDelta?.Invoke(delta);
            }
        }

        protected override void OnSetup()
        {
        }

        protected override void OnInitialize()
        {
            // Check if the agent starts with full health
            CurrentHealth = CurrentHealth == 0 ? MaxHealth : CurrentHealth;
            OnTakeProjectile = null;
            MaxHealthModifier = 0;
            LastAttacker = null;
        }

        public void TakeProjectile(LSProjectile projectile)
        {
            if (Agent.IsActive && CurrentHealth >= 0)
            {
                if (OnTakeProjectile.IsNotNull())
                {
                    OnTakeProjectile(projectile);
                }

                TakeDamage(projectile.CheckExclusiveDamage(Agent.Tag));
            }
        }

        AttackerInfo LastAttacker;
        public void TakeDamage(long damage, AttackerInfo attackerInfo = null)
        {
            if (damage >= 0)
            {
                CurrentHealth -= damage;
                if (attackerInfo != null)
                {
                    LastAttacker = attackerInfo;
                }
                // don't let the health go below zero
                if (CurrentHealth <= 0)
                {
                    CurrentHealth = 0;

                    Die();
                    return;
                }
            }
            else
            {
                CurrentHealth -= damage;
                if (CurrentHealth >= MaxHealth)
                {
                    CurrentHealth = MaxHealth;
                }
            }
        }

        public void Die()
        {
            if (Agent.IsActive)
            {
                if (OnDie.IsNotNull())
                {
                    OnDie(this, LastAttacker);
                }

                Agent.Die();
            }
        }

        protected override void OnDeactivate()
        {
            OnTakeProjectile = null;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteLong(writer, "MaxHealthModifier", MaxHealthModifier);
            SaveManager.WriteLong(writer, "CurrentHealth", _currentHealth);
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "MaxHealthModifier":
                    MaxHealthModifier = (long)readValue;
                    break;
                case "CurrentHealth":
                    _currentHealth = (long)readValue;
                    break;
                default:
                    break;
            }
        }
    }
}