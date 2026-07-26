using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace PartnerService.DataAccess.Repositories
{
    public class EfRepository<T> : IRepository<T> where T: BaseEntity
    {
        private readonly DataContext _dataContext;

        public EfRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            throw new NotImplementedException();
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

    }
}
