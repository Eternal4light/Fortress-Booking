using Microsoft.EntityFrameworkCore;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;

namespace PartnerService.DataAccess.Repositories
{
    /// <summary>
    /// Реализация репозитория поверх EF Core (PostgreSQL). Данные сохраняются в реальной
    /// базе данных и переживают перезапуск сервиса — в отличие от прежней in-memory версии.
    /// </summary>
    public class EfRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly DataContext _context;
        private readonly DbSet<T> _dbSet;

        public EfRepository(DataContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IReadOnlyCollection<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            return (await _dbSet.FindAsync(id))!;
        }

        public async Task AddAsync(T entity)
        {
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
