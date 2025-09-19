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
            // Get user roles from database
            var userRoles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Join(_dbContext.Roles,
                    ur => ur.RoleId,
                    r => r.RoleId,
                    (ur, r) => r.RoleName)
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName + " " + user.LastName),
                new Claim("userId", user.UserId.ToString())
            };

            // Add role claims
            foreach (var role in userRoles)
            {
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

            // Save refresh token to database
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                JwtId = token.Id,
                UserId = user.UserId,
                Token = refreshToken,
                IsUsed = false,
                IsRevoked = false,
                CreateAt = DateTime.Now,
                ExpiredAt = DateTime.Now.AddDays(7)
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
                ValidateLifetime = false
            };

            try
            {
                // Check if AccessToken is valid
                var tokenInVerification = tokenHandler.ValidateToken(model.AccessToken, tokenValidateParam, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtSecurityToken)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid token format"
                    };
                }

                // Check if access token has expired
                var utcExpireDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
                var expireDate = ConvertUnixTimeToDateTime(utcExpireDate);
                if (expireDate > DateTime.UtcNow)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Access token has not yet expired"
                    };
                }

                // Check if refresh token exists in DB
                var storedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == model.RefreshToken);
                if (storedToken == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Refresh token does not exist"
                    };
                }

                // Check if refresh token is used/revoked
                if (storedToken.IsUsed)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Refresh token has been used"
                    };
                }

                if (storedToken.IsRevoked)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Refresh token has been revoked"
                    };
                }

                // Check if AccessToken id matches JwtId in RefreshToken
                var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
                if (storedToken.JwtId != jti)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Token doesn't match"
                    };
                }

                // Update token as used
                storedToken.IsRevoked = true;
                storedToken.IsUsed = true;
                _dbContext.RefreshTokens.Update(storedToken);
                await _dbContext.SaveChangesAsync();

                // Create new token
                var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserId == storedToken.UserId);
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