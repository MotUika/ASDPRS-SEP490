using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.DAO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository.BaseRepository;
using Repository.IBaseRepository;
using Repository.IRepository;
using Repository.Repository;

namespace Repository
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureRepositoryService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<CourseStudentDAO>();
            services.AddScoped<CourseInstructorDAO>();
            services.AddScoped<AcademicYearDAO>();
            services.AddScoped<CampusDAO>();
            services.AddScoped<CourseDAO>();
            services.AddScoped<CriteriaTemplateDAO>();
            services.AddScoped<CriteriaFeedbackDAO>();
            services.AddScoped<CurriculumDAO>();
            services.AddScoped<SemesterDAO>();
            services.AddScoped<SubmissionDAO>();
            services.AddScoped<CriteriaDAO>();
            services.AddScoped<RubricTemplateDAO>();
            services.AddScoped<RubricDAO>();
            services.AddScoped<AssignmentDAO>();
            services.AddScoped<AISummaryDAO>();
            services.AddScoped<ReviewDAO>();
            services.AddScoped<UserDAO>();
            services.AddScoped<RegradeRequestDAO>();
            services.AddScoped<CourseInstanceDAO>();
            services.AddScoped<ReviewAssignmentDAO>();
            services.AddScoped<NotificationDAO>();
            services.AddScoped<MajorDAO>();



            services.AddScoped<ICourseStudentRepository, CourseStudentRepository>();
            services.AddScoped<ICourseInstructorRepository, CourseInstructorRepository>();
            services.AddScoped<IAcademicYearRepository, AcademicYearRepository>();
            services.AddScoped<ICampusRepository, CampusRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<ICriteriaTemplateRepository, CriteriaTemplateRepository>();
            services.AddScoped<ICriteriaFeedbackRepository, CriteriaFeedbackRepository>();
            services.AddScoped<ICurriculumRepository, CurriculumRepository>();
            services.AddScoped<ISemesterRepository, SemesterRepository>();
            services.AddScoped<ISubmissionRepository, SubmissionRepository>();
            services.AddScoped<ICriteriaRepository, CriteriaRepository>();
            services.AddScoped<IRubricTemplateRepository, RubricTemplateRepository>();
            services.AddScoped<IRubricRepository, RubricRepository>();
            services.AddScoped<IAssignmentRepository, AssignmentRepository>();
            services.AddScoped<IAISummaryRepository, AISummaryRepository>();
            services.AddScoped<IReviewRepository, ReviewRepository>();
            services.AddScoped<IRegradeRequestRepository, RegradeRequestRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAcademicYearRepository, AcademicYearRepository>();
            services.AddScoped<IReviewAssignmentRepository, ReviewAssignmentRepository>();
            services.AddScoped<ICourseInstanceRepository, CourseInstanceRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();
            services.AddScoped<ICourseInstanceRepository, CourseInstanceRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IMajorRepository, MajorRepository>();
            services.AddScoped<IDashboardRepository, DashboardRepository>();



            return services;
        }
    }
}
