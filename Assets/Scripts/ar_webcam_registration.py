import cv2
import mediapipe as mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
import urllib.request
import os
import json
import socket

# Unduh otomatis file model hand_landmarker.task jika belum ada
model_path = "hand_landmarker.task"
if not os.path.exists(model_path):
    print("Mengunduh model MediaPipe Hand Landmarker...")
    url = "https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task"
    urllib.request.urlretrieve(url, model_path)
    print("Model berhasil diunduh!")

# Konfigurasi MediaPipe HandLandmarker (Tasks API)
base_options = python.BaseOptions(model_asset_path=model_path)
options = vision.HandLandmarkerOptions(
    base_options=base_options,
    num_hands=1,
    min_hand_detection_confidence=0.7,
    min_tracking_confidence=0.7
)
detector = vision.HandLandmarker.create_from_options(options)

# Koneksi antar titik sendi tangan untuk menggambar kerangka
HAND_CONNECTIONS = [
    (0, 1), (1, 2), (2, 3), (3, 4),        # Ibu jari
    (0, 5), (5, 6), (6, 7), (7, 8),        # Telunjuk
    (5, 9), (9, 10), (10, 11), (11, 12),   # Jari tengah
    (9, 13), (13, 14), (14, 15), (15, 16), # Jari manis
    (13, 17), (17, 18), (18, 19), (19, 20),# Kelingking
    (0, 17)                                # Telapak tangan
]

# Setup UDP Socket untuk mengirim koordinat tangan ke Unity
UDP_IP = "127.0.0.1"
UDP_PORT = 5052
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Buka kamera webcam
cap = cv2.VideoCapture(0)

if not cap.isOpened():
    print("Error: Webcam tidak dapat dibuka.")
    exit()

print("Webcam aktif. Tekan 'q' pada jendela video untuk keluar.")

while True:
    ret, frame = cap.read()
    if not ret:
        break

    # Flip horizontal agar seperti cermin
    frame = cv2.flip(frame, 1)
    h, w, _ = frame.shape

    # Konversi BGR ke RGB dan bungkus ke format mp.Image
    rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)
    detection_result = detector.detect(mp_image)

    if detection_result.hand_landmarks:
        for hand_landmarks in detection_result.hand_landmarks:
            # Gambar garis penghubung & titik sendi tangan
            for start_idx, end_idx in HAND_CONNECTIONS:
                start = hand_landmarks[start_idx]
                end = hand_landmarks[end_idx]
                cv2.line(frame, (int(start.x * w), int(start.y * h)),
                         (int(end.x * w), int(end.y * h)), (255, 255, 255), 2)

            for lm in hand_landmarks:
                cv2.circle(frame, (int(lm.x * w), int(lm.y * h)), 4, (0, 0, 255), -1)

            # Titik tengah telapak tangan (Landmark 9) untuk registrasi objek maya
            anchor = hand_landmarks[9]
            pixel_x, pixel_y = int(anchor.x * w), int(anchor.y * h)

            # Visualisasi placeholder objek maya yang menempel mengikuti tangan
            cv2.circle(frame, (pixel_x, pixel_y), 25, (0, 255, 0), 3)
            cv2.putText(frame, "Objek Maya Terikat", (pixel_x - 60, pixel_y - 35),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)

            # Kirim data koordinat normalisasi via UDP ke Unity
            data = json.dumps({
                "hand_detected": True,
                "x": round(anchor.x, 4),
                "y": round(anchor.y, 4),
                "z": round(anchor.z, 4)
            })
            sock.sendto(data.encode('utf-8'), (UDP_IP, UDP_PORT))
    else:
        data = json.dumps({"hand_detected": False})
        sock.sendto(data.encode('utf-8'), (UDP_IP, UDP_PORT))

    cv2.imshow("AR Webcam & Registrasi Objek Tangan", frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()