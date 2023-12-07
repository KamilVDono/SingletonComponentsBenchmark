namespace SingletonComponentsBenchmark;

public class SingletonStorageSlow
{
	readonly Dictionary<Type, IComponent> _components;

	public SingletonStorageSlow(int capacity)
	{
		_components = new(capacity);
	}

	public bool Has<T>() where T : unmanaged, IComponent
	{
		return _components.ContainsKey(typeof(T));
	}

	public T Value<T>() where T : unmanaged, IComponent
	{
		if (_components.TryGetValue(typeof(T), out var value))
		{
			return (T)value;
		}
		return default;
	}

	public void Add<T>(T value) where T : unmanaged, IComponent
	{
		_components[typeof(T)] = value;
	}

	public void Remove<T>() where T : unmanaged, IComponent
	{
		_components.Remove(typeof(T));
	}
}
