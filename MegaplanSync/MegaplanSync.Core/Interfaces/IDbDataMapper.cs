using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Models.Department;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaplanSync.Core.Interfaces
{
    public interface IDbDataMapper
    {
        List<Deal> MapDeals(DataTable dataTable);
        List<Department> MapDepartments(DataTable dataTable);
    }
}
