using UnityEngine;

namespace RTSLockstep
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(MovementGroupHelper))]
	[RequireComponent(typeof(AttackGroupHelper))]
	[RequireComponent(typeof(EnvironmentHelper))]
	public class DefaultHelperSetup : MonoBehaviour
	{
		void Awake()
		{
			DestroyImmediate(this);
		}
	}
}