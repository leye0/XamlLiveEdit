using System;
using System.Collections.Generic;

namespace Mesharp
{
	public class GenericsDictionary<KeyType> where KeyType : class
	{
		private Dictionary<KeyType, object> _dict = new Dictionary<KeyType, object>();

		public void Add<T> (KeyType key, T value) where T : class
		{
			if (!_dict.ContainsKey (key))
			{
				_dict.Add(key, new List<T>());
			}

	        (_dict[key] as List<T>).Add(value);
	    }

		public GenericsDictionary<KeyType> AddAndReturn<T> (KeyType key, T value) where T : class
		{
			if (!_dict.ContainsKey (key))
			{
				_dict.Add(key, new List<T>());
			}

	        (_dict[key] as List<T>).Add(value);

	        return this;
	    }

		public T AddAndGet<T> (KeyType key, T value) where T : class, new()
		{
			if (!_dict.ContainsKey (key))
			{
				_dict.Add(key, new List<T>());
			}

	        (_dict[key] as List<T>).Add(value);

	        return value;
	    }

		public bool ContainsItem(KeyType key)
	    {
	        return _dict.ContainsKey(key);
	    }

		public List<T> GetValue<T>(KeyType key) where T : class
	    {
			return _dict[key] as List<T>;
	    }

		public object GetObject(KeyType key)
	    {
			return _dict[key];
	    }
	}

	public class GenericsDictionaryGuid
	{
		private Dictionary<Guid, object> _dict = new Dictionary<Guid, object>();

		public void Add<T> (Guid key, T value) where T : class
		{
			if (!_dict.ContainsKey (key))
			{
				_dict.Add(key, new List<T>());
			}

	        (_dict[key] as List<T>).Add(value);
	    }

		public GenericsDictionaryGuid AddAndReturn<T> (Guid key, T value) where T : class
		{
			if (!_dict.ContainsKey (key))
			{
				_dict.Add(key, new List<T>());
			}

	        (_dict[key] as List<T>).Add(value);

	        return this;
	    }

		public T AddAndGet<T> (Guid key, T value) where T : class, new()
		{
			if (!_dict.ContainsKey (key))
			{
				_dict.Add(key, new List<T>());
			}

	        (_dict[key] as List<T>).Add(value);

	        return value;
	    }

		public bool ContainsItem(Guid key)
	    {
	        return _dict.ContainsKey(key);
	    }

		public List<T> GetValue<T>(Guid key) where T : class
	    {
			return _dict[key] as List<T>;
	    }

		public object GetObject(Guid key)
	    {
			return _dict[key];
	    }
	}
}