using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Poyraz.EntityFramework.Abstractions;
using Poyraz.EntityFramework.Services;
using System;

namespace Poyraz.EntityFramework
{
	public static class StartupExtensions
	{
		public static IServiceCollection AddUnitOfWork<TContext>(this IServiceCollection serviceCollection)
			where TContext : DbContext
		{
			if (serviceCollection == null)
			{
				throw new ArgumentNullException(nameof(serviceCollection));
			}

			//x serviceCollection.AddScoped(typeof(IRepository<>), typeof(Repository<TContext,>));
			serviceCollection.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();
			return serviceCollection;
		}
	}
}
