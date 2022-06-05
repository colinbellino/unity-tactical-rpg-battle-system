using System.Collections.Generic;

namespace UnityEngine.InputNew
{
	public interface IInputControlProvider
	{
		List<InputControlData> controlDataList { get; }
		InputControl this[int index] { get; }
	}
}
