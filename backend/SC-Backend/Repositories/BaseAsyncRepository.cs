using Microsoft.EntityFrameworkCore;
using SC_Backend.DataContext;

namespace SC_Backend.Repositories
{
    public abstract class BaseAsyncRepository<T> : IAsyncRepository<T> where T:class
    {
        protected readonly ApplicationDbContext Context;
        protected readonly DbSet<T> DbSet;

        public BaseAsyncRepository(ApplicationDbContext context)
        {
            Context = context;
            DbSet = Context.Set<T>();
        }

        public virtual async Task<T?> GetAsync(int id)
        {
            return await DbSet.FindAsync(id);
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await DbSet.AsNoTracking().ToListAsync();
        }

        public virtual void Add(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            DbSet.Add(entity);
        }
        public virtual void Delete(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            DbSet.Remove(entity);
        }
        public virtual void Update(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            DbSet.Update(entity);
        }
        public async Task SaveChangesAsync()
        {
            await Context.SaveChangesAsync();
        }
        
    }
}
