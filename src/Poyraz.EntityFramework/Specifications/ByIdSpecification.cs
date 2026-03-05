using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Poyraz.EntityFramework.Abstractions;

namespace Poyraz.EntityFramework.Specifications
{
	public class ByIdSpecification<T> : Specification<T> where T : IEntity
	{
		public ByIdSpecification(long id) : base(x => x.Id == id)
		{
		}
		public ByIdSpecification(long id, params Expression<Func<T, object>>[] includes) : this(id)
		{
			Includes.AddRange(includes);
		}
	}
}
