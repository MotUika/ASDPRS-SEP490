using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Request.SystemConfig;
using Service.RequestAndResponse.Response.SystemConfig;

namespace Service.IService
{
    public interface ISystemConfigService
    {
        Task<string> GetSystemConfigAsync(string key);
        Task<T> GetConfigValueAsync<T>(string key, T defaultValue = default(T)) where T : IConvertible;
        Task<BaseResponse<List<SystemConfigResponse>>> GetAllConfigsAsync();
        Task<BaseResponse<SystemConfigResponse>> GetConfigByKeyAsync(string key);
        Task<BaseResponse<SystemConfigResponse>> UpdateConfigAsync(UpdateSystemConfigRequest request);
    }
}
