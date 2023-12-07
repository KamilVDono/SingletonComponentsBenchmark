namespace SingletonComponentsBenchmark;

public class SingletonStorageFast
{
	readonly IBucket[] _buckets;

	public SingletonStorageFast(int capacity)
	{
		_buckets = new IBucket[capacity];
	}

	public static int Index<T>() where T : unmanaged, IComponent => Bucket<T>.Index;

	public bool Has<T>() where T : unmanaged, IComponent
	{
		var index = Bucket<T>.Index;
		return !ReferenceEquals(_buckets[index], null);
	}

	public ref T Value<T>() where T : unmanaged, IComponent
	{
		var index = Bucket<T>.Index;
		// By calling this we are certain that value exists so we don't need to handle nulls
#pragma warning disable 8602
#pragma warning disable 8600
		return ref ((Bucket<T>)_buckets[index]).value;
#pragma warning restore 8600
#pragma warning restore 8602
	}

	public void Add<T>(T value) where T : unmanaged, IComponent
	{
		var index = Bucket<T>.Index;
		if (_buckets[index] is Bucket<T> bucket)
		{
			bucket.value = value;
		}
		else
		{
			_buckets[index] = new Bucket<T>(value);
		}
	}

	public void Remove<T>() where T : unmanaged, IComponent
	{
		var index = Bucket<T>.Index;
		_buckets[index] = null;
	}

	interface IBucket {}

	static int _nextIndex;
	class Bucket<T> : IBucket where T : unmanaged, IComponent
	{
		public static int Index = _nextIndex++;
		public T value;
		public static T dummyRefReturn;

		public Bucket(T value)
		{
			this.value = value;
		}
	}
}
