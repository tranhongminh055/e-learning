"""
E-Learning Student Monitor - Backend API Server
Flask-based REST API for authentication and monitoring
"""

from flask import Flask, request, jsonify, render_template, Response
from flask_cors import CORS
import sqlite3
import hashlib
import jwt
import datetime
import os

app = Flask(__name__)
CORS(app)

# Storage for active webRTC peers: {username: {camera: peer_id, screen: peer_id}}
active_peers = {}
# Storage for latest screen captures: {username: byte_array}
active_screens = {}

# Secret key for JWT
SECRET_KEY = os.environ.get('SECRET_KEY', 'e-learning-monitor-secret-key-2026')

# Database path
DB_PATH = os.path.join(os.path.dirname(__file__), 'elearning.db')


def get_db():
    """Get database connection"""
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    return conn


def init_db():
    """Initialize database tables"""
    conn = get_db()
    cursor = conn.cursor()

    cursor.execute('''
        CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            full_name TEXT NOT NULL,
            email TEXT UNIQUE NOT NULL,
            username TEXT UNIQUE NOT NULL,
            student_id TEXT NOT NULL,
            password_hash TEXT NOT NULL,
            role TEXT NOT NULL DEFAULT 'student',
            is_active INTEGER DEFAULT 1,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    ''')

    cursor.execute('''
        CREATE TABLE IF NOT EXISTS sessions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            token TEXT NOT NULL,
            ip_address TEXT,
            login_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            is_active INTEGER DEFAULT 1,
            FOREIGN KEY (user_id) REFERENCES users(id)
        )
    ''')

    cursor.execute('''
        CREATE TABLE IF NOT EXISTS monitoring_logs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            exam_id INTEGER,
            screenshot_path TEXT,
            active_window TEXT,
            is_violation INTEGER DEFAULT 0,
            violation_type TEXT,
            captured_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (user_id) REFERENCES users(id)
        )
    ''')

    conn.commit()
    conn.close()


def hash_password(password):
    """Hash password with SHA-256"""
    return hashlib.sha256(password.encode()).hexdigest()


def generate_token(user_id, username, role):
    """Generate JWT token"""
    payload = {
        'user_id': user_id,
        'username': username,
        'role': role,
        'exp': datetime.datetime.utcnow() + datetime.timedelta(hours=24)
    }
    return jwt.encode(payload, SECRET_KEY, algorithm='HS256')


# ============================================
# AUTH ROUTES
# ============================================

@app.route('/api/auth/register', methods=['POST'])
def register():
    """Register a new user"""
    data = request.get_json()

    required_fields = ['full_name', 'email', 'username', 'student_id', 'password', 'role']
    for field in required_fields:
        if field not in data or not data[field]:
            return jsonify({'message': f'Trường {field} là bắt buộc.'}), 400

    if len(data['username']) < 4:
        return jsonify({'message': 'Tên đăng nhập phải có ít nhất 4 ký tự.'}), 400

    if len(data['password']) < 6:
        return jsonify({'message': 'Mật khẩu phải có ít nhất 6 ký tự.'}), 400

    if data['role'] not in ['student', 'teacher']:
        return jsonify({'message': 'Vai trò không hợp lệ.'}), 400

    conn = get_db()
    cursor = conn.cursor()

    try:
        # Check if username exists
        cursor.execute('SELECT id FROM users WHERE username = ?', (data['username'],))
        if cursor.fetchone():
            return jsonify({'message': 'Tên đăng nhập đã tồn tại.'}), 409

        # Check if email exists
        cursor.execute('SELECT id FROM users WHERE email = ?', (data['email'],))
        if cursor.fetchone():
            return jsonify({'message': 'Email đã được sử dụng.'}), 409

        # Insert new user
        password_hash = hash_password(data['password'])
        cursor.execute('''
            INSERT INTO users (full_name, email, username, student_id, password_hash, role)
            VALUES (?, ?, ?, ?, ?, ?)
        ''', (
            data['full_name'],
            data['email'],
            data['username'],
            data['student_id'],
            password_hash,
            data['role']
        ))

        conn.commit()
        return jsonify({'message': 'Đăng ký thành công!'}), 201

    except Exception as e:
        return jsonify({'message': f'Lỗi hệ thống: {str(e)}'}), 500
    finally:
        conn.close()


@app.route('/api/auth/login', methods=['POST'])
def login():
    """Login user"""
    data = request.get_json()

    if not data.get('username') or not data.get('password'):
        return jsonify({'message': 'Vui lòng nhập tên đăng nhập và mật khẩu.'}), 400

    conn = get_db()
    cursor = conn.cursor()

    try:
        password_hash = hash_password(data['password'])
        cursor.execute('''
            SELECT id, full_name, username, role, is_active
            FROM users
            WHERE username = ? AND password_hash = ?
        ''', (data['username'], password_hash))

        user = cursor.fetchone()

        if not user:
            return jsonify({'message': 'Tên đăng nhập hoặc mật khẩu không đúng.'}), 401

        if not user['is_active']:
            return jsonify({'message': 'Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.'}), 403

        # Generate token
        token = generate_token(user['id'], user['username'], user['role'])

        # Save session
        cursor.execute('''
            INSERT INTO sessions (user_id, token, ip_address)
            VALUES (?, ?, ?)
        ''', (user['id'], token, request.remote_addr))
        conn.commit()

        return jsonify({
            'message': 'Đăng nhập thành công!',
            'token': token,
            'role': user['role'],
            'full_name': user['full_name'],
            'user_id': user['id']
        }), 200

    except Exception as e:
        return jsonify({'message': f'Lỗi hệ thống: {str(e)}'}), 500
    finally:
        conn.close()


@app.route('/api/auth/verify', methods=['GET'])
def verify_token():
    """Verify JWT token"""
    auth_header = request.headers.get('Authorization')
    if not auth_header or not auth_header.startswith('Bearer '):
        return jsonify({'message': 'Token không hợp lệ.'}), 401

    token = auth_header.split(' ')[1]
    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=['HS256'])
        return jsonify({
            'valid': True,
            'user_id': payload['user_id'],
            'username': payload['username'],
            'role': payload['role']
        }), 200
    except jwt.ExpiredSignatureError:
        return jsonify({'message': 'Token đã hết hạn.'}), 401
    except jwt.InvalidTokenError:
        return jsonify({'message': 'Token không hợp lệ.'}), 401


# ============================================
# USER ROUTES
# ============================================

@app.route('/api/users', methods=['GET'])
def get_users():
    """Get all users (teacher/admin only)"""
    conn = get_db()
    cursor = conn.cursor()

    try:
        cursor.execute('''
            SELECT id, full_name, email, username, student_id, role, is_active, created_at
            FROM users
            ORDER BY created_at DESC
        ''')
        users = [dict(row) for row in cursor.fetchall()]
        return jsonify({'users': users}), 200
    except Exception as e:
        return jsonify({'message': f'Lỗi: {str(e)}'}), 500
    finally:
        conn.close()


# ============================================
# MONITORING ROUTES
# ============================================

@app.route('/student_camera')
def student_camera():
    return render_template('student_camera.html')

@app.route('/instructor_view')
def instructor_view():
    return render_template('instructor_view.html')

@app.route('/instructor_monitor')
def instructor_monitor():
    return render_template('instructor_monitor.html')

@app.route('/instructor_screen_view')
def instructor_screen_view():
    return render_template('instructor_screen_view.html')

@app.route('/api/monitor/register_peer', methods=['POST'])
def register_peer():
    data = request.get_json()
    username = data.get('username')
    peer_id = data.get('peer_id')
    peer_type = data.get('type', 'camera')  # 'camera' or 'screen'
    if username and peer_id:
        if username not in active_peers:
            active_peers[username] = {}
        active_peers[username][peer_type] = peer_id
    return jsonify({'status': 'ok'})

@app.route('/api/monitor/active_students', methods=['GET'])
def get_active_students():
    # active_peers is a dictionary: {username: peer_id}
    # Let's get their full names from DB if possible
    if not active_peers:
        return jsonify([])
        
    conn = get_db()
    cursor = conn.cursor()
    try:
        placeholders = ','.join(['?'] * len(active_peers))
        query = f"SELECT username, full_name FROM users WHERE username IN ({placeholders})"
        cursor.execute(query, list(active_peers.keys()))
        users = cursor.fetchall()
        
        result = []
        for u in users:
            peers = active_peers.get(u['username'], {})
            result.append({
                'username': u['username'],
                'full_name': u['full_name'],
                'camera_peer_id': peers.get('camera'),
                'is_violating': peers.get('is_violating', False)
            })
        return jsonify(result)
    except Exception as e:
        print("Error getting active students:", e)
        return jsonify([])
    finally:
        conn.close()

@app.route('/api/monitor/screen', methods=['POST'])
def upload_screen():
    username = request.form.get('username', 'unknown')
    file = request.files.get('screen')
    if username and file:
        active_screens[username] = file.read()
    return jsonify({'status': 'ok'})

def gen_frames(username):
    while True:
        frame = active_screens.get(username)
        if frame:
            yield (b'--frame\r\n'
                   b'Content-Type: image/jpeg\r\n\r\n' + frame + b'\r\n')
        else:
            # Yield a blank frame or sleep
            pass
        import time
        time.sleep(0.5)

@app.route('/api/monitor/stream_screen')
def stream_screen():
    username = request.args.get('username')
    return Response(gen_frames(username), mimetype='multipart/x-mixed-replace; boundary=frame')

@app.route('/api/monitor/get_peer', methods=['GET'])
def get_peer():
    username = request.args.get('username')
    peers = active_peers.get(username, {})
    return jsonify({
        'camera_peer_id': peers.get('camera'),
        'screen_peer_id': peers.get('screen')
    })


@app.route('/api/monitor/violation', methods=['POST'])
def report_violation():
    username = request.form.get('username', 'unknown')
    violation_type = request.form.get('violation_type', 'TabSwitch')
    file = request.files.get('screenshot')
    
    screenshot_path = ""
    if file:
        if not os.path.exists('uploads'):
            os.makedirs('uploads')
        filename = f"{username}_{datetime.datetime.now().strftime('%Y%m%d%H%M%S')}.jpg"
        screenshot_path = os.path.join('uploads', filename)
        file.save(screenshot_path)
    
    conn = get_db()
    cursor = conn.cursor()
    try:
        cursor.execute("SELECT id FROM users WHERE username = ?", (username,))
        user = cursor.fetchone()
        user_id = user['id'] if user else 0
        
        cursor.execute('''
            INSERT INTO monitoring_logs (user_id, violation_type, screenshot_path, is_violation)
            VALUES (?, ?, ?, 1)
        ''', (user_id, violation_type, screenshot_path))
        conn.commit()
    except Exception as e:
        pass
    finally:
        conn.close()
        
    if username in active_peers:
        active_peers[username]['is_violating'] = True
        
    return jsonify({'status': 'ok', 'message': 'Violation recorded'})

@app.route('/api/monitor/clear_violation', methods=['POST'])
def clear_violation():
    data = request.get_json() or {}
    username = data.get('username')
    if username and username in active_peers:
        active_peers[username]['is_violating'] = False
    return jsonify({'status': 'ok'})

@app.route('/api/monitor/violations', methods=['GET'])
def get_violations():
    conn = get_db()
    cursor = conn.cursor()
    try:
        cursor.execute('''
            SELECT m.id, u.full_name, u.username, m.violation_type, m.screenshot_path, m.captured_at
            FROM monitoring_logs m
            LEFT JOIN users u ON m.user_id = u.id
            ORDER BY m.captured_at DESC
        ''')
        violations = [dict(row) for row in cursor.fetchall()]
        return jsonify(violations)
    except Exception as e:
        return jsonify([])
    finally:
        conn.close()


# ============================================
# HEALTH CHECK
# ============================================

@app.route('/api/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        'status': 'ok',
        'service': 'E-Learning Student Monitor API',
        'version': '1.0.0'
    }), 200


# ============================================
# MAIN
# ============================================

# Initialize database on import (needed for Gunicorn)
init_db()

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5000))
    print("=" * 50)
    print("  E-Learning Student Monitor API")
    print(f"  Starting server on http://0.0.0.0:{port}")
    print("=" * 50)
    app.run(host='0.0.0.0', port=port, debug=True)
