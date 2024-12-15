using Microsoft.EntityFrameworkCore;
using Poyraz.EntityFramework.Abstractions;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Poyraz.EntityFramework.Services
{
	internal sealed class UnitOfWork<TContext> : IUnitOfWork
		where TContext : DbContext
	{
		private readonly TContext _dbContext;
		private Hashtable _repositories;

		public UnitOfWork(TContext dbContext)
		{
			_dbContext = dbContext;
		}

		public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			return _dbContext.SaveChangesAsync(cancellationToken);
		}

		public IRepository<TEntity> Repository<TEntity>() where TEntity : IEntity
		{
			if (_repositories == null)
				_repositories = new Hashtable();

			var type = typeof(TEntity).Name;

			if (!_repositories.ContainsKey(type))
			{
				var repositoryType = typeof(Repository<,>);
				var repositoryInstance = Activator.CreateInstance(
					repositoryType.MakeGenericType(typeof(TContext), typeof(TEntity)),
					_dbContext
				);

				_repositories.Add(type, repositoryInstance);
			}

			return (IRepository<TEntity>)_repositories[type];
		}

		public long? GetId<TEntity>(Guid? rowGuid) where TEntity : IEntityWithExternalId
		{
			if (rowGuid == null || rowGuid == Guid.Empty)
				return default;

			var record = Repository<TEntity>().FindByRowGuidAsync(rowGuid.Value);
			record.Wait();
			return record.Result?.Id;
		}

		public void Dispose()
		{
			_dbContext.Dispose();
		}
	}
}
