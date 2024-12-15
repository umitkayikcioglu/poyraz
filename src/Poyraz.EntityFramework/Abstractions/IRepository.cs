using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Poyraz.EntityFramework.Abstractions
{
	public interface IRepository<T> where T : IEntity
	{
		Task<T> SingleAsync(ISpecification<T> specification);
		Task<T> FindAsync(Expression<Func<T, bool>> predicate);
		Task<T> FindByIdAsync(object id);
		Task<T> FindByRowGuidAsync(Guid rowGuid);

		IQueryable<T> AsQueryable(Expression<Func<T, bool>> predicate = null);
		IQueryable<T> Find(ISpecification<T> specification = null);
		void Add(T entity);
		void AddRange(IEnumerable<T> entities);
		void Remove(T entity);
		void RemoveRange(IEnumerable<T> entities);
		void Update(T entity);

		bool Contains(ISpecification<T> specification = null);
		bool Contains(Expression<Func<T, bool>> predicate);

		int Count(ISpecification<T> specification = null);
		int CountExcludePagingParameter(ISpecification<T> specification = null);
		int Count(Expression<Func<T, bool>> predicate);
	}
}
