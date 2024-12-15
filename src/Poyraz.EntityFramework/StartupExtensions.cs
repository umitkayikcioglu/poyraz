using Microsoft.Extensions.DependencyInjection;
using Poyraz.EntityFramework.Abstractions;
using Poyraz.EntityFramework.Services;
using System;

namespace Poyraz.EntityFramework
{
	public static class StartupExtensions
	{
		public static IServiceCollection AddRepositoryAndUnitOfWork(this IServiceCollection serviceCollection)
		{
			if (serviceCollection == null)
			{
				throw new ArgumentNullException(nameof(serviceCollection));
			}

			serviceCollection.AddScoped(typeof(IRepository<>), typeof(Repository<>));
			serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();

			return serviceCollection;
		}
	}
}
