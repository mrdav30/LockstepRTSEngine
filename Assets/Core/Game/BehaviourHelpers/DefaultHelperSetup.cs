using RTSLockstep.Grouping;
using RTSLockstep.Environment;
using UnityEngine;

namespace RTSLockstep.BehaviourHelpers
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MovementGroupHelper))]
    [RequireComponent(typeof(AttackGroupHelper))]
    [RequireComponent(typeof(HarvestGroupHelper))]
    [RequireComponent(typeof(ConstructionGroupHelper))]
    [RequireComponent(typeof(EnvironmentHelper))]
    public class DefaultHelperSetup : MonoBehaviour
    {
        void Awake()
        {
            DestroyImmediate(this);
        }
    }
}