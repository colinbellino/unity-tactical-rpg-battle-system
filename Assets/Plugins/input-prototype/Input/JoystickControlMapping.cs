using System;

namespace UnityEngine.InputNew
{
	[Serializable]
	public struct JoystickControlMapping
	{
		public int targetIndex;
		public Assets.Utilities.Range fromRange;
		public Assets.Utilities.Range toRange;
		public Assets.Utilities.Range interDeadZoneRange;
	}
}
