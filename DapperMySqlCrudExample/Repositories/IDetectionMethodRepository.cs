using System.Collections.Generic;
using System.Data;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>偵測方法 Repository 介面</summary>
    public interface IDetectionMethodRepository
    {
        IEnumerable<DetectionMethod> GetAll();
        DetectionMethod GetById(byte id);
        DetectionMethod GetByCode(string methodCode);
        byte Insert(DetectionMethod entity, IDbTransaction transaction = null);
        bool Update(DetectionMethod entity, IDbTransaction transaction = null);
        bool Delete(byte id, IDbTransaction transaction = null);
    }
}
