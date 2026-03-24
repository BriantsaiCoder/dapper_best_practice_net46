using System.Collections.Generic;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>好批批號記錄 Repository 介面</summary>
    public interface IGoodLotRepository
    {
        IEnumerable<GoodLot> GetAll();
        GoodLot GetById(long id);
        IEnumerable<GoodLot> GetByLotsInfoId(int lotsInfoId);
        long Insert(GoodLot entity);
        bool Update(GoodLot entity);
        bool Delete(long id);
    }
}
