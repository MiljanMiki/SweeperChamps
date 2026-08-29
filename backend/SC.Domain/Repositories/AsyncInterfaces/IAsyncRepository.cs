namespace SC.Domain.Repositories.AsyncInterfaces
{
    public interface IAsyncRepository<T> where T: class
    {
        Task<T?> GetAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task SaveChangesAsync();
    }
}
