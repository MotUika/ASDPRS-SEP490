using BussinessObject.Models;
using DataAccessLayer.BaseDAO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    public class UserDAO : BaseDAO<User>
    {
        private readonly ASDPRSContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserDAO(ASDPRSContext context, UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager) : base(context)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<(int totalAccount, int adminAccount, int studentAccount, int instructorAccount)> GetTotalAccount()
        {
            var adminRole = await _roleManager.FindByNameAsync("Admin");
            var adminsCount = await _userManager.GetUsersInRoleAsync(adminRole.Name);

            var studentRole = await _roleManager.FindByNameAsync("Student");
            var studentsCount = await _userManager.GetUsersInRoleAsync(studentRole.Name);

            var instructorRole = await _roleManager.FindByNameAsync("Manager");
            var instructorsCount = await _userManager.GetUsersInRoleAsync(instructorRole.Name);

            int totalAccountsCount = adminsCount.Count + studentsCount.Count + instructorsCount.Count;
            int studentsAccount = studentsCount.Count;
            int instructorsAccount = instructorsCount.Count;
            int adminsAccount = adminsCount.Count;

            return (totalAccountsCount, studentsAccount, instructorsAccount, adminsAccount);
        }
    }
}
