using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>Unit Process Mapping Repository 介面</summary>
    public interface IAnomalyUnitProcessMappingRepository
    {
        IEnumerable<AnomalyUnitProcessMapping> GetAll();
        AnomalyUnitProcessMapping GetById(long id);
        IEnumerable<AnomalyUnitProcessMapping> GetByAnomalyUnitId(long anomalyUnitId);
        long Insert(AnomalyUnitProcessMapping entity, IDbTransaction transaction = null);
        bool Update(AnomalyUnitProcessMapping entity);
        bool Delete(long id);
    }
}
