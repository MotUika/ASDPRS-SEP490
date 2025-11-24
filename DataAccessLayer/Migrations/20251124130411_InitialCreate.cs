using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campuses",
                columns: table => new
                {
                    CampusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampusName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campuses", x => x.CampusId);
                });

            migrationBuilder.CreateTable(
                name: "Majors",
                columns: table => new
                {
                    MajorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MajorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MajorCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Majors", x => x.MajorId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcademicYears",
                columns: table => new
                {
                    AcademicYearId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.AcademicYearId);
                    table.ForeignKey(
                        name: "FK_AcademicYears_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "CampusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    MajorId = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "CampusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "MajorId");
                });

            migrationBuilder.CreateTable(
                name: "Curriculums",
                columns: table => new
                {
                    CurriculumId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    MajorId = table.Column<int>(type: "int", nullable: false),
                    CurriculumName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurriculumCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalCredits = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curriculums", x => x.CurriculumId);
                    table.ForeignKey(
                        name: "FK_Curriculums_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "CampusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Curriculums_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "MajorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    SemesterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicYearId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.SemesterId);
                    table.ForeignKey(
                        name: "FK_Semesters_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "AcademicYearId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JwtId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubricTemplates",
                columns: table => new
                {
                    TemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MajorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricTemplates", x => x.TemplateId);
                    table.ForeignKey(
                        name: "FK_RubricTemplates_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RubricTemplates_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "MajorId");
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    ConfigId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfigValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.ConfigId);
                    table.ForeignKey(
                        name: "FK_SystemConfigs_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurriculumId = table.Column<int>(type: "int", nullable: false),
                    CourseCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Credits = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.CourseId);
                    table.ForeignKey(
                        name: "FK_Courses_Curriculums_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curriculums",
                        principalColumn: "CurriculumId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CriteriaTemplates",
                columns: table => new
                {
                    CriteriaTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ScoringType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScoreLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteriaTemplates", x => x.CriteriaTemplateId);
                    table.ForeignKey(
                        name: "FK_CriteriaTemplates_RubricTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "RubricTemplates",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseInstances",
                columns: table => new
                {
                    CourseInstanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    SectionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EnrollmentPassword = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseInstances", x => x.CourseInstanceId);
                    table.ForeignKey(
                        name: "FK_CourseInstances_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "CampusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseInstances_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseInstances_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "SemesterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseInstanceId = table.Column<int>(type: "int", nullable: false),
                    RubricId = table.Column<int>(type: "int", nullable: true),
                    RubricTemplateId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Guidelines = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NumPeerReviewsRequired = table.Column<int>(type: "int", nullable: false),
                    AllowCrossClass = table.Column<bool>(type: "bit", nullable: false),
                    CrossClassTag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsBlindReview = table.Column<bool>(type: "bit", nullable: false),
                    InstructorWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    GradingScale = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PeerWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PassThreshold = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MissingReviewPenalty = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IncludeAIScore = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClonedFromAssignmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_Assignments_Assignments_ClonedFromAssignmentId",
                        column: x => x.ClonedFromAssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_CourseInstances_CourseInstanceId",
                        column: x => x.CourseInstanceId,
                        principalTable: "CourseInstances",
                        principalColumn: "CourseInstanceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assignments_RubricTemplates_RubricTemplateId",
                        column: x => x.RubricTemplateId,
                        principalTable: "RubricTemplates",
                        principalColumn: "TemplateId");
                });

            migrationBuilder.CreateTable(
                name: "CourseInstructors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseInstanceId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseInstructors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseInstructors_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseInstructors_CourseInstances_CourseInstanceId",
                        column: x => x.CourseInstanceId,
                        principalTable: "CourseInstances",
                        principalColumn: "CourseInstanceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseStudents",
                columns: table => new
                {
                    CourseStudentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseInstanceId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinalGrade = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false),
                    StatusChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseStudents", x => x.CourseStudentId);
                    table.ForeignKey(
                        name: "FK_CourseStudents_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseStudents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseStudents_CourseInstances_CourseInstanceId",
                        column: x => x.CourseInstanceId,
                        principalTable: "CourseInstances",
                        principalColumn: "CourseInstanceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rubrics",
                columns: table => new
                {
                    RubricId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    AssignmentId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsModified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rubrics", x => x.RubricId);
                    table.ForeignKey(
                        name: "FK_Rubrics_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rubrics_RubricTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "RubricTemplates",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    SubmissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    InstructorScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PeerAverageScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FinalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GradedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.SubmissionId);
                    table.ForeignKey(
                        name: "FK_Submissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Submissions_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Criteria",
                columns: table => new
                {
                    CriteriaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RubricId = table.Column<int>(type: "int", nullable: false),
                    CriteriaTemplateId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ScoringType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScoreLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsModified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Criteria", x => x.CriteriaId);
                    table.ForeignKey(
                        name: "FK_Criteria_CriteriaTemplates_CriteriaTemplateId",
                        column: x => x.CriteriaTemplateId,
                        principalTable: "CriteriaTemplates",
                        principalColumn: "CriteriaTemplateId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Criteria_Rubrics_RubricId",
                        column: x => x.RubricId,
                        principalTable: "Rubrics",
                        principalColumn: "RubricId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AISummaries",
                columns: table => new
                {
                    SummaryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SummaryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISummaries", x => x.SummaryId);
                    table.ForeignKey(
                        name: "FK_AISummaries_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegradeRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedByInstructorId = table.Column<int>(type: "int", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CourseInstructorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegradeRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_RegradeRequests_AspNetUsers_ReviewedByInstructorId",
                        column: x => x.ReviewedByInstructorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegradeRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegradeRequests_CourseInstructors_CourseInstructorId",
                        column: x => x.CourseInstructorId,
                        principalTable: "CourseInstructors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegradeRequests_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewAssignments",
                columns: table => new
                {
                    ReviewAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    ReviewerUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAIReview = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewAssignments", x => x.ReviewAssignmentId);
                    table.ForeignKey(
                        name: "FK_ReviewAssignments_AspNetUsers_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewAssignments_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: true),
                    AssignmentId = table.Column<int>(type: "int", nullable: true),
                    SubmissionId = table.Column<int>(type: "int", nullable: true),
                    ReviewAssignmentId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_ReviewAssignments_ReviewAssignmentId",
                        column: x => x.ReviewAssignmentId,
                        principalTable: "ReviewAssignments",
                        principalColumn: "ReviewAssignmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    ReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewAssignmentId = table.Column<int>(type: "int", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    GeneralFeedback = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FeedbackSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.ReviewId);
                    table.ForeignKey(
                        name: "FK_Reviews_ReviewAssignments_ReviewAssignmentId",
                        column: x => x.ReviewAssignmentId,
                        principalTable: "ReviewAssignments",
                        principalColumn: "ReviewAssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CriteriaFeedbacks",
                columns: table => new
                {
                    CriteriaFeedbackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewId = table.Column<int>(type: "int", nullable: false),
                    CriteriaId = table.Column<int>(type: "int", nullable: false),
                    ScoreAwarded = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FeedbackSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteriaFeedbacks", x => x.CriteriaFeedbackId);
                    table.ForeignKey(
                        name: "FK_CriteriaFeedbacks_Criteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalTable: "Criteria",
                        principalColumn: "CriteriaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CriteriaFeedbacks_Reviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "Reviews",
                        principalColumn: "ReviewId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { 1, null, "Admin", "ADMIN" },
                    { 2, null, "Student", "STUDENT" },
                    { 3, null, "Instructor", "INSTRUCTOR" }
                });

            migrationBuilder.InsertData(
                table: "Campuses",
                columns: new[] { "CampusId", "Address", "CampusName" },
                values: new object[,]
                {
                    { 1, "7 Đ. D1, Long Thạnh Mỹ, Thủ Đức, Hồ Chí Minh", "Hồ Chí Minh" },
                    { 2, "Khu Công Nghệ Cao Hòa Lạc, km 29, Đại lộ, Thăng Long, Hà Nội", "Hà Nội" }
                });

            migrationBuilder.InsertData(
                table: "Majors",
                columns: new[] { "MajorId", "IsActive", "MajorCode", "MajorName" },
                values: new object[,]
                {
                    { 1, true, "SE", "Software Engineering" },
                    { 2, true, "CS", "Computer Science" },
                    { 3, true, "IT", "Information Technology" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "AvatarUrl", "CampusId", "ConcurrencyStamp", "CreatedAt", "Email", "EmailConfirmed", "FirstName", "IsActive", "LastName", "LockoutEnabled", "LockoutEnd", "MajorId", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "StudentCode", "TwoFactorEnabled", "UserName" },
                values: new object[] { 1, 0, null, 1, "932d575a-5140-41b4-bb96-9085af75c510", new DateTime(2025, 11, 24, 13, 4, 11, 199, DateTimeKind.Utc).AddTicks(1912), "admin@example.com", true, "Admin", true, "User", true, null, null, "ADMIN@EXAMPLE.COM", "ADMIN", "AQAAAAIAAYagAAAAEK95SlxvEPzqxJyTxIof0ufhmHVKdEGcuw7MxCBj92JUehpXlaMI0F4RrX3mzLDNzA==", null, false, "ebedf431-d435-4ac9-b3dc-24a20d452722", "ADMIN001", false, "admin" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { 1, 1 });

            migrationBuilder.InsertData(
                table: "SystemConfigs",
                columns: new[] { "ConfigId", "ConfigKey", "ConfigValue", "Description", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 100, "ScorePrecision", "0.5", "Number accuracy (0.25, 0.5, 1.0)", new DateTime(2025, 11, 24, 13, 4, 11, 199, DateTimeKind.Utc).AddTicks(1975), 1 },
                    { 101, "AISummaryMaxTokens", "1000", "Maximum number of tokens for AI summary", new DateTime(2025, 11, 24, 13, 4, 11, 199, DateTimeKind.Utc).AddTicks(1976), 1 },
                    { 102, "AISummaryMaxWords", "200", "Maximum word count for AI summary", new DateTime(2025, 11, 24, 13, 4, 11, 199, DateTimeKind.Utc).AddTicks(1977), 1 },
                    { 103, "DefaultPassThreshold", "50", "Ngưỡng điểm mặc định để Pass", new DateTime(2025, 11, 24, 13, 4, 11, 199, DateTimeKind.Utc).AddTicks(1978), 1 },
                    { 104, "PlagiarismThreshold", "80", "Maximum allowed plagiarism percentage before blocking submission (0-100)", new DateTime(2025, 11, 24, 13, 4, 11, 199, DateTimeKind.Utc).AddTicks(1978), 1 },
                    { 105, "RegradeProcessingDeadlineDays", "7", "Number of days for instructors to process regrade requests", new DateTime(2025, 11, 24, 13, 4, 11, 199, DateTimeKind.Utc).AddTicks(1979), 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_CampusId",
                table: "AcademicYears",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_AISummaries_Content",
                table: "AISummaries",
                column: "Content");

            migrationBuilder.CreateIndex(
                name: "IX_AISummaries_SubmissionId",
                table: "AISummaries",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CampusId",
                table: "AspNetUsers",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_MajorId",
                table: "AspNetUsers",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ClonedFromAssignmentId",
                table: "Assignments",
                column: "ClonedFromAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CourseInstanceId_Status",
                table: "Assignments",
                columns: new[] { "CourseInstanceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_Deadline",
                table: "Assignments",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_Description",
                table: "Assignments",
                column: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_FinalDeadline",
                table: "Assignments",
                column: "FinalDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_RubricTemplateId",
                table: "Assignments",
                column: "RubricTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_StartDate",
                table: "Assignments",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_Status",
                table: "Assignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_Title",
                table: "Assignments",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstances_CampusId",
                table: "CourseInstances",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstances_CourseId",
                table: "CourseInstances",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstances_SemesterId",
                table: "CourseInstances",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstructors_CourseInstanceId",
                table: "CourseInstructors",
                column: "CourseInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseInstructors_UserId",
                table: "CourseInstructors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CurriculumId",
                table: "Courses",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseStudents_ChangedByUserId",
                table: "CourseStudents",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseStudents_CourseInstanceId",
                table: "CourseStudents",
                column: "CourseInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseStudents_UserId",
                table: "CourseStudents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_CriteriaTemplateId",
                table: "Criteria",
                column: "CriteriaTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_RubricId",
                table: "Criteria",
                column: "RubricId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaFeedbacks_CriteriaId",
                table: "CriteriaFeedbacks",
                column: "CriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaFeedbacks_ReviewId",
                table: "CriteriaFeedbacks",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaTemplates_TemplateId",
                table: "CriteriaTemplates",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_CampusId",
                table: "Curriculums",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Curriculums_MajorId",
                table: "Curriculums",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_AssignmentId",
                table: "Notifications",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReviewAssignmentId",
                table: "Notifications",
                column: "ReviewAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SenderUserId",
                table: "Notifications",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SubmissionId",
                table: "Notifications",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RegradeRequests_CourseInstructorId",
                table: "RegradeRequests",
                column: "CourseInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_RegradeRequests_ReviewedByInstructorId",
                table: "RegradeRequests",
                column: "ReviewedByInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_RegradeRequests_ReviewedByUserId",
                table: "RegradeRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RegradeRequests_SubmissionId",
                table: "RegradeRequests",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAssignments_ReviewerUserId",
                table: "ReviewAssignments",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewAssignments_SubmissionId",
                table: "ReviewAssignments",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_GeneralFeedback",
                table: "Reviews",
                column: "GeneralFeedback");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReviewAssignmentId",
                table: "Reviews",
                column: "ReviewAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_AssignmentId",
                table: "Rubrics",
                column: "AssignmentId",
                unique: true,
                filter: "[AssignmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_TemplateId",
                table: "Rubrics",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricTemplates_CreatedByUserId",
                table: "RubricTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricTemplates_MajorId",
                table: "RubricTemplates",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_AcademicYearId",
                table: "Semesters",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignmentId",
                table: "Submissions",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_UserId",
                table: "Submissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_UpdatedByUserId",
                table: "SystemConfigs",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AISummaries");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CourseStudents");

            migrationBuilder.DropTable(
                name: "CriteriaFeedbacks");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RegradeRequests");

            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Criteria");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "CourseInstructors");

            migrationBuilder.DropTable(
                name: "CriteriaTemplates");

            migrationBuilder.DropTable(
                name: "Rubrics");

            migrationBuilder.DropTable(
                name: "ReviewAssignments");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "CourseInstances");

            migrationBuilder.DropTable(
                name: "RubricTemplates");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Curriculums");

            migrationBuilder.DropTable(
                name: "AcademicYears");

            migrationBuilder.DropTable(
                name: "Majors");

            migrationBuilder.DropTable(
                name: "Campuses");
        }
    }
}
