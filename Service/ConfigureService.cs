using DataAccessLayer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository;
using Repository.IRepository;
using Repository.Repository;
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
            services.AddAutoMapper(typeof(CurriculumMappingProfile));
            services.AddAutoMapper(typeof(CourseInstanceMappingProfile));
            services.AddAutoMapper(typeof(UserMappingProfile));



            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICampusService, CampusService>();
            services.AddScoped<IAcademicYearService, AcademicYearService>();
            services.AddScoped<ISemesterService, SemesterService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<ICurriculumService, CurriculumService>();
            services.AddScoped<ICourseInstanceService, CourseInstanceService>();
            return services;

        }
    }
}
