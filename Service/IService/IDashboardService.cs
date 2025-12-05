using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Response.Dashboard;

namespace Service.IService
{
    public interface IDashboardService
    {
        Task<BaseResponse<SemesterStatisticResponse>> GetSemesterStatisticsAsync(int academicYearId, int semesterId);
    }
}

