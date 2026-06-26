import sqlite3, json, sys
sys.stdout.reconfigure(encoding='utf-8')

conn = sqlite3.connect('d:/MyDTU/Backend/elearning.db')
conn.row_factory = sqlite3.Row
c = conn.cursor()

# Get all users
c.execute("SELECT * FROM users")
users = [dict(r) for r in c.fetchall()]
print("=== ALL USERS ===")
for u in users:
    print(json.dumps(u, ensure_ascii=False))

# Get session count per user
c.execute("SELECT user_id, COUNT(*) as cnt FROM sessions GROUP BY user_id")
for r in c.fetchall():
    print(f"User {r[0]}: {r[1]} sessions")

# Get latest sessions
c.execute("SELECT * FROM sessions ORDER BY login_at DESC LIMIT 10")
sessions = [dict(r) for r in c.fetchall()]
print("\n=== LATEST 10 SESSIONS ===")
for s in sessions:
    s['token'] = s['token'][:50] + '...'  # truncate token
    print(json.dumps(s, ensure_ascii=False))

conn.close()
