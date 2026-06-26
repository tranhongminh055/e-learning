USE `E-LEARNING`;
SET NAMES utf8mb4;
DROP TABLE IF EXISTS account_change_logs;
DROP TABLE IF EXISTS exam_results;
DROP TABLE IF EXISTS attendance;
DROP TABLE IF EXISTS lessons;
DROP TABLE IF EXISTS monitoring_logs;
DROP TABLE IF EXISTS assignments;
DROP TABLE IF EXISTS sessions;
DROP TABLE IF EXISTS users;

CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) UNIQUE NOT NULL,
    username VARCHAR(50) UNIQUE NOT NULL,
    student_id VARCHAR(20) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role ENUM('student','teacher') NOT NULL DEFAULT 'student',
    is_active TINYINT(1) DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE sessions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    token TEXT NOT NULL,
    ip_address VARCHAR(45),
    login_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    is_active TINYINT(1) DEFAULT 1,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE assignments (
    id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    status VARCHAR(50) DEFAULT 'active',
    exam_type VARCHAR(50),
    is_online_exam TINYINT(1) DEFAULT 1,
    open_date VARCHAR(100),
    due_date VARCHAR(100),
    last_modified_by VARCHAR(100),
    modified_date VARCHAR(100),
    file_name VARCHAR(500),
    questions_count INT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE exam_results (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    assignment_title VARCHAR(200),
    score DECIMAL(5,2) DEFAULT 0,
    feedback TEXT,
    submitted_at DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE attendance (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    fullname VARCHAR(100),
    similarity DECIMAL(5,2) DEFAULT 0,
    is_match TINYINT(1) DEFAULT 0,
    verdict VARCHAR(50),
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE lessons (
    id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    subject VARCHAR(100),
    access_level VARCHAR(50) DEFAULT 'Public',
    upload_date VARCHAR(50),
    uploaded_by VARCHAR(100),
    file_size VARCHAR(20),
    file_path VARCHAR(500),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE monitoring_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    screenshot_path VARCHAR(500),
    is_violation TINYINT(1) DEFAULT 0,
    violation_type VARCHAR(50),
    captured_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE account_change_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    changed_field VARCHAR(50),
    old_value TEXT,
    new_value TEXT,
    changed_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

DELIMITER //
CREATE TRIGGER trg_lock_student_on_update
BEFORE UPDATE ON users
FOR EACH ROW
BEGIN
    IF OLD.role = 'student' AND (
        OLD.full_name != NEW.full_name OR OLD.email != NEW.email OR
        OLD.username != NEW.username OR OLD.student_id != NEW.student_id OR
        OLD.password_hash != NEW.password_hash
    ) THEN
        SET NEW.is_active = 0;
        INSERT INTO account_change_logs (user_id, changed_field, old_value, new_value)
        VALUES (OLD.id,
            CASE WHEN OLD.full_name != NEW.full_name THEN 'full_name'
                 WHEN OLD.email != NEW.email THEN 'email'
                 WHEN OLD.username != NEW.username THEN 'username'
                 WHEN OLD.student_id != NEW.student_id THEN 'student_id'
                 ELSE 'password_hash' END,
            CASE WHEN OLD.full_name != NEW.full_name THEN OLD.full_name
                 WHEN OLD.email != NEW.email THEN OLD.email
                 WHEN OLD.username != NEW.username THEN OLD.username
                 WHEN OLD.student_id != NEW.student_id THEN OLD.student_id
                 ELSE '***' END,
            CASE WHEN OLD.full_name != NEW.full_name THEN NEW.full_name
                 WHEN OLD.email != NEW.email THEN NEW.email
                 WHEN OLD.username != NEW.username THEN NEW.username
                 WHEN OLD.student_id != NEW.student_id THEN NEW.student_id
                 ELSE '***' END);
    END IF;
END //
DELIMITER ;
INSERT INTO users (full_name,email,username,student_id,password_hash,role,is_active,created_at,updated_at) VALUES ('admin','mt1479233@gmail.com','admin123','29210247142','ddfa08f04ffbedd937ce079026ead9826c0f4572feee5e45ff2a66d058c0c9d5','teacher',1,'2026-06-10 12:00:59','2026-06-10 12:00:59');
INSERT INTO users (full_name,email,username,student_id,password_hash,role,is_active,created_at,updated_at) VALUES ('TRAN HONG MINH','tranhongminh@dtu.edu.vn','tranhongminh','29210247142','30fbe3e1ab2d0995251d1b02e1d9c4357158ad8a25ab48e1e3d4a0221263591e','student',1,'2026-06-11 01:11:03','2026-06-11 01:11:03');
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ','127.0.0.1','2026-06-11 12:46:51',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ','192.168.110.53','2026-06-11 16:08:55',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','192.168.110.53','2026-06-11 16:10:44',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ','172.22.83.43','2026-06-12 00:54:57',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','172.22.83.43','2026-06-12 00:56:15',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','172.22.83.43','2026-06-12 00:57:05',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ','172.22.83.43','2026-06-12 01:47:30',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','172.22.83.43','2026-06-12 01:47:57',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ','172.22.83.43','2026-06-12 01:52:39',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','172.22.83.43','2026-06-12 01:53:04',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','127.0.0.1','2026-06-12 03:02:41',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ','127.0.0.1','2026-06-12 03:04:10',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','127.0.0.1','2026-06-12 03:08:31',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ','127.0.0.1','2026-06-12 03:09:27',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','127.0.0.1','2026-06-12 08:19:21',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ','127.0.0.1','2026-06-12 09:03:13',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','127.0.0.1','2026-06-12 09:04:40',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','127.0.0.1','2026-06-12 09:16:36',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (2,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoyLCJ1c2VybmFtZSI6InRyYW5ob25nbWluaCIsInJvbGUiOiJ','127.0.0.1','2026-06-12 09:22:56',1);
INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES (1,'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoxLCJ1c2VybmFtZSI6ImFkbWluMTIzIiwicm9sZSI6InRlYWN','127.0.0.1','2026-06-12 09:24:35',1);
INSERT INTO assignments (title,description,status,exam_type,is_online_exam,open_date,due_date,last_modified_by,modified_date,file_name,questions_count) VALUES ('kiểm tra','bài kiểm tra','Just Uploaded','Trắc nghiệm',1,'Jun 13, 2026, 19:35','Jun 13, 2026, 19:40','CURRENT INSTRUCTOR','Jun 13, 2026, 7:35 PM','C:\\Users\\Admin\\Downloads\\trắc nghiệm.docx',101);
INSERT INTO lessons (title,subject,access_level,upload_date,uploaded_by,file_size,file_path) VALUES ('Process Management Presentation.pdf','CMU-SE 433 SGIS (2026SU)','Public','14/06/2026','Giảng viên','2056 KB','C:\\Users\\Admin\\Downloads\\Process Management Presentation.pdf');
INSERT INTO exam_results (username,assignment_title,score,feedback,submitted_at) VALUES ('phantanphuc','kiểm tra',9,'Hệ thống AI đã phân tích biểu đồ năng lực.
Điểm số: 9/100. 
Nhận xét: Kém, bạn bị hổng kiến thức nghiêm trọng.','2026-06-13 19:38:18');
INSERT INTO attendance (username,fullname,similarity,is_match,verdict,timestamp) VALUES ('phantanphuc','phantanphuc',98.19,1,'Co mat',FROM_UNIXTIME(1781368015));
INSERT INTO attendance (username,fullname,similarity,is_match,verdict,timestamp) VALUES ('phantanphuc','phantanphuc',98.69,1,'Có mặt',FROM_UNIXTIME(1781368877));
INSERT INTO attendance (username,fullname,similarity,is_match,verdict,timestamp) VALUES ('phantanphuc','phantanphuc',93.78,1,'Có mặt',FROM_UNIXTIME(1781415808));
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174401.jpg',1,'TabSwitch','2026-06-11 10:44:01');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174410.jpg',1,'TabSwitch','2026-06-11 10:44:10');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174449.jpg',1,'TabSwitch','2026-06-11 10:44:49');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174455.jpg',1,'TabSwitch','2026-06-11 10:44:55');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174501.jpg',1,'TabSwitch','2026-06-11 10:45:01');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174547.jpg',1,'TabSwitch','2026-06-11 10:45:47');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174554.jpg',1,'TabSwitch','2026-06-11 10:45:54');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174606.jpg',1,'TabSwitch','2026-06-11 10:46:06');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174616.jpg',1,'TabSwitch','2026-06-11 10:46:16');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174627.jpg',1,'TabSwitch','2026-06-11 10:46:27');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174630.jpg',1,'TabSwitch','2026-06-11 10:46:30');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611174645.jpg',1,'TabSwitch','2026-06-11 10:46:45');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611182834.jpg',1,'TabSwitch','2026-06-11 11:28:34');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611182848.jpg',1,'TabSwitch','2026-06-11 11:28:48');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611182851.jpg',1,'TabSwitch','2026-06-11 11:28:51');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611182916.jpg',1,'TabSwitch','2026-06-11 11:29:16');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611182919.jpg',1,'TabSwitch','2026-06-11 11:29:19');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611182924.jpg',1,'TabSwitch','2026-06-11 11:29:24');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611182928.jpg',1,'TabSwitch','2026-06-11 11:29:28');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611183640.jpg',1,'TabSwitch','2026-06-11 11:36:40');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611183715.jpg',1,'TabSwitch','2026-06-11 11:37:15');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611193123.jpg',1,'TabSwitch','2026-06-11 12:31:23');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611193228.jpg',1,'TabSwitch','2026-06-11 12:32:28');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611193234.jpg',1,'TabSwitch','2026-06-11 12:32:34');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611193250.jpg',1,'TabSwitch','2026-06-11 12:32:50');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611193359.jpg',1,'TabSwitch','2026-06-11 12:33:59');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611193502.jpg',1,'TabSwitch','2026-06-11 12:35:02');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611193517.jpg',1,'TabSwitch','2026-06-11 12:35:17');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611194655.jpg',1,'TabSwitch','2026-06-11 12:46:55');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611194719.jpg',1,'TabSwitch','2026-06-11 12:47:19');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611230930.jpg',1,'TabSwitch','2026-06-11 16:09:30');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611230931.jpg',1,'TabSwitch','2026-06-11 16:09:31');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260611231147.jpg',1,'TabSwitch','2026-06-11 16:11:47');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612075506.jpg',1,'TabSwitch','2026-06-12 00:55:06');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612075526.jpg',1,'TabSwitch','2026-06-12 00:55:26');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612075526.jpg',1,'TabSwitch','2026-06-12 00:55:26');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612075532.jpg',1,'TabSwitch','2026-06-12 00:55:32');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612075532.jpg',1,'TabSwitch','2026-06-12 00:55:32');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612075544.jpg',1,'TabSwitch','2026-06-12 00:55:44');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612075554.jpg',1,'TabSwitch','2026-06-12 00:55:54');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612075758.jpg',1,'TabSwitch','2026-06-12 00:57:58');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612084736.jpg',1,'TabSwitch','2026-06-12 01:47:36');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612085001.jpg',1,'TabSwitch','2026-06-12 01:50:01');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612085245.jpg',1,'TabSwitch','2026-06-12 01:52:45');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612100419.jpg',1,'TabSwitch','2026-06-12 03:04:19');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612100423.jpg',1,'TabSwitch','2026-06-12 03:04:23');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612100432.jpg',1,'TabSwitch','2026-06-12 03:04:32');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612100437.jpg',1,'TabSwitch','2026-06-12 03:04:37');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612100502.jpg',1,'TabSwitch','2026-06-12 03:05:02');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612100934.jpg',1,'TabSwitch','2026-06-12 03:09:34');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612101001.jpg',1,'TabSwitch','2026-06-12 03:10:01');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612101004.jpg',1,'TabSwitch','2026-06-12 03:10:04');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612101045.jpg',1,'TabSwitch','2026-06-12 03:10:45');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612160520.jpg',1,'TabSwitch','2026-06-12 09:05:20');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612160528.jpg',1,'TabSwitch','2026-06-12 09:05:28');
INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES (2,'uploads\\tranhongminh_20260612162303.jpg',1,'TabSwitch','2026-06-12 09:23:03');
