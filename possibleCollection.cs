internal interface IPossibleCollection<T>
{
    public static readonly IPossibleCollection<T> Empty = new EmptyCollection<T>();
    public IEnumerable<T> GetCollection { get; }
    public bool IsCollection { get; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns>A collection if not already</returns>
    public IPossibleCollection<T> Add(T item);

    public T GetSingleValue { get; }

    public void ForEach(Action<T> action);
}

internal class EmptyCollection<T> : IPossibleCollection<T>
{
    public IEnumerable<T> GetCollection
    {
        get
        {
            yield break;
        }
    }

    public bool IsCollection => false;

    public T GetSingleValue => throw new Exception("There is no value");

    public IPossibleCollection<T> Add(T item)
    {
        return new NonCollection<T>(item);
    }

    public void ForEach(Action<T> action)
    {
        return;
    }
}
internal class NonCollection<T> : IPossibleCollection<T>
{
    public bool IsCollection => false;
    public readonly T value;
    IEnumerable<T> IPossibleCollection<T>.GetCollection
    {
        get
        {
            yield return value;
        }
    }


    public IPossibleCollection<T> Add(T item)
    {
        return new Collection<T>(new() { value, item });
    }

    public void ForEach(Action<T> action)
    {
        action.Invoke(value);
    }

    public T GetSingleValue => value;


    public NonCollection(T value)
    {
        this.value = value;
    }
}

internal class Collection<T> : IPossibleCollection<T>
{
    public bool IsCollection => true;
    private readonly List<T> values;
    public T GetSingleValue => throw new Exception("Cannot get single value, there are multible entrys");

    public Collection(List<T> values)
    {
        this.values = values;
    }
    public IPossibleCollection<T> Add(T item)
    {
        values.Add(item);
        return this;
    }

    public void ForEach(Action<T> action)
    {
        foreach (var item in values)
        {
            action.Invoke(item);
        }
    }

    IEnumerable<T> IPossibleCollection<T>.GetCollection => values;

}