using System.Collections.Concurrent;
using System.Linq;
using Airport.Application;

namespace Airport.Infrastructure;

public class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly ConcurrentDictionary<Guid, T> _store = new();
    private readonly Func<T, Guid> _idGetter;

    public InMemoryRepository(Func<T, Guid> idGetter)
    {
        _idGetter = idGetter;
    }

    public Task<T?> GetByIdAsync(Guid id)
        => Task.FromResult(_store.TryGetValue(id, out var value) ? value : null);

    public Task AddAsync(T entity)
    {
        _store[_idGetter(entity)] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity)
    {
        _store[_idGetter(entity)] = entity;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<T>> ListAsync()
        => Task.FromResult((IReadOnlyList<T>)_store.Values.ToList());
}
