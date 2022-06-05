using System;
using System.Collections.Generic;

namespace UnityEngine.InputNew
{
	[Serializable]
	public class InputControlDescriptor
	{
		public int controlIndex;
		public SerializableType deviceType;

		public virtual InputControlDescriptor Clone()
		{
			var clone = (InputControlDescriptor) Activator.CreateInstance(GetType());
			clone.controlIndex = controlIndex;
			clone.deviceType = new SerializableType(deviceType);
			return clone;
		}
		
		public override string ToString()
		{
			return string.Format( "(device:{0}, control:{1})", deviceType.Name, controlIndex );
		}
		
		public void ExtractDeviceTypeAndControlIndex(Dictionary<Type, List<int>> controlIndicesPerDeviceType)
		{
			List<int> entries;
			if (!controlIndicesPerDeviceType.TryGetValue(deviceType, out entries))
			{
				entries = new List<int>();
				controlIndicesPerDeviceType[deviceType] = entries;
			}
			
			entries.Add(controlIndex);
		}
	}
}
