using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Response.Statistic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.IService
{
    public interface IStatisticsService
    {
        Task<BaseResponse<IEnumerable<AssignmentStatisticResponse>>>
            GetAssignmentStatisticsByClassAsync(int userId, int courseInstanceId);

        Task<BaseResponse<IEnumerable<ClassStatisticResponse>>>
            GetClassStatisticsByCourseAsync(int userId, int courseId);
    }
}
