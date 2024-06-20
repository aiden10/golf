import cv2 
import numpy as np
import threading
from fastapi import FastAPI
from fastapi.responses import JSONResponse

# lower_white = np.array([0, 0, 180], dtype=np.uint8)
# upper_white = np.array([180, 50, 255], dtype=np.uint8)

url = 'http://192.168.111.63:8080/video'
# url = 'http://10.0.0.223:8080/video'
# cap = cv2.VideoCapture(url)
app = FastAPI()
prev_ball = None
ball_movements = []
frame_count = 0
capture_duration = 120
moving = False
waiting_for_reset = False

def process(img):
    hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)
    lower_h = cv2.getTrackbarPos('LowerH', 'Trackbars')
    lower_s = cv2.getTrackbarPos('LowerS', 'Trackbars')
    lower_v = cv2.getTrackbarPos('LowerV', 'Trackbars')
    upper_h = cv2.getTrackbarPos('UpperH', 'Trackbars')
    upper_s = cv2.getTrackbarPos('UpperS', 'Trackbars')
    upper_v = cv2.getTrackbarPos('UpperV', 'Trackbars')
    lower_white = np.array([lower_h, lower_s, lower_v], dtype=np.uint8)
    upper_white = np.array([upper_h, upper_s, upper_v], dtype=np.uint8)

    mask = cv2.inRange(hsv, lower_white, upper_white)
    mask = cv2.GaussianBlur(mask, (5, 5), 0)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))

    cv2.imshow("mask", mask)
    return mask

def track_ball(img):
    mask = process(img)
    _, th = cv2.threshold(mask, 127, 255, cv2.THRESH_BINARY+ cv2.THRESH_OTSU) 
    th_er = cv2.erode(th, np.ones((15, 15), np.uint8))
    th_er1 = 255-cv2.bitwise_not(th_er) 
    contours, _ = cv2.findContours(th_er1, cv2.RETR_TREE, cv2.CHAIN_APPROX_NONE)
    white_areas = []
    whitest = 0

    for cnt in contours:
        area = cv2.contourArea(cnt)
        white_areas.append(area)

    if len(white_areas) > 0:
        whitest = max(white_areas)

    if whitest > 800:  # minimum area to draw circle
        (x, y), radius = cv2.minEnclosingCircle(cnt)
        if radius > 40:
            center = (int(x), int(y))
            radius = int(radius)
            ball = (center, radius)
            cv2.circle(img, center, radius, (0, 0, 255), 2)
            return True, ball # circle drawn
    
    return False, None   # circle not drawn    
    
def draw_circles_with_delay(img, circles):
    for (x, y, radius) in circles:
        # Draw the circle
        cv2.circle(img, (x, y), radius, (0, 0, 255), 2)
        cv2.imshow("Circles", img)
        cv2.waitKey(25) 

        # Erase the circle by redrawing the original area (this is a simple approach)
        img_copy = img.copy()
        cv2.circle(img_copy, (x, y), radius, (0, 0, 0), -1)
        img = img_copy.copy()
        cv2.imshow("Circles", img)
        cv2.waitKey(25)  

    cv2.destroyWindow("Circles")

def nothing(x):
    pass 

cv2.namedWindow('Trackbars')

# Create trackbars for adjusting HSV values
cv2.createTrackbar('LowerH', 'Trackbars', 0, 180, nothing)
cv2.createTrackbar('LowerS', 'Trackbars', 0, 255, nothing)
cv2.createTrackbar('LowerV', 'Trackbars', 200, 255, nothing)
cv2.createTrackbar('UpperH', 'Trackbars', 180, 180, nothing)
cv2.createTrackbar('UpperS', 'Trackbars', 55, 255, nothing)
cv2.createTrackbar('UpperV', 'Trackbars', 255, 255, nothing)

def capture_video():
    global prev_ball, frame_count, capture_duration, moving, ball_movements, waiting_for_reset
    cap = cv2.VideoCapture(0)
    while(True):
        ret, frame = cap.read()
        hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)

        if frame is not None:
            ball_detected, ball = track_ball(frame)

            if ball_detected:
                if prev_ball:
                    prev_ball_x, prev_ball_y, prev_ball_radius = prev_ball[0][0], prev_ball[0][1], prev_ball[1] 
                    ball_x, ball_y, ball_radius = ball[0][0], ball[0][1], ball[1]
                    
                    # if it detects another circle somewhere else, don't want that to count
                    max_x_displacement = (ball_radius * 2) 
                    max_y_displacement = (ball_radius * 2)
                    
                    # if it redraws the circle in a slightly different position, also don't want that to count
                    min_x_displacement = (ball_radius * 0.15)
                    min_y_displacement = (ball_radius * 0.15)
                    x_displacement = abs(prev_ball_x - ball_x)
                    y_displacement = abs(prev_ball_y - ball_y)
                    max_radius_difference = 15
                    radius_difference = abs(prev_ball_radius - ball_radius)
                    # print(f'Displacements: {x_displacement}, {y_displacement}')
                    # print(f'Radius: {ball_radius}')
                    # print(f'Min x: {min_x_displacement}')
                    # print(f'Min y: {min_y_displacement}')
                    # print(f'Max x: {max_x_displacement}')
                    # print(f'Max y: {max_y_displacement}')

                    if (x_displacement > min_x_displacement and x_displacement < max_x_displacement and
                        y_displacement > min_y_displacement and y_displacement < max_y_displacement and
                        radius_difference < max_radius_difference) and not moving and not waiting_for_reset:
                        moving = True
                        print('ball moving')
                        # print(f'ball moved from ({prev_ball_x}, {prev_ball_y}) to ({ball_x}, {ball_y})')
            
                if moving and frame_count < capture_duration: # ball detected, moving, and tracking
                    ball_movements.append((ball_x, ball_y, ball_radius))

                prev_ball = ball

            if frame_count < capture_duration and moving and not waiting_for_reset: # increase even if ball isn't detected
                frame_count += 1 
                print(f'frame_count: {frame_count}')

            if frame_count >= capture_duration:
                waiting_for_reset = True
                frame_count = 0
                moving = False
                height, width = 1000, 1000
                image = np.zeros((height, width, 3), dtype=np.uint8)
                draw_circles_with_delay(image, ball_movements)
                # break

            cv2.imshow('frame',frame)
            
        q = cv2.waitKey(1)
        if q == ord("q"):
            break

    cap.release()
    cv2.destroyAllWindows()

@app.get('/')
async def root():
    return {"status": moving, "movements": ball_movements}

@app.get('/reset')
async def reset():
    global ball_movements, waiting_for_reset
    ball_movements = []
    waiting_for_reset = False
    return JSONResponse(content={"status": "Movements cleared"})

def start_video_capture():
    capture_thread = threading.Thread(target=capture_video)
    capture_thread.daemon = True
    capture_thread.start()

if __name__ == "__main__":
    start_video_capture()
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)