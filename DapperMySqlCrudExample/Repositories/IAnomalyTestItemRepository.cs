using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>異常測項明細 Repository 介面</summary>
    public interface IAnomalyTestItemRepository
    {
        IEnumerable<AnomalyTestItem> GetAll();
        AnomalyTestItem GetById(long id);
        IEnumerable<AnomalyTestItem> GetByAnomalyLotId(long anomalyLotId);
        long Insert(AnomalyTestItem entity, IDbTransaction transaction = null);
        bool Update(AnomalyTestItem entity, IDbTransaction transaction = null);
        bool Delete(long id, IDbTransaction transaction = null);
    }
}
