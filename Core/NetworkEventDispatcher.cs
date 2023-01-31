using System;
using System.Collections.Generic;

namespace Kalkatos.Network
{
	public abstract class NetworkEventDispatcher
	{
		protected Dictionary<byte, List<Action<object>>> eventCallbacks = new Dictionary<byte, List<Action<object>>>();

		public virtual void AddListener (byte key, Action<object> callback)
		{
			if (eventCallbacks.ContainsKey(key))
			{
				var list = eventCallbacks[key];
				if (!list.Contains(callback))
					list.Add(callback);
			}
			else
				eventCallbacks.Add(key, new List<Action<object>> { callback });
		}
		public virtual void RemoveListener (byte key, Action<object> callback) 
		{
			if (eventCallbacks.ContainsKey(key)) 
				eventCallbacks[key].Remove(callback);
		}

		protected void FireEvent (byte key, object param)
		{
			if (eventCallbacks.ContainsKey(key))
				foreach (var item in eventCallbacks[key])
					item.Invoke(param);
		}
	}
}