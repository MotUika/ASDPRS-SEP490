using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Service.IService;
using Service.RequestAndResponse.BaseResponse;
using Service.RequestAndResponse.Enums;
using Service.RequestAndResponse.Request.SystemConfig;
using Service.RequestAndResponse.Response.SystemConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service
{
    public class SystemConfigService : ISystemConfigService
    {
        private readonly ASDPRSContext _context;
        private readonly ILogger<SystemConfigService> _logger;

        public SystemConfigService(ASDPRSContext context, ILogger<SystemConfigService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetSystemConfigAsync(string key)
        {
            var config = await _context.SystemConfigs
                .FirstOrDefaultAsync(sc => sc.ConfigKey == key);
            return config?.ConfigValue;
        }

        public async Task<BaseResponse<List<SystemConfigResponse>>> GetAllConfigsAsync()
        {
            try
            {
                var configs = await _context.SystemConfigs
                    .OrderBy(sc => sc.ConfigKey)
                    .ToListAsync();

                var responses = configs.Select(c => new SystemConfigResponse
                {
                    ConfigId = c.ConfigId,
                    ConfigKey = c.ConfigKey,
                    ConfigValue = c.ConfigValue,
                    Description = c.Description,
                    UpdatedAt = c.UpdatedAt,
                    UpdatedByUserId = c.UpdatedByUserId
                }).ToList();

                return new BaseResponse<List<SystemConfigResponse>>(
                    "System configs retrieved successfully",
                    StatusCodeEnum.OK_200,
                    responses);
            }
            catch (Exception ex)
            {
                return new BaseResponse<List<SystemConfigResponse>>(
                    $"Error retrieving system configs: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<SystemConfigResponse>> GetConfigByKeyAsync(string key)
        {
            try
            {
                var config = await _context.SystemConfigs
                    .FirstOrDefaultAsync(sc => sc.ConfigKey == key);

                if (config == null)
                {
                    return new BaseResponse<SystemConfigResponse>(
                        $"System config with key '{key}' not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                var response = new SystemConfigResponse
                {
                    ConfigId = config.ConfigId,
                    ConfigKey = config.ConfigKey,
                    ConfigValue = config.ConfigValue,
                    Description = config.Description,
                    UpdatedAt = config.UpdatedAt,
                    UpdatedByUserId = config.UpdatedByUserId
                };

                return new BaseResponse<SystemConfigResponse>(
                    "System config retrieved successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<SystemConfigResponse>(
                    $"Error retrieving system config: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        public async Task<BaseResponse<SystemConfigResponse>> UpdateConfigAsync(UpdateSystemConfigRequest request)
        {
            try
            {
                var config = await _context.SystemConfigs
                    .FirstOrDefaultAsync(sc => sc.ConfigKey == request.ConfigKey);

                if (config == null)
                {
                    return new BaseResponse<SystemConfigResponse>(
                        $"System config with key '{request.ConfigKey}' not found",
                        StatusCodeEnum.NotFound_404,
                        null);
                }

                // Validate config value based on key
                var validationResult = ValidateConfigValue(request.ConfigKey, request.ConfigValue);
                if (!validationResult.IsValid)
                {
                    return new BaseResponse<SystemConfigResponse>(
                        validationResult.ErrorMessage,
                        StatusCodeEnum.BadRequest_400,
                        null);
                }

                // Chỉ update ConfigValue, không cho phép thay đổi các field khác
                config.ConfigValue = request.ConfigValue;
                config.UpdatedAt = DateTime.UtcNow;
                config.UpdatedByUserId = request.UpdatedByUserId;

                // Không update Description nếu không cung cấp, nhưng theo request, Description optional nên không update nếu null
                if (!string.IsNullOrEmpty(request.Description))
                {
                    config.Description = request.Description;
                }

                await _context.SaveChangesAsync();

                var response = new SystemConfigResponse
                {
                    ConfigId = config.ConfigId,
                    ConfigKey = config.ConfigKey,
                    ConfigValue = config.ConfigValue,
                    Description = config.Description,
                    UpdatedAt = config.UpdatedAt,
                    UpdatedByUserId = config.UpdatedByUserId
                };

                return new BaseResponse<SystemConfigResponse>(
                    "System config updated successfully",
                    StatusCodeEnum.OK_200,
                    response);
            }
            catch (Exception ex)
            {
                return new BaseResponse<SystemConfigResponse>(
                    $"Error updating system config: {ex.Message}",
                    StatusCodeEnum.InternalServerError_500,
                    null);
            }
        }

        // Helper method để validate config value
        private (bool IsValid, string ErrorMessage) ValidateConfigValue(string key, string value)
        {
            return key switch
            {
                "ScorePrecision" => ValidateScorePrecision(value),
                "AISummaryMaxTokens" => ValidatePositiveInteger(value, "AISummaryMaxTokens"),
                "AISummaryMaxWords" => ValidatePositiveInteger(value, "AISummaryMaxWords"),
                "DefaultPassThreshold" => ValidatePercentage(value),
                "PlagiarismThreshold" => ValidatePercentage(value),
                "RegradeProcessingDeadlineDays" => ValidatePositiveInteger(value, "RegradeProcessingDeadlineDays"),
                _ => (true, string.Empty) // Cho phép update các config khác không cần validate
            };
        }

        private (bool IsValid, string ErrorMessage) ValidateScorePrecision(string value)
        {
            if (!decimal.TryParse(value, out decimal precision))
                return (false, "ScorePrecision must be a decimal number");

            var allowedPrecisions = new[] { 0.25m, 0.5m, 1.0m };
            if (!allowedPrecisions.Contains(precision))
                return (false, "ScorePrecision must be one of: 0.25, 0.5, 1.0");

            return (true, string.Empty);
        }

        private (bool IsValid, string ErrorMessage) ValidatePositiveInteger(string value, string fieldName)
        {
            if (!int.TryParse(value, out int intValue) || intValue <= 0)
                return (false, $"{fieldName} must be a positive integer");

            return (true, string.Empty);
        }

        private (bool IsValid, string ErrorMessage) ValidatePercentage(string value)
        {
            if (!decimal.TryParse(value, out decimal percentage) || percentage < 0 || percentage > 100)
                return (false, "Value must be a percentage between 0 and 100");

            return (true, string.Empty);
        }

        // Helper method để lấy giá trị config với fallback và type conversion
        public async Task<T> GetConfigValueAsync<T>(string key, T defaultValue = default(T)) where T : IConvertible
        {
            try
            {
                var config = await _context.SystemConfigs
                    .FirstOrDefaultAsync(sc => sc.ConfigKey == key);

                if (config == null || string.IsNullOrEmpty(config.ConfigValue))
                    return defaultValue;

                return (T)Convert.ChangeType(config.ConfigValue, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}