using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.InputNew
{
	public class PlayerHandle
	{
		public readonly int index;
		public List<PlayerDeviceAssignment> assignments = new List<PlayerDeviceAssignment>();
		public List<ActionMapInput> maps = new List<ActionMapInput>();

		private bool m_Global = false;
		private InputEventTree treeNode { get; set; }

		public delegate void ChangeEvent();
		public static ChangeEvent onChange;

		IInputConsumer currentInputConsumer
		{
			get
			{
				return m_Global ? InputSystem.globalPlayers : InputSystem.assignedPlayers;
			}
		}

		internal PlayerHandle(int index)
		{
			this.index = index;

			treeNode = new InputEventTree
			{
				name = "Player "+index,
				processInput = ProcessEvent,
				beginFrame = BeginFrameEvent,
				endFrame = EndFrameEvent
			};
			currentInputConsumer.children.Add(treeNode);

			if (onChange != null)
				onChange.Invoke();
		}

		public void Destroy()
		{
			foreach (var map in maps)
				map.active = false;

			for (int i = assignments.Count - 1; i >= 0; i--)
				assignments[i].Unassign();
			
			currentInputConsumer.children.Remove(treeNode);
			treeNode = null;

			PlayerHandleManager.RemovePlayerHandle(this);
			if (onChange != null)
				onChange.Invoke();
		}

		public bool global
		{
			get { return m_Global; }
			set
			{
				if (value == m_Global)
					return;

				// Note: value of m_Global changes what currentInputConsumer is.
				currentInputConsumer.children.Remove(treeNode);
				m_Global = value;
				currentInputConsumer.children.Add(treeNode);

				if (onChange != null)
					onChange.Invoke();
			}
		}

		public T GetActions<T>() where T : ActionMapInput
		{
			// If already contains ActionMapInput if this type, return that.
			for (int i = 0; i < maps.Count; i++)
				if (maps[i].GetType() == typeof(T))
					return (T)maps[i];
			return null;
		}

		public ActionMapInput GetActions(ActionMap actionMap)
		{
			// If already contains ActionMapInput based on this ActionMap, return that.
			for (int i = 0; i < maps.Count; i++)
				if (maps[i].actionMap == actionMap)
					return maps[i];
			return null;
		}

		public bool AssignDevice(InputDevice device, bool assign)
		{
			if (assign)
			{
				if (device.assignment != null)
				{
					// If already assigned to this player, accept as success. Otherwise, fail.
					if (device.assignment.player == this)
						return true;
					else
						return false;
				}

				var assignment = new PlayerDeviceAssignment(this, device);
				assignment.Assign();

				return true;
			}
			else
			{
				if (device.assignment.player == this)
				{
					device.assignment.Unassign();
					return true;
				}
				return false;
			}
		}

		bool ProcessEvent(InputEvent inputEvent)
		{
			if (!global && (inputEvent.device.assignment == null || inputEvent.device.assignment.player != this))
				return false;

			for (int i = 0; i < maps.Count; i++)
			{
				if (maps[i].active)
				{
					if (ProcessEventInMap(maps[i], inputEvent) || maps[i].blockSubsequent)
						return true;
				}
			}

			return false;
		}

		bool ProcessEventInMap(ActionMapInput map, InputEvent inputEvent)
		{
			if (map.ProcessEvent(inputEvent))
				return true;

			if (map.CurrentlyUsesDevice(inputEvent.device))
				return false;

			if (!map.TryInitializeWithDevices(GetApplicableDevices()))
				return false;

			// When changing control scheme, we do not want to init control scheme to device states
			// like we normally want, so do a hard reset here, before processing the new event.
			map.Reset(false);

			return map.ProcessEvent(inputEvent);
		}

		public IEnumerable<InputDevice> GetApplicableDevices()
		{
			if (global)
				return InputSystem.devices.Where(e => e.assignment == null);
			else
				return assignments.Select(e => e.device);
		}

		void BeginFrameEvent()
		{
			for (int i = 0; i < maps.Count; i++)
			{
				if (maps[i].active)
					maps[i].BeginFrame();
			}
		}
		
		void EndFrameEvent()
		{
			for (int i = 0; i < maps.Count; i++)
			{
				if (maps[i].active)
					maps[i].EndFrame();
			}
		}
	}
	
}
