using BussinessObject.IdentityModel;
using BussinessObject.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repository.IBaseRepository;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Repository.BaseRepository
{
    public class TokenRepository : ITokenRepository
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;
        private readonly ASDPRSContext _dbContext;

        public TokenRepository(IConfiguration config, ASDPRSContext dbContext)
        {
            _config = config;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SigningKey"]));
            _dbContext = dbContext;
        }

        public async Task<TokenModel> CreateToken(User user)
        {
            // ở constructor hoặc trước khi tạo token
            var keyFromRepo = _config["JWT:SigningKey"] ?? "<null>";
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(keyFromRepo)));
                Console.WriteLine($"[DEBUG] (TokenRepository) SigningKey SHA256: {hash} (len={keyFromRepo.Length})");
            }

            var userRoles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_dbContext.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name)
                .ToListAsync();

            var jti = Guid.NewGuid().ToString();

            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Jti, jti),
        
        new Claim("userId", user.Id.ToString()),
        new Claim("studentCode", user.StudentCode ?? string.Empty),
        new Claim("fullName", $"{user.FirstName} {user.LastName}")
    };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim("role", role));
                claims.Add(new Claim(ClaimTypes.Role, role));
            }


            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = creds,
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            var accessToken = tokenHandler.WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                JwtId = jti,
                UserId = user.Id,
                Token = refreshToken,
                IsUsed = false,
                IsRevoked = false,
                CreateAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddDays(7)
            };

            await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity);
            await _dbContext.SaveChangesAsync();

            return new TokenModel
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        public async Task<ApiResponse> RenewToken(TokenModel model)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValidateParam = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _config["JWT:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["JWT:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateLifetime = false,
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier 
            };

            try
            {
                var tokenInVerification = tokenHandler.ValidateToken(model.AccessToken, tokenValidateParam, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtSecurityToken)
                {
                    return new ApiResponse { Success = false, Message = "Invalid token format" };
                }

                
                var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrEmpty(jti))
                {
                    return new ApiResponse { Success = false, Message = "JwtId (JTI) not found in token" };
                }

                
                var utcExpireDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)?.Value ?? "0");
                var expireDate = ConvertUnixTimeToDateTime(utcExpireDate);

                if (expireDate > DateTime.UtcNow)
                {
                    return new ApiResponse { Success = false, Message = "Access token has not yet expired" };
                }

                
                var storedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == model.RefreshToken);
                if (storedToken == null)
                {
                    return new ApiResponse { Success = false, Message = "Refresh token does not exist" };
                }

                
                if (storedToken.IsUsed)
                {
                    return new ApiResponse { Success = false, Message = "Refresh token has been used" };
                }

                if (storedToken.IsRevoked)
                {
                    return new ApiResponse { Success = false, Message = "Refresh token has been revoked" };
                }

                
                if (storedToken.JwtId != jti)
                {
                    return new ApiResponse { Success = false, Message = "Token doesn't match" };
                }

                // Update token as used
                storedToken.IsRevoked = true;
                storedToken.IsUsed = true;
                _dbContext.RefreshTokens.Update(storedToken);
                await _dbContext.SaveChangesAsync();

                // Create new token
                var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == storedToken.UserId);
                var token = await CreateToken(user);

                return new ApiResponse
                {
                    Success = true,
                    Message = "Renew token success",
                    Data = token
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "Something went wrong: " + ex.Message
                };
            }
        }

        private DateTime ConvertUnixTimeToDateTime(long utcExpireDate)
        {
            var dateTimeInterval = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dateTimeInterval.AddSeconds(utcExpireDate).ToUniversalTime();
        }
    }
}