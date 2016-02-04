using System;
using System.Collections.Generic;
using System.Linq;

namespace com.LuminousVector.Events
{
	public class EventManager
	{
		private Dictionary<string, Action> eventDictionary;

		public void Init()
		{
			eventDictionary = new Dictionary<string, Action>();
		}

		public void AddListener(string eventName, Action listener)
		{
			if (eventDictionary == null)
				return;
			if (eventDictionary.ContainsKey(eventName))
				return;
			eventDictionary.Add(eventName, listener);
		}

		public void RemoveListener(string eventName)
		{
			if (eventDictionary == null)
				return;
			if (eventDictionary.ContainsKey(eventName))
				eventDictionary.Remove(eventName);
		}

		public void TriggerEvent(string eventName)
		{
			if (eventDictionary == null)
				return;
			Action action;
			eventDictionary.TryGetValue(eventName, out action);
			if (action == null)
				return;
			action.Invoke();
		}
	}
}
