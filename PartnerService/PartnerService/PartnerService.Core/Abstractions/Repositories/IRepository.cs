using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace PartnerService.Core.Abstractions.Repositories
{
    public interface IRepository<T>
    {
            Task<IEnumerable<T>> GetAllAsync();

            Task<T> GetByIdAsync(Guid id);

            Task AddAsync(T entity);

            Task UpdateAsync(T entity);

            Task DeleteAsync(T entity);
        }
    }
