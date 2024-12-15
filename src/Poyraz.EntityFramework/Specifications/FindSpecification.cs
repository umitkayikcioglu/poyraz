using Poyraz.EntityFramework.Abstractions;
using System;
using System.Linq.Expressions;

namespace Poyraz.EntityFramework.Specifications
{
	public class FindSpecification<TEntity> : Specification<TEntity> where TEntity : IEntity
	{
		public FindSpecification(Expression<Func<TEntity, bool>>? criteria) : base(criteria)
		{
		}
	}
}
