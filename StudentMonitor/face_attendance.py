import cv2
import os
import sys
import json
import time
import requests
import numpy as np
from deepface import DeepFace

FIREBASE_DB = "" # We'll pass via args
FIREBASE_TOKEN = ""
USERNAME = ""
FULLNAME = ""

def load_reference_face(db, token, username):
    try:
        url = f"{db}/face_references/{username}.json?auth={token}"
        res = requests.get(url)
        if res.status_code == 200 and res.json():
            return res.json()
    except Exception as e:
        print("Error loading ref:", e)
    return None

def main():
    global FIREBASE_DB, FIREBASE_TOKEN, USERNAME, FULLNAME
    if len(sys.argv) < 5:
        print("Missing arguments")
        return
    
    FIREBASE_DB = sys.argv[1]
    FIREBASE_TOKEN = sys.argv[2]
    USERNAME = sys.argv[3]
    FULLNAME = sys.argv[4]

    ref_data = load_reference_face(FIREBASE_DB, FIREBASE_TOKEN, USERNAME)
    has_ref = True if ref_data else False

    cap = cv2.VideoCapture(0)
    
    state = "READY"
    status_msg = "Nhan [R] de Dang ky | Nhan [A] de Diem danh" if has_ref else "Chua co du lieu! Nhan [R] de Dang ky"
    
    while True:
        ret, frame = cap.read()
        if not ret:
            break

        frame = cv2.flip(frame, 1)
        display_frame = frame.copy()

        # Display status
        cv2.putText(display_frame, status_msg, (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
        cv2.putText(display_frame, "Nhan [Q] de Thoat", (20, 80), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)

        cv2.imshow("Diem Danh Khoun Mat - AI Python", display_frame)

        key = cv2.waitKey(1) & 0xFF
        
        if key == ord('q'):
            break
        elif key == ord('r'):
            status_msg = "Dang phan tich khuon mat..."
            cv2.putText(display_frame, status_msg, (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)
            cv2.imshow("Diem Danh Khoun Mat - AI Python", display_frame)
            cv2.waitKey(1)
            
            try:
                # Trích xuất embeddings
                result = DeepFace.represent(frame, model_name="Facenet", enforce_detection=True)
                if result:
                    embedding = result[0]["embedding"]
                    
                    # Encode image to base64
                    _, buffer = cv2.imencode('.jpg', frame)
                    import base64
                    photo_b64 = base64.b64encode(buffer).decode('utf-8')
                    
                    # Save to firebase
                    url = f"{FIREBASE_DB}/face_references/{USERNAME}.json?auth={FIREBASE_TOKEN}"
                    payload = {
                        "descriptor": embedding,
                        "photo": photo_b64,
                        "fullname": FULLNAME,
                        "registered_at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
                    }
                    requests.put(url, json=payload)
                    status_msg = "Dang ky thanh cong! Nhan [A] de Diem danh"
                    has_ref = True
                    ref_data = payload
            except Exception as e:
                status_msg = "Loi: Khong tim thay khuon mat ro rang!"
                print(e)
                
        elif key == ord('a'):
            if not has_ref:
                status_msg = "Loi: Ban chua dang ky khuon mat!"
                continue
                
            status_msg = "Dang xac thuc khuon mat..."
            cv2.putText(display_frame, status_msg, (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)
            cv2.imshow("Diem Danh Khoun Mat - AI Python", display_frame)
            cv2.waitKey(1)
            
            try:
                result = DeepFace.represent(frame, model_name="Facenet", enforce_detection=True)
                if result:
                    current_embedding = result[0]["embedding"]
                    ref_embedding = ref_data["descriptor"]
                    
                    # Compute distance
                    dist = np.linalg.norm(np.array(current_embedding) - np.array(ref_embedding))
                    threshold = 10.0 # Tùy model, Facenet threshold khoang 10
                    is_match = dist < threshold
                    
                    _, buffer = cv2.imencode('.jpg', frame)
                    import base64
                    photo_b64 = base64.b64encode(buffer).decode('utf-8')
                    
                    record = {
                        "username": USERNAME,
                        "fullname": FULLNAME,
                        "similarity": round((1 - dist/20)*100, 2),
                        "is_match": bool(is_match),
                        "photo": photo_b64,
                        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                        "verdict": "Co mat" if is_match else "Vang mat (Khong khop)"
                    }
                    
                    url = f"{FIREBASE_DB}/attendance/{USERNAME}/{int(time.time()*1000)}.json?auth={FIREBASE_TOKEN}"
                    requests.put(url, json=record)
                    
                    if is_match:
                        status_msg = f"Xac thuc THANH CONG! ({record['similarity']}%)"
                    else:
                        status_msg = "Xac thuc THAT BAI! Khong giong khuon mat dang ky."
            except Exception as e:
                status_msg = "Loi: Khong the xac thuc (Mat mo hoac qua toi)"
                print(e)

    cap.release()
    cv2.destroyAllWindows()

if __name__ == "__main__":
    main()
