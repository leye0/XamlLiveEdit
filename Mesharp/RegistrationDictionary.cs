using System;
using System.Collections.Generic;

namespace Mesharp
{
	public class RegistrationDictionary
	{
		private Dictionary<Type, object> _dict = new Dictionary<Type, object>();

		public void Add<T> (Type key, T value) where T : class
		{
			if (!_dict.ContainsKey (key))
			{
				_dict.Add(key, new List<T>());
			}

	        (_dict[key] as List<T>).Add(value);
	    }

		public bool ContainsRegistration(Type key)
	    {
	        return _dict.ContainsKey(key);
	    }

		public List<T> GetValue<T>(Type key) where T : class
	    {
			return _dict[key] as List<T>;
	    }

		public object GetObject(Type key)
	    {
			return _dict[key];
	    }
	}
}