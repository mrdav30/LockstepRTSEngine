using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Agents;
using RTSLockstep.Data;
using RTSLockstep.Player.Commands;
using RTSLockstep.Utility;
using System;
using System.Collections.Generic;

namespace RTSLockstep.Abilities
{
    public class AbilityManager
    {
        static FastList<ActiveAbility> setupActives = new FastList<ActiveAbility>();

        public Ability[] Abilities { get; private set; }
        public ActiveAbility[] ActiveAbilitys { get; private set; }
        public readonly FastList<AbilityDataItem> Interfacers = new FastList<AbilityDataItem>();

        public void Setup(LSAgent agent)
        {
            setupActives.FastClear();
            Abilities = agent.AttachedAbilities;
            for (int i = 0; i < Abilities.Length; i++)
            {
                Ability abil = Abilities[i];

                ActiveAbility activeAbil = abil as ActiveAbility;
                if (activeAbil.IsNotNull())
                {
                    setupActives.Add(activeAbil);
                }
            }

            ActiveAbilitys = setupActives.ToArray();

            for (int i = 0; i < Abilities.Length; i++)
            {
                Abilities[i].Setup(agent, i);
            }
            for (int i = 0; i < Abilities.Length; i++)
            {
                Abilities[i].LateSetup();
            }
            for (int i = 0; i < ActiveAbilitys.Length; i++)
            {
                if (ActiveAbilitys[i].Data.IsNotNull())

                    Interfacers.Add(ActiveAbilitys[i].Data);
            }
        }

        public void Initialize()
        {
            for (int i = 0; i < Abilities.Length; i++)
            {
                Abilities[i].Initialize();
            }
        }

        public void Simulate()
        {
            for (int i = 0; i < Abilities.Length; i++)
            {
                Abilities[i].Simulate();
            }
        }

        public void LateSimulate()
        {
            for (int i = 0; i < Abilities.Length; i++)
            {
                Abilities[i].LateSimulate();
            }
        }

        public void Visualize()
        {
            for (int i = 0; i < Abilities.Length; i++)
            {
                Abilities[i].Visualize();
            }
        }
        public void LateVisualize()
        {
            for (int i = 0; i < Abilities.Length; i++)
            {
                Abilities[i].LateVisualize();
            }
        }
        public void Execute(Command com)
        {
            for (int k = 0; k < ActiveAbilitys.Length; k++)
            {
                ActiveAbility abil = ActiveAbilitys[k];
                if (abil.ListenInput == com.InputCode)
                {
                    abil.Execute(com);
                }
                if (abil.DoRawExecute)
                {
                    abil.RawExecute(com);
                }
            }
        }

        public bool CheckCasting()
        {
            for (var k = 0; k < Abilities.Length; k++)
            {
                if (Abilities[k].IsCasting)
                {
                    return true;
                }
            }
            return false;
        }

        public bool CheckFocus()
        {
            for (var k = 0; k < Abilities.Length; k++)
            {
                if (Abilities[k].IsFocused)
                {
                    return true;
                }
            }
            return false;
        }

        public void StopCast(int exception)
        {
            for (var k = 0; k < Abilities.Length; k++)
            {
                if (k != exception)
                {
                    Abilities[k].StopCast();
                }
            }
        }

        public void Deactivate()
        {
            for (var k = 0; k < Abilities.Length; k++)
            {
                Abilities[k].Deactivate();
            }
        }

        public Ability GetAbilityWithInput(string inputCode)
        {
            //Linear search for first ability with inputCode
            for (int i = 0; i < ActiveAbilitys.Length; i++)
            {
                var abil = ActiveAbilitys[i];
                if (abil.Data.ListenInputCode == inputCode)
                {
                    return abil;
                }
            }

            return null;
        }
        public Ability GetAbilityWithInput(int inputID)
        {
            //Linear search for first ability with inputID
            for (int i = 0; i < ActiveAbilitys.Length; i++)
            {
                var abil = ActiveAbilitys[i];
                if (abil.Data.ListenInputID == inputID)
                    return abil;
            }
            return null;
        }

        public Ability GetAbilityAny(Type type)
        {
            for (var k = 0; k < Abilities.Length; k++)
            {
                var ability = Abilities[k];
                Type abilType = ability.GetType();
                if (abilType == type || abilType.IsSubclassOf(type))
                {
                    return ability;
                }
            }
            return null;
        }
        public Ability GetAbility(string name)
        {
            if (Abilities.IsNotNull())
            {
                for (var k = 0; k < Abilities.Length; k++)
                {
                    var ability = Abilities[k];
                    if (ability.Data != null)
                    {
                        if (ability.Data.Name == name)
                        {
                            return ability;
                        }
                    }
                }
            }

            return null;
        }
        public T GetAbility<T>() where T : Ability
        {
            if (Abilities.IsNotNull())
            {
                for (var k = 0; k < Abilities.Length; k++)
                {
                    var ability = Abilities[k] as T;
                    if (ReferenceEquals(ability, null) == false)
                    {
                        return ability;
                    }
                }
            }

            return null;
        }
        public Ability GetAbilityAny<T>()
        {
            for (var k = 0; k < Abilities.Length; k++)
            {
                Ability abil = Abilities[k];
                if (abil is T)
                {
                    return abil;
                }
            }
            return null;
        }

        public IEnumerable<Ability> GetAbilitiesAny<T>()
        {
            for (var k = 0; k < Abilities.Length; k++)
            {
                var abil = Abilities[k];
                if (abil is T)
                    yield return abil;
            }
        }
    }
}