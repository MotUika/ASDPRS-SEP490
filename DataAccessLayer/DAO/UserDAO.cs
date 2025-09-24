using BussinessObject.Models;
using DataAccessLayer.BaseDAO;
using DataAccessLayer.IBaseDAO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.DAO
{
    public interface IUserDAO : IBaseDAO<User>
    {
        Task<(int totalAccount, int adminAccount, int studentAccount, int instructorAccount)> GetTotalAccount();
    }

    public class UserDAO : BaseDAO<User>, IUserDAO
    {
        private readonly ASDPRSContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public UserDAO(
            ASDPRSContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager) : base(context)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<(int totalAccount, int adminAccount, int studentAccount, int instructorAccount)> GetTotalAccount()
        {
            var adminRole = await _roleManager.FindByNameAsync("Admin");
            var adminsCount = await _userManager.GetUsersInRoleAsync(adminRole?.Name ?? string.Empty);

            var studentRole = await _roleManager.FindByNameAsync("Student");
            var studentsCount = await _userManager.GetUsersInRoleAsync(studentRole?.Name ?? string.Empty);

            var instructorRole = await _roleManager.FindByNameAsync("Instructor");
            var instructorsCount = await _userManager.GetUsersInRoleAsync(instructorRole?.Name ?? string.Empty);

            return (
                totalAccount: adminsCount.Count + studentsCount.Count + instructorsCount.Count,
                adminAccount: adminsCount.Count,
                studentAccount: studentsCount.Count,
                instructorAccount: instructorsCount.Count
            );
        }
    }
}
