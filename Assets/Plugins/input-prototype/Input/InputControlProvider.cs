using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.InputNew
{
	public abstract class InputControlProvider : IInputControlProvider
	{
		public List<InputControlData> controlDataList { get { return m_ControlDataList; } }
		public InputState state { get { return m_State; } }

		private List<InputControlData> m_ControlDataList;
		private List<InputControl> m_Controls;
		private InputState m_State;

		protected void SetControls(List<InputControlData> controls)
		{
			m_ControlDataList = controls;
			m_State = new InputState(this);
			m_Controls = new List<InputControl>(controlCount);
			for (int i = 0; i < controlCount; i++)
			{
				switch(controls[i].controlType)
				{
				case InputControlType.Button:
					m_Controls.Add(new ButtonInputControl(i, m_State));
					break;
				case InputControlType.AbsoluteAxis:
				case InputControlType.RelativeAxis:
					m_Controls.Add(new AxisInputControl(i, m_State));
					break;
				case InputControlType.Vector2:
					m_Controls.Add(new Vector2InputControl(i, m_State));
					break;
				case InputControlType.Vector3:
					m_Controls.Add(new Vector3InputControl(i, m_State));
					break;
				}
			}
		}

		public virtual bool ProcessEvent(InputEvent inputEvent)
		{
			lastEventTime = inputEvent.time;
			return false;
		}

		public InputControlData GetControlData(int index)
		{
			return controlDataList[index];
		}
		
		public int controlCount
		{
			get { return controlDataList.Count; }
		}
		
		public InputControl this[int index]
		{
			get { return m_Controls[index]; }
		}
		
		public InputControl this[string controlName]
		{
			get
			{
				for (var i = 0; i < controlDataList.Count; ++ i)
				{
					if (controlDataList[i].name == controlName)
						return this[i];
				}
				
				throw new KeyNotFoundException(controlName);
			}
		}

		public virtual string GetPrimarySourceName(int controlIndex, string buttonAxisFormattingString = "{0} & {1}")
		{
			return this[controlIndex].name;
		}
		
		protected void SetControlNameOverride(int controlIndex, string nameOverride)
		{
			InputControlData data = controlDataList[controlIndex];
			data.name = nameOverride;
			controlDataList[controlIndex] = data;
		}

		public float lastEventTime { get; protected set; }
	}
}
