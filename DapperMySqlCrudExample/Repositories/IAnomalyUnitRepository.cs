using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>異常 Unit 明細 Repository 介面</summary>
    public interface IAnomalyUnitRepository
    {
        IEnumerable<AnomalyUnit> GetAll();
        AnomalyUnit GetById(long id);
        IEnumerable<AnomalyUnit> GetByAnomalyTestItemId(long anomalyTestItemId);
        long Insert(AnomalyUnit entity, IDbTransaction transaction = null);
        bool Update(AnomalyUnit entity, IDbTransaction transaction = null);
        bool Delete(long id, IDbTransaction transaction = null);
    }
}
