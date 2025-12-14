USE [LMS_ASDPRS]
GO

SET NOCOUNT ON;

-- ====================================================================================
-- SET UP VARIABLES & CONFIGURATION (Date: 2025-12-14)
-- ====================================================================================
DECLARE @CurrentDate DATETIME2 = '2025-12-14 09:00:00';
DECLARE @PasswordHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEK95SlxvEPzqxJyTxIof0ufhmHVKdEGcuw7MxCBj92JUehpXlaMI0F4RrX3mzLDNzA=='; -- Password: "12345aA@"

-- ID Variables
DECLARE @CampusId INT;
DECLARE @MajorSE_Id INT, @MajorIB_Id INT, @MajorBiz_Id INT;
DECLARE @InstructorId INT;
DECLARE @Student1_Id INT, @Student2_Id INT, @Student3_Id INT; 
DECLARE @Student4_Id INT, @Student5_Id INT, @Student6_Id INT; 
DECLARE @Student7_Id INT, @Student8_Id INT;
DECLARE @AcademicYearId INT, @SemesterId INT;
DECLARE @CourseSWT_Id INT, @CoursePRE_Id INT, @CourseEXE_Id INT, @CoursePending_Id INT;
DECLARE @ClassSWT_Id INT, @ClassSWT_Cross_Id INT, @ClassPRE_Id INT, @ClassEXE_Id INT, @ClassPending_Id INT;
DECLARE @RubricTemplateSWT_Id INT, @RubricTemplatePRE_Id INT, @RubricTemplateEXE_Id INT;
DECLARE @Ass_InReview_Id INT, @Ass_Graded_Id INT, @Ass_Published_Id INT;
DECLARE @RubricSWT_Id INT, @RubricPRE_Id INT, @RubricEXE_Id INT;

PRINT '>>> STARTING DATA POPULATION...';

-- ====================================================================================
-- 1. INFRASTRUCTURE DATA (Campus, Major)
-- ====================================================================================

-- 1.1 Campus (Fixed Unicode N'...')
IF NOT EXISTS (SELECT 1 FROM Campuses WHERE CampusName = N'Hồ Chí Minh')
BEGIN
    INSERT INTO Campuses (CampusName, Address) VALUES (N'Hồ Chí Minh', N'Khu Công Nghệ Cao, Q9');
    PRINT 'Inserted Campus: Hồ Chí Minh';
END

-- Retrieve CampusId accurately
SELECT TOP 1 @CampusId = CampusId FROM Campuses WHERE CampusName = N'Hồ Chí Minh';

-- Fallback: If still null (maybe name mismatch), pick the first available campus
IF @CampusId IS NULL
BEGIN
    SELECT TOP 1 @CampusId = CampusId FROM Campuses;
    PRINT 'Warning: Could not match exact CampusName. Using first available CampusId: ' + CAST(@CampusId AS NVARCHAR(10));
END

IF @CampusId IS NULL 
BEGIN 
    PRINT 'CRITICAL ERROR: No Campus found and creation failed. Stopping.'; 
    RETURN; 
END

-- 1.2 Majors
IF NOT EXISTS (SELECT 1 FROM Majors WHERE MajorCode = 'SE') INSERT INTO Majors (MajorName, MajorCode, IsActive) VALUES ('Software Engineering', 'SE', 1);
IF NOT EXISTS (SELECT 1 FROM Majors WHERE MajorCode = 'IB') INSERT INTO Majors (MajorName, MajorCode, IsActive) VALUES ('International Business', 'IB', 1);
IF NOT EXISTS (SELECT 1 FROM Majors WHERE MajorCode = 'ENT') INSERT INTO Majors (MajorName, MajorCode, IsActive) VALUES ('Entrepreneurship', 'ENT', 1);

SELECT @MajorSE_Id = MajorId FROM Majors WHERE MajorCode = 'SE';
SELECT @MajorIB_Id = MajorId FROM Majors WHERE MajorCode = 'IB';
SELECT @MajorBiz_Id = MajorId FROM Majors WHERE MajorCode = 'ENT';

-- ====================================================================================
-- 2. USERS
-- ====================================================================================

-- 2.1 Instructor
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'instructor_demo@asdprs.edu.vn')
BEGIN
    INSERT INTO AspNetUsers (CampusId, MajorId, FirstName, LastName, StudentCode, IsActive, CreatedAt, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
    VALUES (@CampusId, @MajorSE_Id, 'John', 'Teacher', 'INS001', 1, @CurrentDate, 'instructor_demo', 'INSTRUCTOR_DEMO', 'instructor_demo@asdprs.edu.vn', 'INSTRUCTOR_DEMO@ASDPRS.EDU.VN', 1, @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0);
    SET @InstructorId = SCOPE_IDENTITY();
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@InstructorId, 3); -- Role 3 = Instructor
END
ELSE
BEGIN
    SELECT @InstructorId = Id FROM AspNetUsers WHERE Email = 'instructor_demo@asdprs.edu.vn';
END

-- 2.2 Students Loop
IF OBJECT_ID('tempdb..#TempStudents') IS NOT NULL DROP TABLE #TempStudents;
CREATE TABLE #TempStudents (TempId INT IDENTITY(1,1), Name NVARCHAR(50), Email NVARCHAR(100), Code NVARCHAR(20));
INSERT INTO #TempStudents VALUES 
('Alice', 'student1@asdprs.edu.vn', 'STU001'), ('Bob', 'student2@asdprs.edu.vn', 'STU002'), ('Charlie', 'student3@asdprs.edu.vn', 'STU003'),
('David', 'student4@asdprs.edu.vn', 'STU004'), ('Eve', 'student5@asdprs.edu.vn', 'STU005'), ('Frank', 'student6@asdprs.edu.vn', 'STU006'),
('Grace', 'student7@asdprs.edu.vn', 'STU007'), ('Heidi', 'student8@asdprs.edu.vn', 'STU008');

DECLARE @i INT = 1, @MaxI INT = 8;
DECLARE @sName NVARCHAR(50), @sEmail NVARCHAR(100), @sCode NVARCHAR(20), @CurrId INT;

WHILE @i <= @MaxI
BEGIN
    SELECT @sName = Name, @sEmail = Email, @sCode = Code FROM #TempStudents WHERE TempId = @i;
    
    -- Check Exist
    SELECT @CurrId = Id FROM AspNetUsers WHERE Email = @sEmail;
    
    IF @CurrId IS NULL
    BEGIN
        INSERT INTO AspNetUsers (CampusId, MajorId, FirstName, LastName, StudentCode, IsActive, CreatedAt, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES (@CampusId, @MajorSE_Id, @sName, 'Student', @sCode, 1, @CurrentDate, LEFT(@sEmail, CHARINDEX('@', @sEmail)-1), UPPER(LEFT(@sEmail, CHARINDEX('@', @sEmail)-1)), @sEmail, UPPER(@sEmail), 1, @PasswordHash, NEWID(), NEWID(), 0, 0, 1, 0);
        SET @CurrId = SCOPE_IDENTITY();
        INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@CurrId, 2); -- Role 2 = Student
    END

    -- Assign specific variables
    IF @sCode = 'STU001' SET @Student1_Id = @CurrId;
    IF @sCode = 'STU002' SET @Student2_Id = @CurrId;
    IF @sCode = 'STU003' SET @Student3_Id = @CurrId;
    IF @sCode = 'STU004' SET @Student4_Id = @CurrId;
    IF @sCode = 'STU005' SET @Student5_Id = @CurrId;
    IF @sCode = 'STU006' SET @Student6_Id = @CurrId;
    IF @sCode = 'STU007' SET @Student7_Id = @CurrId;
    IF @sCode = 'STU008' SET @Student8_Id = @CurrId;

    SET @i = @i + 1;
    SET @CurrId = NULL; 
END
DROP TABLE #TempStudents;

-- ====================================================================================
-- 3. ACADEMIC YEAR & SEMESTER
-- ====================================================================================

IF NOT EXISTS (SELECT 1 FROM AcademicYears WHERE Name = '2025' AND CampusId = @CampusId)
    INSERT INTO AcademicYears (CampusId, Name, StartDate, EndDate) VALUES (@CampusId, '2025', '2025-01-01', '2025-12-31');
SELECT @AcademicYearId = AcademicYearId FROM AcademicYears WHERE Name = '2025' AND CampusId = @CampusId;

IF NOT EXISTS (SELECT 1 FROM Semesters WHERE Name = 'Fall 2025' AND AcademicYearId = @AcademicYearId)
    INSERT INTO Semesters (AcademicYearId, Name, StartDate, EndDate) VALUES (@AcademicYearId, 'Fall 2025', '2025-09-01', '2025-12-31');
SELECT @SemesterId = SemesterId FROM Semesters WHERE Name = 'Fall 2025' AND AcademicYearId = @AcademicYearId;

-- ====================================================================================
-- 4. COURSES & INSTANCES
-- ====================================================================================
-- Courses
IF NOT EXISTS (SELECT 1 FROM Courses WHERE CourseCode = 'SWT301') INSERT INTO Courses (CourseCode, CourseName, IsActive) VALUES ('SWT301', 'Software Testing', 1);
IF NOT EXISTS (SELECT 1 FROM Courses WHERE CourseCode = 'PRE203') INSERT INTO Courses (CourseCode, CourseName, IsActive) VALUES ('PRE203', 'Introduction to Public Relations', 1);
IF NOT EXISTS (SELECT 1 FROM Courses WHERE CourseCode = 'EXE101') INSERT INTO Courses (CourseCode, CourseName, IsActive) VALUES ('EXE101', 'Experiential Entrepreneurship 1', 1);
IF NOT EXISTS (SELECT 1 FROM Courses WHERE CourseCode = 'PEN101') INSERT INTO Courses (CourseCode, CourseName, IsActive) VALUES ('PEN101', 'Pending Subject', 1);

SELECT @CourseSWT_Id = CourseId FROM Courses WHERE CourseCode = 'SWT301';
SELECT @CoursePRE_Id = CourseId FROM Courses WHERE CourseCode = 'PRE203';
SELECT @CourseEXE_Id = CourseId FROM Courses WHERE CourseCode = 'EXE101';
SELECT @CoursePending_Id = CourseId FROM Courses WHERE CourseCode = 'PEN101';

-- Helper to safely insert Course Instance and get ID
IF NOT EXISTS (SELECT 1 FROM CourseInstances WHERE SectionCode = 'SE1801_SWT')
    INSERT INTO CourseInstances (CourseId, SemesterId, CampusId, SectionCode, EnrollmentPassword, StartDate, EndDate, IsActive, RequiresApproval, CreatedAt)
    VALUES (@CourseSWT_Id, @SemesterId, @CampusId, 'SE1801_SWT', '123456', '2025-09-05', '2025-12-30', 1, 0, @CurrentDate);
SELECT @ClassSWT_Id = CourseInstanceId FROM CourseInstances WHERE SectionCode = 'SE1801_SWT';

IF NOT EXISTS (SELECT 1 FROM CourseInstances WHERE SectionCode = 'SE1802_SWT')
    INSERT INTO CourseInstances (CourseId, SemesterId, CampusId, SectionCode, EnrollmentPassword, StartDate, EndDate, IsActive, RequiresApproval, CreatedAt)
    VALUES (@CourseSWT_Id, @SemesterId, @CampusId, 'SE1802_SWT', '123456', '2025-09-05', '2025-12-30', 1, 0, @CurrentDate);
SELECT @ClassSWT_Cross_Id = CourseInstanceId FROM CourseInstances WHERE SectionCode = 'SE1802_SWT';

IF NOT EXISTS (SELECT 1 FROM CourseInstances WHERE SectionCode = 'IB1801_PRE')
    INSERT INTO CourseInstances (CourseId, SemesterId, CampusId, SectionCode, EnrollmentPassword, StartDate, EndDate, IsActive, RequiresApproval, CreatedAt)
    VALUES (@CoursePRE_Id, @SemesterId, @CampusId, 'IB1801_PRE', '123456', '2025-09-05', '2025-12-30', 1, 0, @CurrentDate);
SELECT @ClassPRE_Id = CourseInstanceId FROM CourseInstances WHERE SectionCode = 'IB1801_PRE';

IF NOT EXISTS (SELECT 1 FROM CourseInstances WHERE SectionCode = 'SE1803_EXE')
    INSERT INTO CourseInstances (CourseId, SemesterId, CampusId, SectionCode, EnrollmentPassword, StartDate, EndDate, IsActive, RequiresApproval, CreatedAt)
    VALUES (@CourseEXE_Id, @SemesterId, @CampusId, 'SE1803_EXE', '123456', '2025-09-05', '2025-12-30', 1, 0, @CurrentDate);
SELECT @ClassEXE_Id = CourseInstanceId FROM CourseInstances WHERE SectionCode = 'SE1803_EXE';

IF NOT EXISTS (SELECT 1 FROM CourseInstances WHERE SectionCode = 'PENDING_01')
    INSERT INTO CourseInstances (CourseId, SemesterId, CampusId, SectionCode, EnrollmentPassword, StartDate, EndDate, IsActive, RequiresApproval, CreatedAt)
    VALUES (@CoursePending_Id, @SemesterId, @CampusId, 'PENDING_01', '123456', '2025-10-01', '2026-01-30', 1, 1, @CurrentDate);
SELECT @ClassPending_Id = CourseInstanceId FROM CourseInstances WHERE SectionCode = 'PENDING_01';

-- ====================================================================================
-- 5. ENROLLMENTS & INSTRUCTORS
-- ====================================================================================

-- Instructor Assignment (Using MERGE or IF NOT EXISTS to prevent duplicates)
IF NOT EXISTS (SELECT 1 FROM CourseInstructors WHERE CourseInstanceId = @ClassSWT_Id AND UserId = @InstructorId)
    INSERT INTO CourseInstructors (CourseInstanceId, UserId) VALUES (@ClassSWT_Id, @InstructorId);
IF NOT EXISTS (SELECT 1 FROM CourseInstructors WHERE CourseInstanceId = @ClassSWT_Cross_Id AND UserId = @InstructorId)
    INSERT INTO CourseInstructors (CourseInstanceId, UserId) VALUES (@ClassSWT_Cross_Id, @InstructorId);
IF NOT EXISTS (SELECT 1 FROM CourseInstructors WHERE CourseInstanceId = @ClassPRE_Id AND UserId = @InstructorId)
    INSERT INTO CourseInstructors (CourseInstanceId, UserId) VALUES (@ClassPRE_Id, @InstructorId);
IF NOT EXISTS (SELECT 1 FROM CourseInstructors WHERE CourseInstanceId = @ClassEXE_Id AND UserId = @InstructorId)
    INSERT INTO CourseInstructors (CourseInstanceId, UserId) VALUES (@ClassEXE_Id, @InstructorId);

-- Students Enrollment
-- Function to safely insert student
INSERT INTO CourseStudents (CourseInstanceId, UserId, EnrolledAt, Status, IsPassed)
SELECT T.CId, T.UId, @CurrentDate, T.Stat, 0
FROM (
    VALUES 
    (@ClassSWT_Id, @Student1_Id, 'Enrolled'), (@ClassPending_Id, @Student1_Id, 'Pending'),
    (@ClassSWT_Id, @Student2_Id, 'Enrolled'), (@ClassPending_Id, @Student2_Id, 'Pending'),
    (@ClassSWT_Id, @Student3_Id, 'Enrolled'), (@ClassPending_Id, @Student3_Id, 'Pending'),
    (@ClassSWT_Cross_Id, @Student4_Id, 'Enrolled'), 
    (@ClassPRE_Id, @Student4_Id, 'Enrolled'), (@ClassPending_Id, @Student4_Id, 'Pending'),
    (@ClassPRE_Id, @Student5_Id, 'Enrolled'), (@ClassPending_Id, @Student5_Id, 'Pending'),
    (@ClassPRE_Id, @Student6_Id, 'Enrolled'), (@ClassPending_Id, @Student6_Id, 'Pending'),
    (@ClassEXE_Id, @Student7_Id, 'Enrolled'), (@ClassPending_Id, @Student7_Id, 'Pending'),
    (@ClassEXE_Id, @Student8_Id, 'Enrolled'), (@ClassPending_Id, @Student8_Id, 'Pending')
) AS T(CId, UId, Stat)
WHERE NOT EXISTS (SELECT 1 FROM CourseStudents WHERE CourseInstanceId = T.CId AND UserId = T.UId);


-- ====================================================================================
-- 6. RUBRICS & ASSIGNMENTS
-- ====================================================================================

-- Template 1
IF NOT EXISTS (SELECT 1 FROM RubricTemplates WHERE Title = 'Software Testing Project Rubric' AND CreatedByUserId = @InstructorId)
BEGIN
    INSERT INTO RubricTemplates (Title, IsPublic, CreatedByUserId, CreatedAt, MajorId) VALUES ('Software Testing Project Rubric', 1, @InstructorId, @CurrentDate, @MajorSE_Id);
    SET @RubricTemplateSWT_Id = SCOPE_IDENTITY();
    INSERT INTO CriteriaTemplates (TemplateId, Title, Description, Weight, MaxScore, ScoringType, ScoreLabel) VALUES
    (@RubricTemplateSWT_Id, 'Test Cases Coverage', 'Completeness of test cases covering requirements', 40, 10, 'Scale', 'Points'),
    (@RubricTemplateSWT_Id, 'Bug Reporting', 'Clarity and reproducibility of bug reports', 30, 10, 'Scale', 'Points'),
    (@RubricTemplateSWT_Id, 'Automation Scripts', 'Quality and execution of automation code', 30, 10, 'Scale', 'Points');
END
ELSE SELECT @RubricTemplateSWT_Id = TemplateId FROM RubricTemplates WHERE Title = 'Software Testing Project Rubric' AND CreatedByUserId = @InstructorId;

-- Template 2
IF NOT EXISTS (SELECT 1 FROM RubricTemplates WHERE Title = 'Public Relations Plan Rubric' AND CreatedByUserId = @InstructorId)
BEGIN
    INSERT INTO RubricTemplates (Title, IsPublic, CreatedByUserId, CreatedAt, MajorId) VALUES ('Public Relations Plan Rubric', 1, @InstructorId, @CurrentDate, @MajorIB_Id);
    SET @RubricTemplatePRE_Id = SCOPE_IDENTITY();
    INSERT INTO CriteriaTemplates (TemplateId, Title, Description, Weight, MaxScore, ScoringType, ScoreLabel) VALUES
    (@RubricTemplatePRE_Id, 'Strategy', 'Depth of PR strategy', 50, 10, 'Scale', 'Points'),
    (@RubricTemplatePRE_Id, 'Creativity', 'Originality of the campaign', 30, 10, 'Scale', 'Points'),
    (@RubricTemplatePRE_Id, 'Formatting', 'Adherence to document standards', 20, 10, 'Scale', 'Points');
END
ELSE SELECT @RubricTemplatePRE_Id = TemplateId FROM RubricTemplates WHERE Title = 'Public Relations Plan Rubric' AND CreatedByUserId = @InstructorId;

-- Template 3
IF NOT EXISTS (SELECT 1 FROM RubricTemplates WHERE Title = 'Startup Pitch Deck Rubric' AND CreatedByUserId = @InstructorId)
BEGIN
    INSERT INTO RubricTemplates (Title, IsPublic, CreatedByUserId, CreatedAt, MajorId) VALUES ('Startup Pitch Deck Rubric', 1, @InstructorId, @CurrentDate, @MajorBiz_Id);
    SET @RubricTemplateEXE_Id = SCOPE_IDENTITY();
    INSERT INTO CriteriaTemplates (TemplateId, Title, Description, Weight, MaxScore, ScoringType, ScoreLabel) VALUES
    (@RubricTemplateEXE_Id, 'Business Model', 'Viability of the business model', 60, 10, 'Scale', 'Points'),
    (@RubricTemplateEXE_Id, 'Presentation', 'Clarity and flow of pitch', 40, 10, 'Scale', 'Points');
END
ELSE SELECT @RubricTemplateEXE_Id = TemplateId FROM RubricTemplates WHERE Title = 'Startup Pitch Deck Rubric' AND CreatedByUserId = @InstructorId;


-- 6.1 Assignment InReview
IF NOT EXISTS (SELECT 1 FROM Assignments WHERE Title = 'Assignment 2: Selenium Testing' AND CourseInstanceId = @ClassSWT_Id)
BEGIN
    INSERT INTO Assignments (CourseInstanceId, RubricTemplateId, Title, Description, Guidelines, CreatedAt, StartDate, Deadline, ReviewDeadline, FinalDeadline, NumPeerReviewsRequired, AllowCrossClass, CrossClassTag, IsBlindReview, InstructorWeight, PeerWeight, GradingScale, IncludeAIScore, Status)
    VALUES (@ClassSWT_Id, @RubricTemplateSWT_Id, 'Assignment 2: Selenium Testing', 'Submit your automation project', 'Upload zip or docx', DATEADD(day, -10, @CurrentDate), DATEADD(day, -10, @CurrentDate), DATEADD(day, -1, @CurrentDate), DATEADD(day, 5, @CurrentDate), DATEADD(day, 6, @CurrentDate), 1, 1, '#TEST_DEC25', 1, 60, 40, 'Scale10', 1, 'InReview');
    SET @Ass_InReview_Id = SCOPE_IDENTITY();

    INSERT INTO Rubrics (TemplateId, AssignmentId, Title, IsModified) VALUES (@RubricTemplateSWT_Id, @Ass_InReview_Id, 'Software Testing Project Rubric', 0);
    SET @RubricSWT_Id = SCOPE_IDENTITY();
    INSERT INTO Criteria (RubricId, CriteriaTemplateId, Title, Description, Weight, MaxScore, ScoringType, ScoreLabel, IsModified)
    SELECT @RubricSWT_Id, CriteriaTemplateId, Title, Description, Weight, MaxScore, ScoringType, ScoreLabel, 0 FROM CriteriaTemplates WHERE TemplateId = @RubricTemplateSWT_Id;
    UPDATE Assignments SET RubricId = @RubricSWT_Id WHERE AssignmentId = @Ass_InReview_Id;
END
ELSE SELECT @Ass_InReview_Id = AssignmentId FROM Assignments WHERE Title = 'Assignment 2: Selenium Testing' AND CourseInstanceId = @ClassSWT_Id;

-- 6.1.1 Cross Class Assignment
IF NOT EXISTS (SELECT 1 FROM Assignments WHERE Title = 'Assignment 2: Selenium Testing (Cross)' AND CourseInstanceId = @ClassSWT_Cross_Id)
BEGIN
    INSERT INTO Assignments (CourseInstanceId, RubricTemplateId, Title, Description, Guidelines, CreatedAt, StartDate, Deadline, ReviewDeadline, FinalDeadline, NumPeerReviewsRequired, AllowCrossClass, CrossClassTag, IsBlindReview, InstructorWeight, PeerWeight, GradingScale, IncludeAIScore, Status)
    VALUES (@ClassSWT_Cross_Id, @RubricTemplateSWT_Id, 'Assignment 2: Selenium Testing (Cross)', 'Same specs', 'Upload zip or docx', DATEADD(day, -10, @CurrentDate), DATEADD(day, -10, @CurrentDate), DATEADD(day, -1, @CurrentDate), DATEADD(day, 5, @CurrentDate), DATEADD(day, 6, @CurrentDate), 1, 1, '#TEST_DEC25', 1, 60, 40, 'Scale10', 1, 'InReview');
END

-- 6.2 Assignment Graded
IF NOT EXISTS (SELECT 1 FROM Assignments WHERE Title = 'Crisis Management Plan' AND CourseInstanceId = @ClassPRE_Id)
BEGIN
    INSERT INTO Assignments (CourseInstanceId, RubricTemplateId, Title, Description, Guidelines, CreatedAt, StartDate, Deadline, ReviewDeadline, FinalDeadline, NumPeerReviewsRequired, AllowCrossClass, IsBlindReview, InstructorWeight, PeerWeight, GradingScale, IncludeAIScore, Status)
    VALUES (@ClassPRE_Id, @RubricTemplatePRE_Id, 'Crisis Management Plan', 'Draft a plan for a mock crisis', 'Upload docx', DATEADD(day, -30, @CurrentDate), DATEADD(day, -30, @CurrentDate), DATEADD(day, -10, @CurrentDate), DATEADD(day, -5, @CurrentDate), DATEADD(day, -4, @CurrentDate), 1, 0, 1, 70, 30, 'Scale10', 0, 'Closed');
    SET @Ass_Graded_Id = SCOPE_IDENTITY();

    INSERT INTO Rubrics (TemplateId, AssignmentId, Title, IsModified) VALUES (@RubricTemplatePRE_Id, @Ass_Graded_Id, 'Public Relations Plan Rubric', 0);
    SET @RubricPRE_Id = SCOPE_IDENTITY();
    INSERT INTO Criteria (RubricId, CriteriaTemplateId, Title, Description, Weight, MaxScore, ScoringType, ScoreLabel, IsModified)
    SELECT @RubricPRE_Id, CriteriaTemplateId, Title, Description, Weight, MaxScore, ScoringType, ScoreLabel, 0 FROM CriteriaTemplates WHERE TemplateId = @RubricTemplatePRE_Id;
    UPDATE Assignments SET RubricId = @RubricPRE_Id WHERE AssignmentId = @Ass_Graded_Id;
END
ELSE SELECT @Ass_Graded_Id = AssignmentId FROM Assignments WHERE Title = 'Crisis Management Plan' AND CourseInstanceId = @ClassPRE_Id;

-- 6.3 Assignment Published
IF NOT EXISTS (SELECT 1 FROM Assignments WHERE Title = 'Pitch Deck Final' AND CourseInstanceId = @ClassEXE_Id)
BEGIN
    INSERT INTO Assignments (CourseInstanceId, RubricTemplateId, Title, Description, Guidelines, CreatedAt, StartDate, Deadline, ReviewDeadline, FinalDeadline, NumPeerReviewsRequired, AllowCrossClass, IsBlindReview, InstructorWeight, PeerWeight, GradingScale, IncludeAIScore, Status)
    VALUES (@ClassEXE_Id, @RubricTemplateEXE_Id, 'Pitch Deck Final', 'Final submission', 'Upload pptx/pdf', DATEADD(day, -40, @CurrentDate), DATEADD(day, -40, @CurrentDate), DATEADD(day, -20, @CurrentDate), DATEADD(day, -15, @CurrentDate), DATEADD(day, -14, @CurrentDate), 1, 0, 1, 50, 50, 'Scale10', 0, 'GradesPublished');
    SET @Ass_Published_Id = SCOPE_IDENTITY();

    INSERT INTO Rubrics (TemplateId, AssignmentId, Title, IsModified) VALUES (@RubricTemplateEXE_Id, @Ass_Published_Id, 'Startup Pitch Deck Rubric', 0);
    SET @RubricEXE_Id = SCOPE_IDENTITY();
    INSERT INTO Criteria (RubricId, CriteriaTemplateId, Title, Description, Weight, MaxScore, ScoringType, ScoreLabel, IsModified)
    SELECT @RubricEXE_Id, CriteriaTemplateId, Title, Description, Weight, MaxScore, ScoringType, ScoreLabel, 0 FROM CriteriaTemplates WHERE TemplateId = @RubricTemplateEXE_Id;
    UPDATE Assignments SET RubricId = @RubricEXE_Id WHERE AssignmentId = @Ass_Published_Id;
END
ELSE SELECT @Ass_Published_Id = AssignmentId FROM Assignments WHERE Title = 'Pitch Deck Final' AND CourseInstanceId = @ClassEXE_Id;


-- ====================================================================================
-- 7. SUBMISSIONS
-- ====================================================================================

-- 7.1 InReview (SWT301)
IF NOT EXISTS (SELECT 1 FROM Submissions WHERE AssignmentId = @Ass_InReview_Id AND UserId = @Student1_Id)
    INSERT INTO Submissions (AssignmentId, UserId, FileUrl, FileName, OriginalFileName, Keywords, SubmittedAt, Status, IsPublic)
    VALUES (@Ass_InReview_Id, @Student1_Id, 'https://yznanpovvpvcqtblwggk.supabase.co/storage/v1/object/public/files/submissions/11/2/1218d24a-c8cf-4a49-9907-fcd4287e2caf_The Gold Standard of Crisis Management.docx?', 'The_Gold_Standard_of_Crisis_Management.docx', 'The Gold Standard of Crisis Management.docx', 'Crisis, Management, PR', DATEADD(day, -2, @CurrentDate), 'Submitted', 1);

IF NOT EXISTS (SELECT 1 FROM Submissions WHERE AssignmentId = @Ass_InReview_Id AND UserId = @Student2_Id)
    INSERT INTO Submissions (AssignmentId, UserId, FileUrl, FileName, OriginalFileName, Keywords, SubmittedAt, Status, IsPublic)
    VALUES (@Ass_InReview_Id, @Student2_Id, 'https://yznanpovvpvcqtblwggk.supabase.co/storage/v1/object/public/files/dummy_project.zip', 'Project_Source.zip', 'Project_Source.zip', 'Automation, Selenium', DATEADD(day, -3, @CurrentDate), 'Submitted', 1);

IF NOT EXISTS (SELECT 1 FROM Submissions WHERE AssignmentId = @Ass_InReview_Id AND UserId = @Student3_Id)
    INSERT INTO Submissions (AssignmentId, UserId, FileUrl, FileName, OriginalFileName, Keywords, SubmittedAt, Status, IsPublic)
    VALUES (@Ass_InReview_Id, @Student3_Id, 'https://yznanpovvpvcqtblwggk.supabase.co/storage/v1/object/public/files/submissions/11/9/05db05f7-59a9-4f04-b2fd-1e5e8fa9d76b_Classic Spaghetti Carbonara.docx?', 'Classic_Spaghetti_Carbonara.docx', 'Classic Spaghetti Carbonara.docx', 'Recipe, Cooking', DATEADD(day, -2, @CurrentDate), 'Submitted', 1);

-- 7.2 Graded (PRE203)
DECLARE @Sub4_Id INT;
IF NOT EXISTS (SELECT 1 FROM Submissions WHERE AssignmentId = @Ass_Graded_Id AND UserId = @Student4_Id)
BEGIN
    INSERT INTO Submissions (AssignmentId, UserId, FileUrl, FileName, OriginalFileName, Keywords, SubmittedAt, Status, IsPublic, InstructorScore, PeerAverageScore, FinalScore, GradedAt)
    VALUES (@Ass_Graded_Id, @Student4_Id, 'https://dummy_url_1', 'Plan_A.docx', 'Plan_A.docx', 'PR', DATEADD(day, -12, @CurrentDate), 'Graded', 1, 8.5, 9.0, 8.65, DATEADD(day, -2, @CurrentDate));
    SET @Sub4_Id = SCOPE_IDENTITY();
END
ELSE SELECT @Sub4_Id = SubmissionId FROM Submissions WHERE AssignmentId = @Ass_Graded_Id AND UserId = @Student4_Id;

IF NOT EXISTS (SELECT 1 FROM Submissions WHERE AssignmentId = @Ass_Graded_Id AND UserId = @Student5_Id)
    INSERT INTO Submissions (AssignmentId, UserId, FileUrl, FileName, OriginalFileName, Keywords, SubmittedAt, Status, IsPublic, InstructorScore, PeerAverageScore, FinalScore, GradedAt)
    VALUES (@Ass_Graded_Id, @Student5_Id, 'https://dummy_url_2', 'Plan_B.docx', 'Plan_B.docx', 'PR', DATEADD(day, -11, @CurrentDate), 'Graded', 1, 7.0, 0, 7.0, DATEADD(day, -2, @CurrentDate));

IF NOT EXISTS (SELECT 1 FROM Submissions WHERE AssignmentId = @Ass_Graded_Id AND UserId = @Student6_Id)
    INSERT INTO Submissions (AssignmentId, UserId, FileUrl, FileName, OriginalFileName, Keywords, SubmittedAt, Status, IsPublic, InstructorScore, PeerAverageScore, FinalScore, GradedAt, Feedback)
    VALUES (@Ass_Graded_Id, @Student6_Id, 'Not Submitted', 'Not Submitted', 'Not Submitted', '', DATEADD(day, -10, @CurrentDate), 'Graded', 1, 0, 0, 0, DATEADD(day, -2, @CurrentDate), 'Auto grade zero due to non-submission');

-- 7.3 Published (EXE101)
DECLARE @Sub7_Id INT, @Sub8_Id INT;
IF NOT EXISTS (SELECT 1 FROM Submissions WHERE AssignmentId = @Ass_Published_Id AND UserId = @Student7_Id)
BEGIN
    INSERT INTO Submissions (AssignmentId, UserId, FileUrl, FileName, OriginalFileName, Keywords, SubmittedAt, Status, IsPublic, InstructorScore, PeerAverageScore, FinalScore, GradedAt)
    VALUES (@Ass_Published_Id, @Student7_Id, 'https://dummy_url_3', 'Pitch_Final.pptx', 'Pitch_Final.pptx', 'Startup', DATEADD(day, -25, @CurrentDate), 'Graded', 1, 6.0, 6.5, 6.25, DATEADD(day, -10, @CurrentDate));
    SET @Sub7_Id = SCOPE_IDENTITY();
END
ELSE SELECT @Sub7_Id = SubmissionId FROM Submissions WHERE AssignmentId = @Ass_Published_Id AND UserId = @Student7_Id;

IF NOT EXISTS (SELECT 1 FROM Submissions WHERE AssignmentId = @Ass_Published_Id AND UserId = @Student8_Id)
BEGIN
    INSERT INTO Submissions (AssignmentId, UserId, FileUrl, FileName, OriginalFileName, Keywords, SubmittedAt, Status, IsPublic, InstructorScore, PeerAverageScore, FinalScore, GradedAt, Feedback)
    VALUES (@Ass_Published_Id, @Student8_Id, 'https://dummy_url_4', 'Pitch_v1.pdf', 'Pitch_v1.pdf', 'Startup', DATEADD(day, -22, @CurrentDate), 'Graded', 1, 0, 0, 0, DATEADD(day, -10, @CurrentDate), 'File corrupted');
    SET @Sub8_Id = SCOPE_IDENTITY();
END
ELSE SELECT @Sub8_Id = SubmissionId FROM Submissions WHERE AssignmentId = @Ass_Published_Id AND UserId = @Student8_Id;


-- ====================================================================================
-- 8. REVIEWS & REGRADE
-- ====================================================================================

-- 8.1 Reviews for Student 4 (Graded Ass)
DECLARE @RA_4_By_5 INT;
IF NOT EXISTS (SELECT 1 FROM ReviewAssignments WHERE SubmissionId = @Sub4_Id AND ReviewerUserId = @Student5_Id)
BEGIN
    INSERT INTO ReviewAssignments (SubmissionId, ReviewerUserId, Status, AssignedAt, Deadline, IsAIReview)
    VALUES (@Sub4_Id, @Student5_Id, 'Completed', DATEADD(day, -9, @CurrentDate), DATEADD(day, -5, @CurrentDate), 0);
    SET @RA_4_By_5 = SCOPE_IDENTITY();

    INSERT INTO Reviews (ReviewAssignmentId, OverallScore, GeneralFeedback, ReviewedAt, ReviewType, FeedbackSource)
    VALUES (@RA_4_By_5, 9.0, 'Excellent work on the crisis strategy.', DATEADD(day, -6, @CurrentDate), 'Peer', 'Student');
END

DECLARE @RA_4_By_Ins INT;
IF NOT EXISTS (SELECT 1 FROM ReviewAssignments WHERE SubmissionId = @Sub4_Id AND ReviewerUserId = @InstructorId)
BEGIN
    INSERT INTO ReviewAssignments (SubmissionId, ReviewerUserId, Status, AssignedAt, Deadline, IsAIReview)
    VALUES (@Sub4_Id, @InstructorId, 'Completed', DATEADD(day, -4, @CurrentDate), DATEADD(day, -1, @CurrentDate), 0);
    SET @RA_4_By_Ins = SCOPE_IDENTITY();

    INSERT INTO Reviews (ReviewAssignmentId, OverallScore, GeneralFeedback, ReviewedAt, ReviewType, FeedbackSource)
    VALUES (@RA_4_By_Ins, 8.5, 'Good structure, but formatting needs work.', DATEADD(day, -2, @CurrentDate), 'Instructor', 'Instructor');
END

-- 8.2 Regrade Requests (Published Ass)
IF NOT EXISTS (SELECT 1 FROM RegradeRequests WHERE SubmissionId = @Sub7_Id)
    INSERT INTO RegradeRequests (SubmissionId, Reason, Status, RequestedAt)
    VALUES (@Sub7_Id, 'I believe my business model analysis was deeper than graded.', 'Pending', DATEADD(day, -1, @CurrentDate));

IF NOT EXISTS (SELECT 1 FROM RegradeRequests WHERE SubmissionId = @Sub8_Id)
    INSERT INTO RegradeRequests (SubmissionId, Reason, Status, RequestedAt)
    VALUES (@Sub8_Id, 'I uploaded the wrong file version, here is the correct one link...', 'Pending', DATEADD(day, -1, @CurrentDate));

PRINT '>>> DATA POPULATION COMPLETED SUCCESSFULLY.';
GO