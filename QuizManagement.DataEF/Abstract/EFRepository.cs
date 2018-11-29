using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using QuizManagement.Data.Interfaces;
using QuizManagement.DataEF.Connector;
using QuizManagement.Infrastructure.Interfaces;
using QuizManagement.Infrastructure.SharedKernel;
using QuizManagement.Utilities.Extensions;

namespace QuizManagement.DataEF.Abstract
{
    public class EFRepository<T, K> : IRepository<T, K>, IDisposable where T : DomainEntity<K>
    {
        private readonly AppDbContext _context;

        public EFRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Add(T entity)
        {
            _context.Add(entity);
        }

        public void Dispose()
        {
            if (_context != null)
            {
                _context.Dispose();
            }
        }

        public IQueryable<T> FindAll(params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> items = _context.Set<T>();
            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    items = items.Include(includeProperty);
                }
            }

            return items;
        }

        public IQueryable<T> FindAll(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> items = _context.Set<T>();
            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    items = items.Include(includeProperty);
                }
            }

            return items.Where(predicate);
        }

        public T FindById(K id, params Expression<Func<T, object>>[] includeProperties)
        {
            return FindAll(includeProperties).SingleOrDefault(x => x.Id.Equals(id));
        }

        public T FindSingle(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties)
        {
            return FindAll(includeProperties).SingleOrDefault(predicate);
        }

        public void Remove(T entity)
        {
            _context.Set<T>().Remove(entity);
        }

        public void Remove(K id)
        {
            Remove(FindById(id));
        }

        public void RemoveMultiple(List<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
        }

        public T Update(T entity)
        {
            var dbEntity = _context.Set<T>().AsNoTracking().Single(p => p.Id.Equals(entity.Id));
            var databaseEntry = _context.Entry(dbEntity);
            var inputEntry = _context.Entry(entity);

            //no items mentioned, so find out the updated entries

            IEnumerable<string> dateProperties = typeof(IDateTracking).GetPublicProperties().Select(x => x.Name);

            var allProperties = databaseEntry.Metadata.GetProperties()
                .Where(x => !dateProperties.Contains(x.Name));

            foreach (var property in allProperties)
            {
                var proposedValue = inputEntry.Property(property.Name).CurrentValue;
                var originalValue = databaseEntry.Property(property.Name).OriginalValue;

                if (proposedValue != null && !proposedValue.Equals(originalValue))
                {
                    databaseEntry.Property(property.Name).IsModified = true;
                    databaseEntry.Property(property.Name).CurrentValue = proposedValue;
                }
            }

            var result = _context.Set<T>().Update(dbEntity);
            return result.Entity;
        }
    }
}