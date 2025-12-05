using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BussinessObject.Models;

namespace Repository.IRepository
{
    public interface IDashboardRepository
    {
        Task<int> GetTotalStudentsBySemesterAsync(int semesterId);
        Task<int> GetTotalInstructorsBySemesterAsync(int semesterId);
        Task<Dictionary<string, int>> GetAssignmentStatusCountsAsync(int semesterId);
        Task<(int rubrics, int criteria)> GetRubricAndCriteriaCountsAsync(int semesterId);
        Task<List<Assignment>> GetAssignmentsWithSubmissionsBySemesterAsync(int semesterId);
        Task<List<decimal>> GetFinalScoresBySemesterAsync(int semesterId);
        Task<int> GetTotalExpectedSubmissionsAsync(int semesterId);
    }
}

