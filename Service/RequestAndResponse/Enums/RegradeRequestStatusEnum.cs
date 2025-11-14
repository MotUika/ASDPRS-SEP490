using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.Enums
{
    public enum RegradeRequestStatusEnum
    {
        Pending,         // Sinh viên gửi yêu cầu, chờ giảng viên xem xét
        Approved,        // Giảng viên đồng ý phúc khảo và đã cập nhật điểm
        Rejected,        // Giảng viên từ chối yêu cầu phúc khảo
        Completed        // Đã xem và xử lý, nhưng không nhất thiết Approved/Rejected
    }
}
