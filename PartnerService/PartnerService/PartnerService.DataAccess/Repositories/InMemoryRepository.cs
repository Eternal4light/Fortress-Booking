using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PartnerService.DataAccess.Repositories
{
    public class InMemoryRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly ConcurrentDictionary<Guid, T> _data;

        public InMemoryRepository(IEnumerable<T> data)
        {
            _data = new ConcurrentDictionary<Guid, T>(data.Select(e => new KeyValuePair<Guid, T>(e.Id, e)));
        }


        public Task<IReadOnlyCollection<T>> GetAllAsync()
        {
            return Task.FromResult((IReadOnlyCollection<T>)_data.Values);
        }

        public Task<T> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(T entity)
        {
            throw new NotImplementedException();
        }
    }
}
