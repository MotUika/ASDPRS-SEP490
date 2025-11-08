using BussinessObject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DataAccessLayer
{
    public class ASDPRSContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ASDPRSContext(DbContextOptions<ASDPRSContext> options) : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Campus> Campuses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<Curriculum> Curriculums { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseInstance> CourseInstances { get; set; }
        public DbSet<CourseInstructor> CourseInstructors { get; set; }
        public DbSet<CourseStudent> CourseStudents { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Rubric> Rubrics { get; set; }
        public DbSet<RubricTemplate> RubricTemplates { get; set; }
        public DbSet<CriteriaTemplate> CriteriaTemplates { get; set; }
        public DbSet<Criteria> Criteria { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<ReviewAssignment> ReviewAssignments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<CriteriaFeedback> CriteriaFeedbacks { get; set; }
        public DbSet<AISummary> AISummaries { get; set; }
        public DbSet<RegradeRequest> RegradeRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<DocumentEmbedding> DocumentEmbeddings { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Major> Majors { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure non-Identity relationships
            modelBuilder.Entity<Campus>()
                .HasMany(c => c.Users)
                .WithOne(u => u.Campus)
                .HasForeignKey(u => u.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Campus>()
                .HasMany(c => c.AcademicYears)
                .WithOne(ay => ay.Campus)
                .HasForeignKey(ay => ay.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Campus>()
                .HasMany(c => c.Curriculums)
                .WithOne(cu => cu.Campus)
                .HasForeignKey(cu => cu.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Campus>()
                .HasMany(c => c.CourseInstances)
                .WithOne(ci => ci.Campus)
                .HasForeignKey(ci => ci.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.CourseInstructors)
                .WithOne(ci => ci.User)
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.CourseStudents)
                .WithOne(cs => cs.User)
                .HasForeignKey(cs => cs.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.SystemConfigs)
                .WithOne(sc => sc.UpdatedByUser)
                .HasForeignKey(sc => sc.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Submissions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ReviewAssignments)
                .WithOne(ra => ra.ReviewerUser)
                .HasForeignKey(ra => ra.ReviewerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.RegradeRequestsReviewed)
                .WithOne(rr => rr.ReviewedByUser)
                .HasForeignKey(rr => rr.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ReceivedNotifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.CreatedRubricTemplates)
                .WithOne(rt => rt.CreatedByUser)
                .HasForeignKey(rt => rt.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.SentNotifications)
                .WithOne(n => n.SenderUser)
                .HasForeignKey(n => n.SenderUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ReviewedRegradeRequests)
                .WithOne(rr => rr.ReviewedByInstructor)
                .HasForeignKey(rr => rr.ReviewedByInstructorId)
                .OnDelete(DeleteBehavior.Restrict);

            // AcademicYear
            modelBuilder.Entity<AcademicYear>()
                .HasMany(ay => ay.Semesters)
                .WithOne(s => s.AcademicYear)
                .HasForeignKey(s => s.AcademicYearId)
                .OnDelete(DeleteBehavior.Cascade);

            // Curriculum
            modelBuilder.Entity<Curriculum>()
                .HasMany(cu => cu.Courses)
                .WithOne(c => c.Curriculum)
                .HasForeignKey(c => c.CurriculumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Course
            modelBuilder.Entity<Course>()
                .HasMany(c => c.CourseInstances)
                .WithOne(ci => ci.Course)
                .HasForeignKey(ci => ci.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Semester
            modelBuilder.Entity<Semester>()
                .HasMany(s => s.CourseInstances)
                .WithOne(ci => ci.Semester)
                .HasForeignKey(ci => ci.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // CourseInstance
            modelBuilder.Entity<CourseInstance>()
                .HasMany(ci => ci.CourseInstructors)
                .WithOne(ci => ci.CourseInstance)
                .HasForeignKey(ci => ci.CourseInstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseInstance>()
                .HasMany(ci => ci.CourseStudents)
                .WithOne(cs => cs.CourseInstance)
                .HasForeignKey(cs => cs.CourseInstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseInstance>()
                .HasMany(ci => ci.Assignments)
                .WithOne(a => a.CourseInstance)
                .HasForeignKey(a => a.CourseInstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            // CourseInstructor
            modelBuilder.Entity<CourseInstructor>()
                .HasKey(ci => ci.Id);

            // CourseStudent
            modelBuilder.Entity<CourseStudent>()
                .HasOne(cs => cs.ChangedByUser)
                .WithMany()
                .HasForeignKey(cs => cs.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseStudent>(entity =>
            {
                entity.Property(cs => cs.FinalGrade)
                    .HasPrecision(5, 2);
            });
            // SystemConfig
            modelBuilder.Entity<SystemConfig>()
                .HasOne(sc => sc.UpdatedByUser)
                .WithMany(u => u.SystemConfigs)
                .HasForeignKey(sc => sc.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Assignment - UPDATED WITH NEW FIELDS AND RELATIONSHIPS
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Rubric)
                .WithOne(r => r.Assignment)
                .HasForeignKey<Rubric>(r => r.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Assignment>()
                .HasMany(a => a.Submissions)
                .WithOne(s => s.Assignment)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Assignment>()
                .HasMany(a => a.Notifications)
                .WithOne(n => n.Assignment)
                .HasForeignKey(n => n.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Assignment>()
                .Property(a => a.GradingScale)
                .HasMaxLength(50);

            // Self-referencing relationship for cloning
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.ClonedFromAssignment)
                .WithMany(a => a.ClonedAssignments)
                .HasForeignKey(a => a.ClonedFromAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assignment>(entity =>
            {
                entity.Property(a => a.InstructorWeight)
                    .HasPrecision(5, 2);

                entity.Property(a => a.PeerWeight)
                    .HasPrecision(5, 2);

                entity.Property(a => a.PassThreshold)
                    .HasPrecision(5, 2);

                entity.Property(a => a.MissingReviewPenalty)
                    .HasPrecision(5, 2);
            });
            // Indexes for performance
            modelBuilder.Entity<Assignment>()
                .HasIndex(a => a.Status);

            modelBuilder.Entity<Assignment>()
                .HasIndex(a => a.StartDate);

            modelBuilder.Entity<Assignment>()
                .HasIndex(a => a.Deadline);

            modelBuilder.Entity<Assignment>()
                .HasIndex(a => a.FinalDeadline);

            modelBuilder.Entity<Assignment>()
                .HasIndex(a => new { a.CourseInstanceId, a.Status });

            // Rubric
            modelBuilder.Entity<Rubric>()
                .HasOne(r => r.Template)
                .WithMany(rt => rt.Rubrics)
                .HasForeignKey(r => r.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Rubric>()
                .HasMany(r => r.Criteria)
                .WithOne(c => c.Rubric)
                .HasForeignKey(c => c.RubricId)
                .OnDelete(DeleteBehavior.Cascade);

            // RubricTemplate
            modelBuilder.Entity<RubricTemplate>()
                .HasOne(rt => rt.CreatedByUser)
                .WithMany(u => u.CreatedRubricTemplates)
                .HasForeignKey(rt => rt.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RubricTemplate>()
                .HasMany(rt => rt.CriteriaTemplates)
                .WithOne(ct => ct.Template)
                .HasForeignKey(ct => ct.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Criteria
            modelBuilder.Entity<Criteria>()
                .HasOne(c => c.CriteriaTemplate)
                .WithMany(ct => ct.Criteria)
                .HasForeignKey(c => c.CriteriaTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Criteria>()
                .HasMany(c => c.CriteriaFeedbacks)
                .WithOne(cf => cf.Criteria)
                .HasForeignKey(cf => cf.CriteriaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Criteria>(entity =>
            {
                entity.Property(c => c.MaxScore)
                    .HasPrecision(5, 2);
            });

            // CriteriaTemplate
            modelBuilder.Entity<CriteriaTemplate>(entity =>
            {
                entity.Property(ct => ct.MaxScore)
                    .HasPrecision(5, 2);
            });

            // CriteriaFeedback
            modelBuilder.Entity<CriteriaFeedback>(entity =>
            {
                entity.Property(cf => cf.ScoreAwarded)
                    .HasPrecision(5, 2);
            });

            // Submission
            modelBuilder.Entity<Submission>()
                .HasMany(s => s.ReviewAssignments)
                .WithOne(ra => ra.Submission)
                .HasForeignKey(ra => ra.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Submission>()
                .HasMany(s => s.AISummaries)
                .WithOne(ais => ais.Submission)
                .HasForeignKey(ais => ais.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Submission>()
                .HasMany(s => s.RegradeRequests)
                .WithOne(rr => rr.Submission)
                .HasForeignKey(rr => rr.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Submission>()
                .HasMany(s => s.Notifications)
                .WithOne(n => n.Submission)
                .HasForeignKey(n => n.SubmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Submission>()
                .HasMany(s => s.DocumentEmbeddings)
                .WithOne()
                .HasForeignKey(de => de.SourceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DocumentEmbeddings_Submission");

            // ReviewAssignment
            modelBuilder.Entity<ReviewAssignment>()
                .HasMany(ra => ra.Reviews)
                .WithOne(r => r.ReviewAssignment)
                .HasForeignKey(r => r.ReviewAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReviewAssignment>()
                .HasMany(ra => ra.Notifications)
                .WithOne(n => n.ReviewAssignment)
                .HasForeignKey(n => n.ReviewAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review
            modelBuilder.Entity<Review>()
                .HasMany(r => r.CriteriaFeedbacks)
                .WithOne(cf => cf.Review)
                .HasForeignKey(cf => cf.ReviewId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasMany(r => r.DocumentEmbeddings)
                .WithOne()
                .HasForeignKey(de => de.SourceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DocumentEmbeddings_Review");

            modelBuilder.Entity<Review>(entity =>
            {
                entity.Property(r => r.OverallScore)
                    .HasPrecision(5, 2);
            });

            // AISummary
            modelBuilder.Entity<AISummary>()
                .HasMany(ais => ais.DocumentEmbeddings)
                .WithOne()
                .HasForeignKey(de => de.SourceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DocumentEmbeddings_AISummary");

            // Notification
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.SenderUser)
                .WithMany(u => u.SentNotifications)
                .HasForeignKey(n => n.SenderUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // DocumentEmbedding
            modelBuilder.Entity<DocumentEmbedding>()
                .HasIndex(de => new { de.SourceType, de.SourceId })
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Major>()
                .HasMany(m => m.Curriculums)
                .WithOne(c => c.Major)
                .HasForeignKey(c => c.MajorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Curriculum>()
                .HasOne(c => c.Major)
                .WithMany(m => m.Curriculums)
                .HasForeignKey(c => c.MajorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Curriculum>()
                .HasOne(c => c.Campus)
                .WithMany(c => c.Curriculums)
                .HasForeignKey(c => c.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Majors
            modelBuilder.Entity<Major>().HasData(
                new Major { MajorId = 1, MajorName = "Software Engineering", MajorCode = "SE" },
                new Major { MajorId = 2, MajorName = "Computer Science", MajorCode = "CS" },
                new Major { MajorId = 3, MajorName = "Information Technology", MajorCode = "IT" }
            );
            // Seed Campuses
            modelBuilder.Entity<Campus>().HasData(
                new Campus { CampusId = 1, CampusName = "Hồ Chí Minh", Address = "7 Đ. D1, Long Thạnh Mỹ, Thủ Đức, Hồ Chí Minh" },
                new Campus { CampusId = 2, CampusName = "Hà Nội", Address = "Khu Công Nghệ Cao Hòa Lạc, km 29, Đại lộ, Thăng Long, Hà Nội" }
            );

            // Seed Roles
            modelBuilder.Entity<IdentityRole<int>>().HasData(
                new IdentityRole<int> { Id = 1, Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole<int> { Id = 2, Name = "Student", NormalizedName = "STUDENT" },
                new IdentityRole<int> { Id = 3, Name = "Instructor", NormalizedName = "INSTRUCTOR" }
            );

            // Seed Admin User
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    CampusId = 1,
                    FirstName = "Admin",
                    LastName = "User",
                    StudentCode = "ADMIN001",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@example.com",
                    NormalizedEmail = "ADMIN@EXAMPLE.COM",
                    EmailConfirmed = true,
                    PasswordHash = "AQAAAAIAAYagAAAAEK95SlxvEPzqxJyTxIof0ufhmHVKdEGcuw7MxCBj92JUehpXlaMI0F4RrX3mzLDNzA==",
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    LockoutEnabled = true,
                    AccessFailedCount = 0
                }
            );
            // Seed UserRoles
            modelBuilder.Entity<IdentityUserRole<int>>().HasData(
                new IdentityUserRole<int> { UserId = 1, RoleId = 1 }
            );
            modelBuilder.Entity<SystemConfig>().HasData(
                new SystemConfig
                {
                    ConfigId = 100,
                    ConfigKey = "ScorePrecision",
                    ConfigValue = "0.5",
                    Description = "Number accuracy (0.25, 0.5, 1.0)",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = 1
                },
                new SystemConfig
                {
                    ConfigId = 101,
                    ConfigKey = "AISummaryMaxTokens",
                    ConfigValue = "1000",
                    Description = "Maximum number of tokens for AI summary",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = 1
                },
                new SystemConfig
                {
                    ConfigId = 102,
                    ConfigKey = "AISummaryMaxWords",
                    ConfigValue = "200",
                    Description = "Maximum word count for AI summary",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = 1
                },
                new SystemConfig
                {
                    ConfigId = 103,
                    ConfigKey = "DefaultPassThreshold",
                    ConfigValue = "50",
                    Description = "Ngưỡng điểm mặc định để Pass",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = 1
                },
                new SystemConfig
                {
                    ConfigId = 104,
                    ConfigKey = "PlagiarismThreshold",
                    ConfigValue = "80",
                    Description = "Maximum allowed plagiarism percentage before blocking submission (0-100)",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = 1
                }
            );
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ASDPRS-SEP490"));
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var connectionString = configuration.GetConnectionString("DbConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string 'DbConnection' not found in appsettings.json.");
                }

                optionsBuilder.UseSqlServer(connectionString);
            }
        }
    }
}