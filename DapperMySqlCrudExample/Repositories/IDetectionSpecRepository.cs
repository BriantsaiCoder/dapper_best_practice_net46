using System.Collections.Generic;
using DapperMySqlCrudExample.Models;

namespace DapperMySqlCrudExample.Repositories
{
    /// <summary>Spec 規格 Repository 介面</summary>
    public interface IDetectionSpecRepository
    {
        IEnumerable<DetectionSpec> GetAll();
        DetectionSpec GetById(long id);
        IEnumerable<DetectionSpec> GetByProgramAndMethod(string program, byte detectionMethodId);
        long Insert(DetectionSpec entity);
        bool Update(DetectionSpec entity);
        bool Delete(long id);
    }
}
