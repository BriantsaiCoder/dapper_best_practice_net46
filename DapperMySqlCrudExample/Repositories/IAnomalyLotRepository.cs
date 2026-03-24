using System.Collections.Generic;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>異常批號 Repository 介面</summary>
    public interface IAnomalyLotRepository
    {
        IEnumerable<AnomalyLot> GetAll();
        AnomalyLot GetById(long id);
        IEnumerable<AnomalyLot> GetByLotsInfoId(int lotsInfoId);
        long Insert(AnomalyLot entity);
        bool Update(AnomalyLot entity);
        bool Delete(long id);
    }
}
