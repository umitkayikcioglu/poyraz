using System;
using System.Threading;
using System.Threading.Tasks;

namespace Poyraz.EntityFramework.Abstractions
{
	public interface IUnitOfWork : IDisposable
	{
		Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

		IRepository<TEntity> Repository<TEntity>() where TEntity : IEntity;
		long? GetId<TEntity>(Guid? rowGuid) where TEntity : IEntityWithExternalId;
	}
}
