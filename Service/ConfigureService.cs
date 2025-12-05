using DataAccessLayer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository;
using Repository.IRepository;
using Repository.Repository;
using Service.BackgroundJobs;
using Service.Interface;
using Service.IService;
using Service.Mapping;
using Service.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public static class ConfigureService
    {
        public static IServiceCollection ConfigureServiceService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(typeof(CampusMappingProfile));
            services.AddAutoMapper(typeof(AcademicYearMappingProfile));
            services.AddAutoMapper(typeof(SemesterMappingProfile));
            services.AddAutoMapper(typeof(CourseMappingProfile));
            services.AddAutoMapper(typeof(CurriculumProfile));
            services.AddAutoMapper(typeof(CourseInstanceMappingProfile));
            services.AddAutoMapper(typeof(UserMappingProfile));
            services.AddAutoMapper(typeof(RubricTemplateMappingProfile));
            services.AddAutoMapper(typeof(CriteriaTemplateMappingProfile));
            services.AddAutoMapper(typeof(RubricMappingProfile));
            services.AddAutoMapper(typeof(CriteriaMappingProfile));
            services.AddAutoMapper(typeof(CriteriaFeedbackMappingProfile));
            services.AddAutoMapper(typeof(MajorMappingProfile));
            services.AddAutoMapper(typeof(RegradeRequestProfile));






            // Register service interfaces and implementations
            services.AddScoped<ICampusService, CampusService>();
            services.AddScoped<IAcademicYearService, AcademicYearService>();
            services.AddScoped<ISemesterService, SemesterService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<ICurriculumService, CurriculumService>();
            services.AddScoped<ICriteriaTemplateService, CriteriaTemplateService>();
            services.AddScoped<IRubricTemplateService, RubricTemplateService>();
            services.AddScoped<IRubricService, RubricService>();
            services.AddScoped<ICriteriaService, CriteriaService>();
            services.AddScoped<ICriteriaFeedbackService, CriteriaFeedbackService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IAssignmentService, AssignmentService>();
            services.AddScoped<IAISummaryService, AISummaryService>();
            services.AddScoped<ISubmissionService, SubmissionService>();
            services.AddScoped<IRegradeRequestService, RegradeRequestService>();
            services.AddScoped<IReviewAssignmentService, ReviewAssignmentService>();
            services.AddScoped<ICourseInstanceService, CourseInstanceService>();
            services.AddScoped<ICourseInstructorService,CourseInstructorService>();
            services.AddScoped<ICourseStudentService, CourseStudentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IMajorService, MajorService>(); 
            services.AddScoped<IAISummaryService, AISummaryService>();
            services.AddScoped<IGenAIService, GeminiAiService>();
            services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
            services.AddScoped<ISystemConfigService, SystemConfigService>();
            services.AddScoped<IKeywordSearchService, KeywordSearchService>();
            services.AddScoped<IDashboardService, DashboardService>();



            // Add UserService and other dependencies if needed
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddHostedService<DeadlineReminderBackgroundService>();
            services.AddHostedService<CourseStatusWorker>();


            return services;

        }
    }
}
