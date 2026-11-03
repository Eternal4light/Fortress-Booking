using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            return Task.FromResult((IReadOnlyCollection<T>)_data.Values.ToList());
        }

        public Task<T> GetByIdAsync(Guid id)
        {
            _data.TryGetValue(id, out var entity);
            return Task.FromResult(entity!);
        }

        public Task AddAsync(T entity)
        {
            _data[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(T entity)
        {
            _data[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity)
        {
            _data.TryRemove(entity.Id, out _);
            return Task.CompletedTask;
        }
    }
}
