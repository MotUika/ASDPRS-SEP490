using BussinessObject.Models;
using DataAccessLayer;
using DataAccessLayer.BaseDAO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository.BaseRepository;
using Repository.IBaseRepository;
using Repository.IRepository;
using Repository.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public static class ConfigureService
    {
        public static IServiceCollection ConfigureRepositoryService(this IServiceCollection services, IConfiguration configuration)
        {
           
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddTransient<ITokenRepository, TokenRepository>();
            services.AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped<ICourseInstanceRepository, CourseInstanceRepository>();
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
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<ICriteriaRepository, CriteriaRepository>();
            services.AddScoped<IRubricTemplateRepository, RubricTemplateRepository>();
            services.AddScoped<IRubricRepository, RubricRepository>();


            //
            services.AddScoped<UserDAO>();
            services.AddScoped<AcademicYearDAO>();
            services.AddScoped<CampusDAO>();
            services.AddScoped<CourseDAO>();
            services.AddScoped<CriteriaTemplateDAO>();
            services.AddScoped<CriteriaFeedbackDAO>();
            services.AddScoped<CurriculumDAO>();
            services.AddScoped<SemesterDAO>();
            services.AddScoped<SubmissionDAO>();
            services.AddScoped<NotificationDAO>();
            services.AddScoped<CriteriaDAO>();
            services.AddScoped<RubricTemplateDAO>();
            services.AddScoped<RubricDAO>();
            services.AddScoped<RoleDAO>();
            services.AddScoped<CourseInstanceDAO>();
            services.AddScoped<CourseStudentDAO>();
            services.AddScoped<CourseInstructorDAO>();
            //

            return services;
        }
    }
}
