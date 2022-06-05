using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.InputNew
{
	struct NativeInputEvent
	{
		public int deviceIndex;
		public NativeEventType eventType;
		public float time;
	}
}
