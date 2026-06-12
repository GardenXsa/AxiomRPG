namespace AxiomRPG.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> QueryAsync(Func<T, bool> predicate);
    Task SaveAsync(T entity);
    Task DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}
