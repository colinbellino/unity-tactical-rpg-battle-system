using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.InputNew
{
	public interface IInputConsumer
	{
		string name { get; }
		IList<IInputConsumer> children { get; }
		bool isStack { get; set; }
		ProcessInputDelegate processInput { get; set; }
		FrameDelegate beginFrame { get; set; }
		FrameDelegate endFrame { get; set; }
	}

	public static class InputConsumerExtensions
	{
		public static void AddChild(this IInputConsumer consumer, ProcessInputDelegate processInput, string name = "")
		{
			consumer.children.Add(new InputEventTree
			{
				name = name
				, processInput = processInput
			});
		}
	}
}

