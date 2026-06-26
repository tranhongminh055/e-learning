import cv2
import time
import requests
import numpy as np
from flask import Flask, Response, jsonify, request
from deepface import DeepFace

app = Flask(__name__)

# Global variables
FIREBASE_DB = ""
FIREBASE_TOKEN = ""
USERNAME = ""
FULLNAME = ""

cap = cv2.VideoCapture(0)
has_ref = False
ref_data = None
current_status = "Đang khởi động Camera..."
action_triggered = None # "REGISTER" or "ATTEND"

def load_reference():
    global has_ref, ref_data
    try:
        url = f"{FIREBASE_DB}/face_references/{USERNAME}.json?auth={FIREBASE_TOKEN}"
        res = requests.get(url)
        if res.status_code == 200 and res.json():
            ref_data = res.json()
            has_ref = True
    except:
        pass

def generate_frames():
    global current_status, action_triggered, has_ref, ref_data
    
    while True:
        success, frame = cap.read()
        if not success:
            time.sleep(0.1)
            continue
            
        frame = cv2.flip(frame, 1)
        display_frame = frame.copy()
        
        # Process actions
        if action_triggered == "REGISTER":
            current_status = "Đang phân tích và đăng ký..."
            action_triggered = None
            try:
                # Use enforce_detection=False so it tries its best even if dark
                result = DeepFace.represent(frame, model_name="Facenet", enforce_detection=False)
                if result:
                    embedding = result[0]["embedding"]
                    _, buffer = cv2.imencode('.jpg', frame)
                    import base64
                    photo_b64 = base64.b64encode(buffer).decode('utf-8')
                    
                    payload = {
                        "descriptor": embedding,
                        "photo": photo_b64,
                        "fullname": FULLNAME,
                        "registered_at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
                    }
                    requests.put(f"{FIREBASE_DB}/face_references/{USERNAME}.json?auth={FIREBASE_TOKEN}", json=payload)
                    ref_data = payload
                    has_ref = True
                    current_status = "Đăng ký thành công!"
            except Exception as e:
                current_status = f"Lỗi ĐK: Không nhận diện được khuôn mặt. Chi tiết: {str(e)[:50]}"
        elif action_triggered == "ATTEND":
            if not has_ref:
                current_status = "Lỗi: Bạn chưa đăng ký khuôn mặt!"
            else:
                current_status = "Đang xác thực..."
                action_triggered = None
                try:
                    result = DeepFace.represent(frame, model_name="Facenet", enforce_detection=False)
                    if result:
                        current_embedding = result[0]["embedding"]
                        ref_embedding = ref_data["descriptor"]
                        
                        dist = np.linalg.norm(np.array(current_embedding) - np.array(ref_embedding))
                        threshold = 12.0 # More lenient
                        is_match = dist < threshold
                        
                        _, buffer = cv2.imencode('.jpg', frame)
                        import base64
                        photo_b64 = base64.b64encode(buffer).decode('utf-8')
                        
                        record = {
                            "username": USERNAME,
                            "fullname": FULLNAME,
                            "similarity": round(max(0, (1 - dist/20)*100), 2),
                            "is_match": bool(is_match),
                            "photo": photo_b64,
                            "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                            "verdict": "Có mặt" if is_match else "Vắng mặt (Không khớp)"
                        }
                        requests.put(f"{FIREBASE_DB}/attendance/{USERNAME}/{int(time.time()*1000)}.json?auth={FIREBASE_TOKEN}", json=record)
                        
                        if is_match:
                            current_status = f"✅ XÁC THỰC THÀNH CÔNG ({record['similarity']}%)"
                        else:
                            current_status = "❌ XÁC THỰC THẤT BẠI. Không khớp khuôn mặt!"
                except Exception as e:
                    current_status = f"Lỗi ĐD: Không thể xác thực. Chi tiết: {str(e)[:50]}"
        
        ret, buffer = cv2.imencode('.jpg', display_frame)
        frame_bytes = buffer.tobytes()
        yield (b'--frame\r\n'
               b'Content-Type: image/jpeg\r\n\r\n' + frame_bytes + b'\r\n')

@app.route('/video_feed')
def video_feed():
    return Response(generate_frames(), mimetype='multipart/x-mixed-replace; boundary=frame')

@app.route('/status')
def get_status():
    return jsonify({"status": current_status, "has_ref": has_ref})

@app.route('/action', methods=['POST'])
def perform_action():
    global action_triggered
    data = request.json
    action_triggered = data.get("action")
    return jsonify({"success": True})

@app.route('/ui')
def ui():
    html = """
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="utf-8">
        <style>
            body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; text-align: center; background-color: #f5f5f5; margin: 0; padding: 20px; }
            img { border-radius: 10px; box-shadow: 0 4px 8px rgba(0,0,0,0.2); max-width: 100%; }
            .btn { background: #1a73e8; color: white; border: none; padding: 12px 24px; font-size: 16px; border-radius: 5px; cursor: pointer; margin: 10px; font-weight: bold; }
            .btn:hover { background: #1557b0; }
            .btn-register { background: #9c27b0; }
            .btn-register:hover { background: #7b1fa2; }
            #statusBox { background: white; padding: 15px; border-radius: 8px; margin-bottom: 20px; font-size: 18px; font-weight: bold; color: #333; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        </style>
    </head>
    <body>
        <div id="statusBox">Đang kết nối Camera...</div>
        <div>
            <img src="/video_feed" width="640" height="480">
        </div>
        <div style="margin-top: 20px;">
            <button class="btn btn-register" onclick="sendAction('REGISTER')">📸 Đăng ký khuôn mặt</button>
            <button class="btn" onclick="sendAction('ATTEND')">✅ Điểm danh</button>
        </div>
        <script>
            function sendAction(action) {
                document.getElementById('statusBox').innerText = "Đang xử lý...";
                fetch('/action', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ action: action })
                });
            }
            
            setInterval(() => {
                fetch('/status')
                    .then(r => r.json())
                    .then(data => {
                        document.getElementById('statusBox').innerText = data.status;
                    });
            }, 1000);
        </script>
    </body>
    </html>
    """
    return html

if __name__ == '__main__':
    import sys
    import threading
    if len(sys.argv) >= 5:
        FIREBASE_DB = sys.argv[1]
        FIREBASE_TOKEN = sys.argv[2]
        USERNAME = sys.argv[3]
        FULLNAME = sys.argv[4]
        load_reference()
        current_status = "Camera đã sẵn sàng! AI Python (DeepFace) đang chạy."
    
    # Run flask
    app.run(host='127.0.0.1', port=5050, threaded=True)
