using System.Collections.Generic;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>異常 Unit 明細 Repository 介面</summary>
    public interface IAnomalyUnitRepository
    {
        IEnumerable<AnomalyUnit> GetAll();
        AnomalyUnit GetById(long id);
        IEnumerable<AnomalyUnit> GetByAnomalyTestItemId(long anomalyTestItemId);
        long Insert(AnomalyUnit entity);
        bool Update(AnomalyUnit entity);
        bool Delete(long id);
    }
}
