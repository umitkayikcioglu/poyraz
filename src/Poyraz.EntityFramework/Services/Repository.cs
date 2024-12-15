using Microsoft.EntityFrameworkCore;
using Poyraz.EntityFramework.Abstractions;
using Poyraz.EntityFramework.Specifications.Evaluators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Poyraz.EntityFramework.Services
{
	public class Repository<T> : IRepository<T> where T : class, IEntity
	{
		protected DbContext DBContext;
		internal DbSet<T> DBSet;
		public Repository(DbContext dbContext)
		{
			DBContext = dbContext;
			DBSet = dbContext.Set<T>();
		}
		public void Add(T entity)
		{
			DBSet.Add(entity);
		}

		public void AddRange(IEnumerable<T> entities)
		{
			DBSet.AddRange(entities);
		}

		public void Remove(T entity)
		{
			DBSet.Remove(entity);
		}

		public void RemoveRange(IEnumerable<T> entities)
		{
			DBSet.RemoveRange(entities);
		}

		public void Update(T entity)
		{
			DBSet.Attach(entity);
			DBContext.Entry(entity).State = EntityState.Modified;
		}


		public async Task<T> SingleAsync(ISpecification<T> specification)
		{
			return await ApplySpecification(specification).SingleAsync();
		}
		public async Task<T> FindAsync(Expression<Func<T, bool>> predicate)
		{
			return await DBSet.FirstOrDefaultAsync(predicate);
		}

		public async Task<T> FindByIdAsync(object id)
		{
			return await DBSet.FindAsync(id);
		}

		public async Task<T> FindByRowGuidAsync(Guid rowGuid)
		{
			if (typeof(IEntityWithExternalId).IsAssignableFrom(typeof(T)))
				return await DBSet.FirstOrDefaultAsync(f => ((IEntityWithExternalId)f).RowGuid == rowGuid);

			throw new InvalidOperationException("T must implement IEntityWithExternalId to use this method.");
		}

		public IQueryable<T> AsQueryable(Expression<Func<T, bool>> predicate = null)
		{
			if (predicate != null)
				return DBSet.Where(predicate);

			return DBSet.AsQueryable();
		}

		public IQueryable<T> Find(ISpecification<T> specification = null)
		{
			return ApplySpecification(specification);
		}

		public bool Contains(ISpecification<T> specification = null)
		{
			return Count(specification) > 0 ? true : false;
		}

		public bool Contains(Expression<Func<T, bool>> predicate)
		{
			return Count(predicate) > 0 ? true : false;
		}

		public int Count(ISpecification<T> specification = null)
		{
			return ApplySpecification(specification).Count();
		}

		public int CountExcludePagingParameter(ISpecification<T> specification = null)
		{
			specification.UndoPaging();
			return ApplySpecification(specification).Count();
		}

		public int Count(Expression<Func<T, bool>> predicate)
		{
			return DBSet.Where(predicate).Count();
		}

		private IQueryable<T> ApplySpecification(ISpecification<T> spec)
		{
			if (spec == null)
				return DBSet.AsQueryable();

			return SpecificationEvaluator<T>.GetQuery(DBSet.AsQueryable(), spec);
		}
	}
}
