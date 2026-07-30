using Microsoft.EntityFrameworkCore;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace PartnerService.DataAccess.Repositories
{
    public class EfRepository<T>(DataContext context) : IRepository<T> where T: BaseEntity
    {
        private readonly DataContext _dataContext;

        protected virtual IQueryable<T> ApplyIncludes(IQueryable<T> query) => query;

        protected readonly DataContext _context = context;

        protected readonly DbSet<T> _dbSet = context.Set<T>();

        public async Task<IReadOnlyCollection<T>> GetAll(bool withIncludes = false, CancellationToken ct = default)
        {
            IQueryable<T> query = _dbSet;

        if (withIncludes)
            query = ApplyIncludes(query);

        return await query.ToListAsync(ct);
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task AddAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(T entity)
        {
            throw new NotImplementedException();
        }

        Task<IReadOnlyCollection<T>> IRepository<T>.GetAllAsync()
        {
            throw new NotImplementedException();
        }
    }
}
