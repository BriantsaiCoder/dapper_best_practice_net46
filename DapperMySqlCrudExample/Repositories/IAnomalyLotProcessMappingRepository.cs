using System.Collections.Generic;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>批號 Process Mapping Repository 介面</summary>
    public interface IAnomalyLotProcessMappingRepository
    {
        IEnumerable<AnomalyLotProcessMapping> GetAll();
        AnomalyLotProcessMapping GetById(long id);
        IEnumerable<AnomalyLotProcessMapping> GetByAnomalyLotId(long anomalyLotId);
        long Insert(AnomalyLotProcessMapping entity);
        bool Update(AnomalyLotProcessMapping entity);
        bool Delete(long id);
    }
}
