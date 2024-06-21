import cv2 
import numpy as np
import threading
from fastapi import FastAPI
from fastapi.responses import JSONResponse

# url = 'http://192.168.111.63:8080/video'
url = 'http://10.0.0.223:8080/video'

app = FastAPI()
prev_ball = None
ball_movements = []
frame_count = 0
still_frame_count = 0
capture_duration = 120
still_duration = 60  # time tracked ball must be still for  
ready = False
moving = False
waiting_for_reset = False

def process(img):
    """
    lower_white = np.array([0, 0, 180], dtype=np.uint8)
    upper_white = np.array([180, 50, 255], dtype=np.uint8)

    basement_lower_white = np.array([0, 0, 125], dtype=np.uint8)
    basement_upper_white = np.array([30, 255, 255], dtype=np.uint8)

    lower_green = np.array([50, 3, 0], dtype=np.uint8)
    upper_green = np.array([67, 255, 255], dtype=np.uint8)

    lower_yellow = np.array([0, 185, 0], dtype=np.uint8)
    upper_yellow = np.array([180, 255, 255], dtype=np.uint8)

    lower = np.array([lower_h, lower_s, lower_v], dtype=np.uint8)
    upper = np.array([upper_h, upper_s, upper_v], dtype=np.uint8)
    """
    hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)
    lower_h = cv2.getTrackbarPos('LowerH', 'Trackbars')
    lower_s = cv2.getTrackbarPos('LowerS', 'Trackbars')
    lower_v = cv2.getTrackbarPos('LowerV', 'Trackbars')
    upper_h = cv2.getTrackbarPos('UpperH', 'Trackbars')
    upper_s = cv2.getTrackbarPos('UpperS', 'Trackbars')
    upper_v = cv2.getTrackbarPos('UpperV', 'Trackbars')
    basement_lower_white = np.array([0, 0, 125], dtype=np.uint8)
    basement_upper_white = np.array([30, 255, 255], dtype=np.uint8)

    lower = np.array([lower_h, lower_s, lower_v], dtype=np.uint8)
    upper = np.array([upper_h, upper_s, upper_v], dtype=np.uint8)
    lower_green = np.array([50, 3, 0], dtype=np.uint8)
    upper_green = np.array([67, 255, 255], dtype=np.uint8)

    mask = cv2.inRange(hsv, lower, upper)
    mask = cv2.GaussianBlur(mask, (5, 5), 0)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    mask = cv2.resize(mask, (1920, 1080))
    cv2.imshow("mask", mask)
    return mask

def track_ball(img):
    mask = process(img)
    _, th = cv2.threshold(mask, 127, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU) 
    th_er = cv2.erode(th, np.ones((15, 15), np.uint8))
    th_er1 = 255 - cv2.bitwise_not(th_er) 
    contours, _ = cv2.findContours(th_er1, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
    white_areas = []
    whitest = 0

    for cnt in contours:
        area = cv2.contourArea(cnt)
        white_areas.append((area, cnt))

    if len(white_areas) > 0:
        whitest = max(white_areas, key=lambda x: x[0])
        _, cnt = whitest

        if whitest[0] > 100:  # minimum area to draw circle
            (x, y), radius = cv2.minEnclosingCircle(cnt)
            if radius > 25:
                center = (int(x), int(y))
                radius = int(radius)
                ball = (center, radius)
                return True, ball  # ball detected
    
    return False, None  # ball not detected  

def draw_ball_outline(img, x, y, radius, rgb):
    cv2.circle(img, (x, y), radius, rgb, 2)

def trace_movements(img, movements):
    print(f'movements recorded: {len(movements)}')
    for (x, y, radius) in movements:
        cv2.circle(img, (x, y), radius, (0, 0, 255), 2)
        cv2.imshow("Replay", img)
        cv2.waitKey(30) 

    cv2.destroyWindow("Replay")

def nothing(x):
    pass 

def capture_video():
    global prev_ball, frame_count, still_frame_count, moving, ready, ball_movements, waiting_for_reset
    # cap = cv2.VideoCapture(0)
    cap = cv2.VideoCapture(url)
    while True:
        ret, frame = cap.read()
        hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)
        color = (255, 0, 0)  # default to red
        if frame is not None: 
            ball_detected, ball = track_ball(frame)
            if ball_detected:
                ball_x, ball_y, ball_radius = ball[0][0], ball[0][1], ball[1]
                if prev_ball:
                    prev_ball_x, prev_ball_y, prev_ball_radius = prev_ball[0][0], prev_ball[0][1], prev_ball[1] 
                    
                    # if it detects another circle somewhere else, don't want that to count
                    max_x_displacement = (ball_radius * 5) 
                    max_y_displacement = (ball_radius * 5)
                    
                    # if it redraws the circle in a slightly different position, also don't want that to count
                    min_x_displacement = (ball_radius * 0.05)
                    min_y_displacement = (ball_radius * 0.05)
                    x_displacement = abs(prev_ball_x - ball_x)
                    y_displacement = abs(prev_ball_y - ball_y)
                    max_radius_difference = 15
                    radius_difference = abs(prev_ball_radius - ball_radius)

                    if (x_displacement > min_x_displacement and x_displacement < max_x_displacement and
                        y_displacement > min_y_displacement and y_displacement < max_y_displacement and
                        radius_difference < max_radius_difference) and not moving and not waiting_for_reset and ready:
                        print('tracking movements')
                        ready = False
                        moving = True

                    if x_displacement < 20 and y_displacement < 20 and radius_difference < 20 and not waiting_for_reset:  # check that ball has not moved much
                        still_frame_count += 1
                    else:
                        still_frame_count = 0
                        ready = False

                    if still_frame_count >= still_duration and not moving:
                        ready = True

                    if not ready:
                        color = (255, 0, 0)  # red circle while not ready
                        if moving:  # hit and movements are being recorded
                            color = (252, 198, 3)  # draw yellow circle
                    else:
                        color = (0, 255, 0)  # green

                draw_ball_outline(frame, ball_x, ball_y, ball_radius, color)

                if moving and frame_count < capture_duration:  # ball detected, moving, and tracking
                    ball_movements.append((ball_x, ball_y, ball_radius))

                prev_ball = ball

            if frame_count < capture_duration and moving and not waiting_for_reset:  # increase even if ball isn't detected
                frame_count += 1 

            if frame_count >= capture_duration:
                waiting_for_reset = True
                frame_count = 0
                moving = False
                height, width = 1000, 1000
                image = np.zeros((height, width, 3), dtype=np.uint8)
                trace_movements(image, ball_movements)

            frame = cv2.resize(frame, (800, 500))
            cv2.imshow('frame', frame)
            
        q = cv2.waitKey(1)
        if q == ord("q"):
            cap.release()
            cv2.destroyAllWindows()
            break

@app.get('/')
async def root():
    global moving # not sure if I even really need this
    # should be adding a tuple every frame regardless of whether or not the ball moved to help calculations
    # need to determine ball speed, direction, angle, and maybe height by seeing how the radius changes? 
    # in order for that to work though, the tracking needs to be pretty good and consistent 
    response = {
        "status": moving,
        "movements": [
            {"x": x, "y": y, "radius": radius} for (x, y, radius) in ball_movements
        ]
    }
    return JSONResponse(content=response)

@app.get('/reset')
async def reset():
    global ball_movements, waiting_for_reset, moving
    if not moving: 
        ball_movements = []
        waiting_for_reset = False
        return JSONResponse(content={"status": "Movements cleared"})
    else:
        return JSONResponse(content={"status": "Movements being recorded"})

def start_server():
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)

if __name__ == "__main__":
    cv2.namedWindow('Trackbars')
    cv2.createTrackbar('LowerH', 'Trackbars', 0, 180, nothing)
    cv2.createTrackbar('LowerS', 'Trackbars', 0, 255, nothing)
    cv2.createTrackbar('LowerV', 'Trackbars', 200, 255, nothing)
    cv2.createTrackbar('UpperH', 'Trackbars', 180, 180, nothing)
    cv2.createTrackbar('UpperS', 'Trackbars', 55, 255, nothing)
    cv2.createTrackbar('UpperV', 'Trackbars', 255, 255, nothing)

    threading.Thread(target=start_server, daemon=True).start()

    capture_video()
