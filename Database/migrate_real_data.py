import sqlite3, json, sys, subprocess, urllib.request
sys.stdout.reconfigure(encoding='utf-8')

MYSQL_EXE = r"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
MYSQL_PASS = "Hieuthi22032005"
SQLCMD = "sqlcmd"
SA_PASS = "123456"
SQLITE_PATH = "d:/MyDTU/Backend/elearning.db"
DB_URL = "https://vibio-8391c-default-rtdb.firebaseio.com"

def esc(s):
    if s is None: return "NULL"
    return "'" + str(s).replace("\\","\\\\").replace("'","\\'") + "'"

def nesc(s):
    if s is None: return "NULL"
    return "N'" + str(s).replace("'","''") + "'"

# ========== 1. Read all real data ==========
conn = sqlite3.connect(SQLITE_PATH)
conn.row_factory = sqlite3.Row
c = conn.cursor()

users = [dict(r) for r in c.execute("SELECT * FROM users").fetchall()]
sessions = [dict(r) for r in c.execute("SELECT * FROM sessions").fetchall()]
violations = [dict(r) for r in c.execute("SELECT * FROM monitoring_logs").fetchall()]
conn.close()

# Read JSON
with open('d:/MyDTU/StudentMonitor/assignments.json','r',encoding='utf-8') as f:
    assignments = json.load(f)
with open('d:/MyDTU/StudentMonitor/lessons.json','r',encoding='utf-8') as f:
    lessons = json.load(f)

# Read Firebase
firebase_data = {}
for path in ['attendance','exam_results']:
    try:
        req = urllib.request.Request(f"{DB_URL}/{path}.json")
        with urllib.request.urlopen(req, timeout=10) as resp:
            firebase_data[path] = json.loads(resp.read().decode())
    except:
        firebase_data[path] = None

print(f"Users: {len(users)}, Sessions: {len(sessions)}, Violations: {len(violations)}")
print(f"Assignments: {len(assignments)}, Lessons: {len(lessons)}")
print(f"Firebase attendance: {firebase_data.get('attendance')}")
print(f"Firebase exam_results: {firebase_data.get('exam_results')}")

# ========== 2. Generate MySQL SQL ==========
mysql_sql = "USE `E-LEARNING`;\n"
mysql_sql += "SET NAMES utf8mb4;\n"
# Drop old data
for t in ['account_change_logs','exam_results','attendance','lessons','monitoring_logs','assignments','sessions','users']:
    mysql_sql += f"DROP TABLE IF EXISTS {t};\n"

# Create tables
mysql_sql += """
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
"""

# Trigger
mysql_sql += """
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
"""

# Insert users
for u in users:
    mysql_sql += f"INSERT INTO users (full_name,email,username,student_id,password_hash,role,is_active,created_at,updated_at) VALUES ({esc(u['full_name'])},{esc(u['email'])},{esc(u['username'])},{esc(u['student_id'])},{esc(u['password_hash'])},{esc(u['role'])},{u['is_active']},{esc(u['created_at'])},{esc(u['updated_at'])});\n"

# Insert sessions (latest 20)
for s in sessions[-20:]:
    tok = s['token'][:100]
    mysql_sql += f"INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES ({s['user_id']},{esc(tok)},{esc(s['ip_address'])},{esc(s['login_at'])},{s['is_active']});\n"

# Insert assignments
for a in assignments:
    qc = len(a.get('Questions',[]))
    mysql_sql += f"INSERT INTO assignments (title,description,status,exam_type,is_online_exam,open_date,due_date,last_modified_by,modified_date,file_name,questions_count) VALUES ({esc(a.get('Title'))},{esc(a.get('Description'))},{esc(a.get('Status'))},{esc(a.get('ExamType'))},{1 if a.get('IsOnlineExam') else 0},{esc(a.get('OpenDate'))},{esc(a.get('DueDate'))},{esc(a.get('LastModifiedBy'))},{esc(a.get('ModifiedDate'))},{esc(a.get('FileName'))},{qc});\n"

# Insert lessons
for l in lessons:
    mysql_sql += f"INSERT INTO lessons (title,subject,access_level,upload_date,uploaded_by,file_size,file_path) VALUES ({esc(l.get('Title'))},{esc(l.get('Subject'))},{esc(l.get('Access'))},{esc(l.get('UploadDate'))},{esc(l.get('UploadedBy'))},{esc(l.get('Size'))},{esc(l.get('FilePath'))});\n"

# Insert exam_results from Firebase
er = firebase_data.get('exam_results')
if er and isinstance(er, dict):
    for uname, exams in er.items():
        if isinstance(exams, dict):
            for title, data in exams.items():
                mysql_sql += f"INSERT INTO exam_results (username,assignment_title,score,feedback,submitted_at) VALUES ({esc(uname)},{esc(data.get('AssignmentTitle'))},{data.get('Score',0)},{esc(data.get('Feedback'))},{esc(data.get('SubmittedAt'))});\n"

# Insert attendance from Firebase
att = firebase_data.get('attendance')
if att and isinstance(att, dict):
    for uname, records in att.items():
        if isinstance(records, dict):
            for rid, data in records.items():
                sim = data.get('similarity',0)
                ism = 1 if data.get('is_match') else 0
                mysql_sql += f"INSERT INTO attendance (username,fullname,similarity,is_match,verdict,timestamp) VALUES ({esc(uname)},{esc(data.get('fullname'))},{sim},{ism},{esc(data.get('verdict'))},FROM_UNIXTIME({int(rid)//1000}));\n"

# Insert monitoring_logs (violations)
for v in violations:
    mysql_sql += f"INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES ({v['user_id']},{esc(v.get('screenshot_path'))},{v.get('is_violation',0)},{esc(v.get('violation_type'))},{esc(v.get('captured_at'))});\n"

# Write MySQL file
with open('d:/MyDTU/Database/mysql_elearning.sql','w',encoding='utf-8') as f:
    f.write(mysql_sql)
print("MySQL script written!")

# ========== 3. Generate SQL Server SQL ==========
ss = "USE [E-LEARNING];\nGO\n"
for t in ['account_change_logs','exam_results','attendance','lessons','monitoring_logs','assignments','sessions','users']:
    ss += f"IF OBJECT_ID('{t}','U') IS NOT NULL DROP TABLE {t};\n"
ss += "GO\n"

ss += """
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
"""

# Trigger
ss += """
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
"""

# Insert users
for u in users:
    ss += f"INSERT INTO users (full_name,email,username,student_id,password_hash,role,is_active,created_at,updated_at) VALUES ({nesc(u['full_name'])},{nesc(u['email'])},{nesc(u['username'])},{nesc(u['student_id'])},{nesc(u['password_hash'])},{nesc(u['role'])},{u['is_active']},{nesc(u['created_at'])},{nesc(u['updated_at'])});\n"
ss += "GO\n"

# Insert sessions
for s in sessions[-20:]:
    tok = s['token'][:100]
    ss += f"INSERT INTO sessions (user_id,token,ip_address,login_at,is_active) VALUES ({s['user_id']},{nesc(tok)},{nesc(s['ip_address'])},{nesc(s['login_at'])},{s['is_active']});\n"
ss += "GO\n"

# Insert assignments
for a in assignments:
    qc = len(a.get('Questions',[]))
    ss += f"INSERT INTO assignments (title,description,status,exam_type,is_online_exam,open_date,due_date,last_modified_by,modified_date,file_name,questions_count) VALUES ({nesc(a.get('Title'))},{nesc(a.get('Description'))},{nesc(a.get('Status'))},{nesc(a.get('ExamType'))},{1 if a.get('IsOnlineExam') else 0},{nesc(a.get('OpenDate'))},{nesc(a.get('DueDate'))},{nesc(a.get('LastModifiedBy'))},{nesc(a.get('ModifiedDate'))},{nesc(a.get('FileName'))},{qc});\n"
ss += "GO\n"

# Insert lessons
for l in lessons:
    ss += f"INSERT INTO lessons (title,subject,access_level,upload_date,uploaded_by,file_size,file_path) VALUES ({nesc(l.get('Title'))},{nesc(l.get('Subject'))},{nesc(l.get('Access'))},{nesc(l.get('UploadDate'))},{nesc(l.get('UploadedBy'))},{nesc(l.get('Size'))},{nesc(l.get('FilePath'))});\n"
ss += "GO\n"

# Insert exam_results
if er and isinstance(er, dict):
    for uname, exams in er.items():
        if isinstance(exams, dict):
            for title, data in exams.items():
                fb = str(data.get('Feedback','')).replace("'","''")
                ss += f"INSERT INTO exam_results (username,assignment_title,score,feedback,submitted_at) VALUES ({nesc(uname)},{nesc(data.get('AssignmentTitle'))},{data.get('Score',0)},N'{fb}',{nesc(data.get('SubmittedAt'))});\n"
    ss += "GO\n"

# Insert attendance
if att and isinstance(att, dict):
    for uname, records in att.items():
        if isinstance(records, dict):
            for rid, data in records.items():
                sim = data.get('similarity',0)
                ism = 1 if data.get('is_match') else 0
                ts = int(rid)//1000
                from datetime import datetime
                dt = datetime.utcfromtimestamp(ts).strftime('%Y-%m-%d %H:%M:%S')
                ss += f"INSERT INTO attendance (username,fullname,similarity,is_match,verdict,[timestamp]) VALUES ({nesc(uname)},{nesc(data.get('fullname'))},{sim},{ism},{nesc(data.get('verdict'))},{nesc(dt)});\n"
    ss += "GO\n"

# Insert monitoring_logs
for v in violations:
    ss += f"INSERT INTO monitoring_logs (user_id,screenshot_path,is_violation,violation_type,captured_at) VALUES ({v['user_id']},{nesc(v.get('screenshot_path'))},{v.get('is_violation',0)},{nesc(v.get('violation_type'))},{nesc(v.get('captured_at'))});\n"
ss += "GO\n"

with open('d:/MyDTU/Database/sqlserver_elearning.sql','w',encoding='utf-8') as f:
    f.write(ss)
print("SQL Server script written!")
