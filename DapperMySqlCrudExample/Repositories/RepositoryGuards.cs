using System;
using DapperMySqlCrudExample.Infrastructure;

namespace DapperMySqlCrudExample.Repositories
{
    internal static class RepositoryGuards
    {
        internal static IDbConnectionFactory RequireFactory(
            IDbConnectionFactory factory,
            string parameterName
        )
        {
            if (factory == null)
                throw new ArgumentNullException(parameterName);

            return factory;
        }

        internal static TEntity RequireEntity<TEntity>(TEntity entity, string parameterName)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(parameterName);

            return entity;
        }

        internal static void ValidatePaging(int offset, int limit)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(offset),
                    offset,
                    "offset 不可小於 0。"
                );

            if (limit <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(limit),
                    limit,
                    "limit 必須大於 0。"
                );
        }
    }
}
