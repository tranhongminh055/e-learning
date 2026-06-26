USE [E-LEARNING];
GO
IF OBJECT_ID('account_change_logs','U') IS NOT NULL DROP TABLE account_change_logs;
IF OBJECT_ID('exam_results','U') IS NOT NULL DROP TABLE exam_results;
IF OBJECT_ID('attendance','U') IS NOT NULL DROP TABLE attendance;
IF OBJECT_ID('lessons','U') IS NOT NULL DROP TABLE lessons;
IF OBJECT_ID('monitoring_logs','U') IS NOT NULL DROP TABLE monitoring_logs;
IF OBJECT_ID('assignments','U') IS NOT NULL DROP TABLE assignments;
IF OBJECT_ID('sessions','U') IS NOT NULL DROP TABLE sessions;
IF OBJECT_ID('users','U') IS NOT NULL DROP TABLE users;
GO

CREATE TABLE users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    full_name NVARCHAR(100) NOT NULL,
    email NVARCHAR(150) UNIQUE NOT NULL,
    username NVARCHAR(50) UNIQUE NOT NULL,
    student_id NVARCHAR(20) NOT NULL,
    password_hash NVARCHAR(255) NOT NULL,
    role NVARCHAR(10) NOT NULL DEFAULT 'student' CHECK (role IN ('student','teacher')),
    is_active BIT DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE()
);
GO
CREATE TABLE sessions (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    token NVARCHAR(MAX) NOT NULL,
    ip_address NVARCHAR(45),
    login_at DATETIME DEFAULT GETDATE(),
    is_active BIT DEFAULT 1,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
GO
CREATE TABLE assignments (
    id INT IDENTITY(1,1) PRIMARY KEY,
    title NVARCHAR(200) NOT NULL,
    description NVARCHAR(MAX),
    status NVARCHAR(50) DEFAULT 'active',
    exam_type NVARCHAR(50),
    is_online_exam BIT DEFAULT 1,
    open_date NVARCHAR(100),
    due_date NVARCHAR(100),
    last_modified_by NVARCHAR(100),
    modified_date NVARCHAR(100),
    file_name NVARCHAR(500),
    questions_count INT DEFAULT 0,
    created_at DATETIME DEFAULT GETDATE()
);
GO
CREATE TABLE exam_results (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL,
    assignment_title NVARCHAR(200),
    score DECIMAL(5,2) DEFAULT 0,
    feedback NVARCHAR(MAX),
    submitted_at DATETIME DEFAULT GETDATE()
);
GO
CREATE TABLE attendance (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL,
    fullname NVARCHAR(100),
    similarity DECIMAL(5,2) DEFAULT 0,
    is_match BIT DEFAULT 0,
    verdict NVARCHAR(50),
    [timestamp] DATETIME DEFAULT GETDATE()
);
GO
CREATE TABLE lessons (
    id INT IDENTITY(1,1) PRIMARY KEY,
    title NVARCHAR(200) NOT NULL,
    subject NVARCHAR(100),
    access_level NVARCHAR(50) DEFAULT 'Public',
    upload_date NVARCHAR(50),
    uploaded_by NVARCHAR(100),
    file_size NVARCHAR(20),
    file_path NVARCHAR(500),
    created_at DATETIME DEFAULT GETDATE()
);
GO
CREATE TABLE monitoring_logs (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    screenshot_path NVARCHAR(500),
    is_violation BIT DEFAULT 0,
    violation_type NVARCHAR(50),
    captured_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
GO
CREATE TABLE account_change_logs (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL,
    changed_field NVARCHAR(50),
    old_value NVARCHAR(MAX),
    new_value NVARCHAR(MAX),
    changed_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
GO

CREATE OR ALTER TRIGGER trg_lock_student_on_update ON users AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i INNER JOIN deleted d ON i.id=d.id
        WHERE d.role='student' AND (d.full_name!=i.full_name OR d.email!=i.email OR d.username!=i.username OR d.student_id!=i.student_id OR d.password_hash!=i.password_hash))
    BEGIN
        INSERT INTO account_change_logs (user_id,changed_field,old_value,new_value)
        SELECT d.id,
            CASE WHEN d.full_name!=i.full_name THEN 'full_name' WHEN d.email!=i.email THEN 'email' WHEN d.username!=i.username THEN 'username' WHEN d.student_id!=i.student_id THEN 'student_id' ELSE 'password_hash' END,
            CASE WHEN d.full_name!=i.full_name THEN d.full_name WHEN d.email!=i.email THEN d.email WHEN d.username!=i.username THEN d.username WHEN d.student_id!=i.student_id THEN d.student_id ELSE N'***' END,
            CASE WHEN d.full_name!=i.full_name THEN i.full_name WHEN d.email!=i.email THEN i.email WHEN d.username!=i.username THEN i.username WHEN d.student_id!=i.student_id THEN i.student_id ELSE N'***' END
        FROM inserted i INNER JOIN deleted d ON i.id=d.id
        WHERE d.role='student' AND (d.full_name!=i.full_name OR d.email!=i.email OR d.username!=i.username OR d.student_id!=i.student_id OR d.password_hash!=i.password_hash);
        UPDATE u SET u.is_active=0, u.updated_at=GETDATE() FROM users u
        INNER JOIN inserted i ON u.id=i.id INNER JOIN deleted d ON u.id=d.id
        WHERE d.role='student' AND (d.full_name!=i.full_name OR d.email!=i.email OR d.username!=i.username OR d.student_id!=i.student_id OR d.password_hash!=i.password_hash);
    END
END;
GO
INSERT INTO users (full_name,email,username,student_id,password_hash,role,is_active,created_at,updated_at) VALUES (N'admin',N'mt1479233@gmail.com',N'admin123',N'29210247142',N'ddfa08f04ffbedd937ce079026ead9826c0f4572feee5e45ff2a66d058c0c9d5',N'teacher',1,N'2026-06-10 12:00:59',N'2026-06-10 12:00:59');
INSERT INTO users (full_name,email,username,student_id,password_hash,role,is_active,created_at,updated_at) VALUES (N'TRAN HONG MINH',N'tranhongminh@dtu.edu.vn',N'tranhongminh',N'29210247142',N'30fbe3e1ab2d0995251d1b02e1d9c4357158ad8a25ab48e1e3d4a0221263591e',N'student',1,N'2026-06-11 01:11:03',N'2026-06-11 01:11:03');
GO
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ',N'127.0.0.1',N'2026-06-11 12:46:51',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ',N'192.168.110.53',N'2026-06-11 16:08:55',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'192.168.110.53',N'2026-06-11 16:10:44',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ',N'172.22.83.43',N'2026-06-12 00:54:57',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'172.22.83.43',N'2026-06-12 00:56:15',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'172.22.83.43',N'2026-06-12 00:57:05',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ',N'172.22.83.43',N'2026-06-12 01:47:30',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'172.22.83.43',N'2026-06-12 01:47:57',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ',N'172.22.83.43',N'2026-06-12 01:52:39',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'172.22.83.43',N'2026-06-12 01:53:04',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'127.0.0.1',N'2026-06-12 03:02:41',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ',N'127.0.0.1',N'2026-06-12 03:04:10',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'127.0.0.1',N'2026-06-12 03:08:31',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ',N'127.0.0.1',N'2026-06-12 03:09:27',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'127.0.0.1',N'2026-06-12 08:19:21',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ',N'127.0.0.1',N'2026-06-12 09:03:13',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'127.0.0.1',N'2026-06-12 09:04:40',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'127.0.0.1',N'2026-06-12 09:16:36',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ',N'127.0.0.1',N'2026-06-12 09:22:56',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,N'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN',N'127.0.0.1',N'2026-06-12 09:24:35',1);
GO
INSERT INTO assignments (title,description,status,exam_type,is_online_exam,open_date,due_date,last_modified_by,modified_date,file_name,questions_count) VALUES (N'kiểm tra',N'bài kiểm tra',N'Just Uploaded',N'Trắc nghiệm',1,N'Jun 13, 2026, 19:35',N'Jun 13, 2026, 19:40',N'CURRENT INSTRUCTOR',N'Jun 13, 2026, 7:35 PM',N'C:\Users\Admin\Downloads\trắc nghiệm.docx',101);
GO
INSERT INTO lessons (title,subject,access_level,upload_date,uploaded_by,file_size,file_path) VALUES (N'Process Management Presentation.pdf',N'CMU-SE 433 SGIS (2026SU)',N'Public',N'14/06/2026',N'Giảng viên',N'2056 KB',N'C:\Users\Admin\Downloads\Process Management Presentation.pdf');
GO
INSERT INTO exam_results (username,assignment_title,score,feedback,submitted_at) VALUES (N'phantanphuc',N'kiểm tra',9,N'Hệ thống AI đã phân tích biểu đồ năng lực.
Điểm số: 9/100. 
Nhận xét: Kém, bạn bị hổng kiến thức nghiêm trọng.',N'2026-06-13 19:38:18');
GO
INSERT INTO attendance (username,fullname,similarity,is_match,verdict,[timestamp]) VALUES (N'phantanphuc',N'phantanphuc',98.19,1,N'Co mat',N'2026-06-13 16:26:55');
INSERT INTO attendance (username,fullname,similarity,is_match,verdict,[timestamp]) VALUES (N'phantanphuc',N'phantanphuc',98.69,1,N'Có mặt',N'2026-06-13 16:41:17');
INSERT INTO attendance (username,fullname,similarity,is_match,verdict,[timestamp]) VALUES (N'phantanphuc',N'phantanphuc',93.78,1,N'Có mặt',N'2026-06-14 05:43:28');
GO
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174401.jpg',1,N'TabSwitch',N'2026-06-11 10:44:01');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174410.jpg',1,N'TabSwitch',N'2026-06-11 10:44:10');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174449.jpg',1,N'TabSwitch',N'2026-06-11 10:44:49');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174455.jpg',1,N'TabSwitch',N'2026-06-11 10:44:55');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174501.jpg',1,N'TabSwitch',N'2026-06-11 10:45:01');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174547.jpg',1,N'TabSwitch',N'2026-06-11 10:45:47');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174554.jpg',1,N'TabSwitch',N'2026-06-11 10:45:54');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174606.jpg',1,N'TabSwitch',N'2026-06-11 10:46:06');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174616.jpg',1,N'TabSwitch',N'2026-06-11 10:46:16');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174627.jpg',1,N'TabSwitch',N'2026-06-11 10:46:27');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174630.jpg',1,N'TabSwitch',N'2026-06-11 10:46:30');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611174645.jpg',1,N'TabSwitch',N'2026-06-11 10:46:45');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611182834.jpg',1,N'TabSwitch',N'2026-06-11 11:28:34');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611182848.jpg',1,N'TabSwitch',N'2026-06-11 11:28:48');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611182851.jpg',1,N'TabSwitch',N'2026-06-11 11:28:51');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611182916.jpg',1,N'TabSwitch',N'2026-06-11 11:29:16');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611182919.jpg',1,N'TabSwitch',N'2026-06-11 11:29:19');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611182924.jpg',1,N'TabSwitch',N'2026-06-11 11:29:24');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611182928.jpg',1,N'TabSwitch',N'2026-06-11 11:29:28');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611183640.jpg',1,N'TabSwitch',N'2026-06-11 11:36:40');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611183715.jpg',1,N'TabSwitch',N'2026-06-11 11:37:15');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611193123.jpg',1,N'TabSwitch',N'2026-06-11 12:31:23');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611193228.jpg',1,N'TabSwitch',N'2026-06-11 12:32:28');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611193234.jpg',1,N'TabSwitch',N'2026-06-11 12:32:34');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611193250.jpg',1,N'TabSwitch',N'2026-06-11 12:32:50');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611193359.jpg',1,N'TabSwitch',N'2026-06-11 12:33:59');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611193502.jpg',1,N'TabSwitch',N'2026-06-11 12:35:02');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611193517.jpg',1,N'TabSwitch',N'2026-06-11 12:35:17');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611194655.jpg',1,N'TabSwitch',N'2026-06-11 12:46:55');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611194719.jpg',1,N'TabSwitch',N'2026-06-11 12:47:19');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611230930.jpg',1,N'TabSwitch',N'2026-06-11 16:09:30');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611230931.jpg',1,N'TabSwitch',N'2026-06-11 16:09:31');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260611231147.jpg',1,N'TabSwitch',N'2026-06-11 16:11:47');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612075506.jpg',1,N'TabSwitch',N'2026-06-12 00:55:06');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612075526.jpg',1,N'TabSwitch',N'2026-06-12 00:55:26');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612075526.jpg',1,N'TabSwitch',N'2026-06-12 00:55:26');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612075532.jpg',1,N'TabSwitch',N'2026-06-12 00:55:32');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612075532.jpg',1,N'TabSwitch',N'2026-06-12 00:55:32');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612075544.jpg',1,N'TabSwitch',N'2026-06-12 00:55:44');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612075554.jpg',1,N'TabSwitch',N'2026-06-12 00:55:54');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612075758.jpg',1,N'TabSwitch',N'2026-06-12 00:57:58');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612084736.jpg',1,N'TabSwitch',N'2026-06-12 01:47:36');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612085001.jpg',1,N'TabSwitch',N'2026-06-12 01:50:01');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612085245.jpg',1,N'TabSwitch',N'2026-06-12 01:52:45');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612100419.jpg',1,N'TabSwitch',N'2026-06-12 03:04:19');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612100423.jpg',1,N'TabSwitch',N'2026-06-12 03:04:23');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612100432.jpg',1,N'TabSwitch',N'2026-06-12 03:04:32');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612100437.jpg',1,N'TabSwitch',N'2026-06-12 03:04:37');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612100502.jpg',1,N'TabSwitch',N'2026-06-12 03:05:02');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612100934.jpg',1,N'TabSwitch',N'2026-06-12 03:09:34');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612101001.jpg',1,N'TabSwitch',N'2026-06-12 03:10:01');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612101004.jpg',1,N'TabSwitch',N'2026-06-12 03:10:04');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612101045.jpg',1,N'TabSwitch',N'2026-06-12 03:10:45');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612160520.jpg',1,N'TabSwitch',N'2026-06-12 09:05:20');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612160528.jpg',1,N'TabSwitch',N'2026-06-12 09:05:28');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,N'uploads\tranhongminh_20260612162303.jpg',1,N'TabSwitch',N'2026-06-12 09:23:03');
GO
