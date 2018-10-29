using RTSLockstep.Data;

namespace RTSLockstep
{
	public class AttackGroupHelper : BehaviourHelper
	{
		public override ushort ListenInput {
			get {
                return AbilityDataItem.FindInterfacer(typeof (Attack)).ListenInputID;
			}
		}
			
		protected override void OnExecute (Command com)
		{
            MovementGroupHelper.StaticExecute (com);
		}
	}
}