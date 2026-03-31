using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>異常批號 Repository 介面</summary>
    public interface IAnomalyLotRepository
    {
        IEnumerable<AnomalyLot> GetAll();
        AnomalyLot GetById(long id);
        IEnumerable<AnomalyLot> GetByLotsInfoId(int lotsInfoId);
        long Insert(AnomalyLot entity, IDbTransaction transaction = null);
        bool Update(AnomalyLot entity, IDbTransaction transaction = null);
        bool Delete(long id, IDbTransaction transaction = null);
        IEnumerable<AnomalyLot> GetPaged(int offset, int limit);
        int GetCount();
        bool Exists(long id);
    }
}
