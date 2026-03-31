using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>Site 測項統計值 Repository 介面</summary>
    public interface ISiteTestStatisticRepository
    {
        IEnumerable<SiteTestStatistic> GetAll();
        SiteTestStatistic GetById(long id);
        IEnumerable<SiteTestStatistic> GetByLotsInfoId(int lotsInfoId);
        IEnumerable<SiteTestStatistic> GetBySiteAndItem(uint siteId, string testItemName);
        long Insert(SiteTestStatistic entity, IDbTransaction transaction = null);
        bool Update(SiteTestStatistic entity, IDbTransaction transaction = null);
        bool Delete(long id, IDbTransaction transaction = null);
    }
}
