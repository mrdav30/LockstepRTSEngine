using RTSLockstep.Player.Commands;
using System.Linq;

namespace RTSLockstep.BehaviourHelpers
{
    public static class BehaviourHelperManager
    {
        private static ILockstepEventsHandler[] Helpers { get; set; }

        public static void Initialize(ILockstepEventsHandler[] helpers)
        {
            Helpers = helpers;
            foreach (ILockstepEventsHandler helper in Helpers)
            {
                helper.EarlyInitialize();
            }
        }

        public static void LateInitialize()
        {
            foreach (ILockstepEventsHandler helper in Helpers)
            {
                helper.Initialize();
            }
            foreach (ILockstepEventsHandler helper in Helpers)
            {
                helper.LateInitialize();
            }
        }

        // allow behavior helper initialization on demand
        public static void InitializeOnDemand(ILockstepEventsHandler helper)
        {
            Helpers = (Helpers ?? Enumerable.Empty<ILockstepEventsHandler>()).Concat(Enumerable.Repeat(helper, 1)).ToArray();
            helper.Initialize();
        }

        public static void GameStart()
        {
            foreach (var helper in Helpers)
            {
                helper.GameStart();
            }
        }

        public static void Simulate()
        {
            foreach (ILockstepEventsHandler helper in Helpers)
            {
                helper.Simulate();
            }
        }

        public static void LateSimulate()
        {
            foreach (ILockstepEventsHandler helper in Helpers)
            {
                helper.LateSimulate();
            }
        }

        public static void Execute(Command com)
        {
            foreach (ILockstepEventsHandler helper in Helpers)
            {
                if (helper.GetListenInput() == com.InputCode)
                {
                    helper.GlobalExecute(com);
                }
                helper.RawExecute(com);
            }
        }

        public static void Visualize()
        {
            foreach (ILockstepEventsHandler helper in Helpers)
            {
                helper.Visualize();
            }
        }

        public static void LateVisualize()
        {
            foreach (var helper in Helpers)
            {
                helper.LateVisualize();
            }
        }

        public static void Deactivate()
        {
            foreach (ILockstepEventsHandler helper in Helpers)
            {
                helper.Deactivate();
            }
        }

        public static THelper GetHelper<THelper>() where THelper : ILockstepEventsHandler
        {
            foreach (var helper in Helpers)
            {
                if (helper is THelper)
                {
                    return (THelper)helper;
                }
            }

            return default;
        }
    }
}