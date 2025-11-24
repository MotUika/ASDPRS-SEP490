using Service.RequestAndResponse.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.RequestAndResponse.BaseResponse
{
    public class BaseResponse<T>
    {
        public string Message { get; set; } = "Sucessfull";
        public StatusCodeEnum StatusCode { get; set; }
        public T Data { get; set; }
        public List<ErrorDetail> Errors { get; set; } = new List<ErrorDetail>();
        // M?I: C?nh báo không block (ch? thông báo)
        public List<ErrorDetail> Warnings { get; set; } = new List<ErrorDetail>();

        public BaseResponse(string? message, StatusCodeEnum statusCode, T? data)
        {
            Message = message;
            StatusCode = statusCode;
            Data = data;
        }

        // Constructor m?i – dùng khi có Errors/Warnings
        public BaseResponse(
            string? message,
            StatusCodeEnum statusCode,
            T? data,
            List<ErrorDetail>? errors = null,
            List<ErrorDetail>? warnings = null)
        {
            Message = message ?? "Successful";
            StatusCode = statusCode;
            Data = data;
            Errors = errors ?? new List<ErrorDetail>();
            Warnings = warnings ?? new List<ErrorDetail>();
        }
    }
    public class StandardResponse<T>
    {
        public string Message { get; set; }
        public T Data { get; set; }
    }

    public class ErrorDetail
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Suggestion { get; set; }
    }

}
