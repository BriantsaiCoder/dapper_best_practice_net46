using System.Collections.Generic;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>異常測項明細 Repository 介面</summary>
    public interface IAnomalyTestItemRepository
    {
        IEnumerable<AnomalyTestItem> GetAll();
        AnomalyTestItem GetById(long id);
        IEnumerable<AnomalyTestItem> GetByAnomalyLotId(long anomalyLotId);
        long Insert(AnomalyTestItem entity);
        bool Update(AnomalyTestItem entity);
        bool Delete(long id);
    }
}
