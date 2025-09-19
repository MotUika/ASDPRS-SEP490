using DataAccessLayer.IBaseDAO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.BaseDAO
{
    public class BaseDAO<T> : IBaseDAO<T> where T : class
    {
        private readonly DbContext _dbContext;
        public BaseDAO(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<T> GetByIdAsync(int id)
        {
            if (id == null || id <= 0)
            {
                throw new ArgumentNullException($"id {id} not found");
            }
            var entity = await _dbContext.Set<T>().FindAsync(id);

            if (entity == null)
            {
                throw new ArgumentNullException($"Entity of type {typeof(T).Name} with id {id} not found");
            }
            return entity;
        }
        public async Task<T> AddAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            await _dbContext.Set<T>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }
        public async Task<T> UpdateAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            _dbContext.Entry(entity).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
            return entity;
        }
        public async Task<T> DeleteAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            _dbContext.Set<T>().Remove(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbContext.Set<T>().ToListAsync();

        }

        public async Task<T> GetByStringIdAsync(string id)
        {
            var entity = await _dbContext.Set<T>().FindAsync(id);
            if (entity == null)
            {
                throw new ArgumentNullException($"Entity of type {typeof(T).Name} with id {id} not found");
            }
            return entity;
        }
    }
}
