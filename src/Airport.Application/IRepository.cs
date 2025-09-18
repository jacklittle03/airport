namespace Airport.Application;

public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task RemoveAsync(Guid id);
    Task<IReadOnlyList<T>> ListAsync();
}
