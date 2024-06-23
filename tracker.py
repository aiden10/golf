import cv2 
import numpy as np
import threading
import math
from fastapi import FastAPI
from fastapi.responses import JSONResponse

# url = 'http://192.168.111.63:8080/video'
url = 'http://10.0.0.223:8080/video'

app = FastAPI()
prev_ball = None
ball_movements = []
frame_count = 0
still_frame_count = 0
capture_duration = 80
still_duration = 60  # time tracked ball must be still for  
ready = False
moving = False
waiting_for_reset = False

def process(img):
    """
    Different values for color isolation. Leaving these here commented out just so I can swap them out for testing different colored balls.
    lower_white = np.array([0, 0, 180], dtype=np.uint8)
    upper_white = np.array([180, 50, 255], dtype=np.uint8)

    basement_lower_white = np.array([0, 0, 125], dtype=np.uint8)
    basement_upper_white = np.array([30, 255, 255], dtype=np.uint8)

    lower_green = np.array([50, 3, 0], dtype=np.uint8)
    upper_green = np.array([67, 255, 255], dtype=np.uint8)

    lower_yellow = np.array([0, 185, 0], dtype=np.uint8)
    upper_yellow = np.array([180, 255, 255], dtype=np.uint8)

    lower_pink = np.array([0, 120, 142], dtype=np.uint8)
    upper_pink = np.array([100, 255, 255], dtype=np.uint8)

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

    lower = np.array([lower_h, lower_s, lower_v], dtype=np.uint8)
    upper = np.array([upper_h, upper_s, upper_v], dtype=np.uint8)
    lower_pink = np.array([0, 130, 142], dtype=np.uint8)
    upper_pink = np.array([180, 255, 255], dtype=np.uint8)

    mask = cv2.inRange(hsv, lower_pink, upper_pink)
    mask = cv2.GaussianBlur(mask, (5, 5), 0)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    # mask = cv2.resize(mask, (1920, 1080))
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
    cv2.circle(img, (x, y), radius, rgb, 5)

def trace_movements(img, movements):
    print(f'movements recorded: {len(movements)}')
    for (x, y, radius) in movements:
        if radius <= 0:
            print(f"Invalid radius: {radius}. Skipping this movement.")
            continue

        # Validate x and y coordinates
        if x < 0 or y < 0 or x >= img.shape[1] or y >= img.shape[0]:
            print(f"Invalid coordinates: ({x}, {y}). Skipping this movement.")
            continue

        # Draw the circle
        try:
            cv2.circle(img, (x, y), radius, (0, 0, 255), 2)
        except cv2.error as e:
            print(f"Failed to draw circle at ({x}, {y}) with radius {radius}: {e}")
            
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
            ball_x, ball_y, ball_radius = -1000, -1000, -1000 # consistent placeholder for the Unity script 
            ball_detected, ball = track_ball(frame)
            if ball_detected:
                ball_x, ball_y, ball_radius = ball[0][0], ball[0][1], ball[1]
                if prev_ball:
                    prev_ball_x, prev_ball_y, prev_ball_radius = prev_ball[0][0], prev_ball[0][1], prev_ball[1] 
                    
                    # if it detects another circle somewhere else, don't want that to count
                    max_x_displacement = (ball_radius * 10) # with good tracking this almost isn't needed
                    max_y_displacement = (ball_radius * 10)
                    
                    # if it redraws the circle in a slightly different position, also don't want that to count
                    # instead of just comparing the previous ball position, I could try comparing the previous n positions
                    # but that would really only be necessary if the tracking is bad
                    min_x_displacement = (ball_radius * 0.1)
                    min_y_displacement = (ball_radius * 0.1)
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

                prev_ball = ball
                
            if moving and frame_count < capture_duration:  # add even if ball is not detected, moving, and tracking
                ball_movements.append((ball_x, ball_y, ball_radius))

            if frame_count < capture_duration and moving and not waiting_for_reset:  # increase even if ball isn't detected
                frame_count += 1 

            if frame_count >= capture_duration:
                waiting_for_reset = True
                frame_count = 0
                moving = False
                height, width = 1000, 1000
                image = np.zeros((height, width, 3), dtype=np.uint8)
                # trace_movements(image, ball_movements) # causing a lot of errors at the moment

            frame = cv2.resize(frame, (800, 500))
            cv2.imshow('frame', frame)
            
        q = cv2.waitKey(1)
        if q == ord("q"):
            cap.release()
            cv2.destroyAllWindows()
            break

@app.get('/')
async def root():
    
    x_increases, x_decreases, y_increases, y_decreases = 0, 0, 0, 0
    radius_sum = 0
    ball_detected_frames = 0
    prev_x, prev_y, prev_radius = None, None, None
    speed, direction, angle, rise = 0, "None", 0, 0 # default values, might be redundant
    detected_positions = []
    if waiting_for_reset: # means that the has been hit and finished 
        for frame in range(len(ball_movements)):
            x, y, radius = ball_movements[frame][0], ball_movements[frame][1], ball_movements[frame][2]
            # -1000 is what the each value is if the ball wasn't detected that frame. Figured it'd be helpful to know what frame a particular position came from 
            if x != -1000 and y != -1000 and radius != -1000: 
                ball_detected_frames += 1
                detected_positions.append((ball_movements[frame][0], ball_movements[frame][1], ball_movements[frame][2]))
                if prev_x and prev_y and prev_radius: # ensure previous positions exist
                    x_displacement = x - prev_x
                    y_displacement = y - prev_y
                    radius_sum += radius
                    if x_displacement < 0: # ball moved left
                        x_decreases += (x_displacement * -1)
                    else: # ball moved right
                        x_increases += x_displacement

                    if y_displacement < 0:
                        y_decreases += (y_displacement * -1)
                    else:
                        y_increases += y_displacement

                prev_x, prev_y, prev_radius = x, y, radius

        # end calculations
        # Speed
        average_radius = radius_sum / ball_detected_frames
        initial_x, initial_y, initial_radius = detected_positions[0][0], detected_positions[0][1], detected_positions[0][2]
        last_x, last_y, last_radius = detected_positions[-1][0], detected_positions[-1][1], detected_positions[-1][2]
        total_x_displacement = abs(initial_x - last_x)
        total_y_displacement = abs(initial_y - last_y)

        # dividing by capture duration here and not len(detected_positions) for consistency 
        x_displacement_rate = (total_x_displacement / capture_duration) / average_radius # pixels per frame, divided by radius for consistency 
        y_displacement_rate = (total_y_displacement / capture_duration) / average_radius 
        speed = (x_displacement_rate ** 2 + y_displacement_rate ** 2) ** 0.5 # ball radiuses per frame

        # Angle
        angle = math.atan2(last_x - initial_x, last_y - initial_y) * 180 / math.pi

        # Rise
        rise = abs(initial_radius - last_radius) / average_radius
        
        directions = {
            "Right": x_increases,
            "Left": x_decreases,
            "Up": y_increases,
            "Down": y_decreases
        }
        direction = max(directions.items()) # find which direction the ball has increased the most
    
    if not waiting_for_reset: # don't want to send anything until the ball has finished moving and its status is fully calculated
        speed, direction, angle, rise = 0, "None", 0, 0
    response = {
        "status": {
            "speed": speed,
            "direction": direction,
            "angle": angle,
            "rise": rise
        }
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
    cv2.resizeWindow('Trackbars', 200, 250)
    cv2.createTrackbar('LowerH', 'Trackbars', 0, 180, nothing)
    cv2.createTrackbar('LowerS', 'Trackbars', 0, 255, nothing)
    cv2.createTrackbar('LowerV', 'Trackbars', 200, 255, nothing)
    cv2.createTrackbar('UpperH', 'Trackbars', 180, 180, nothing)
    cv2.createTrackbar('UpperS', 'Trackbars', 55, 255, nothing)
    cv2.createTrackbar('UpperV', 'Trackbars', 255, 255, nothing)

    threading.Thread(target=start_server, daemon=True).start()

    capture_video()
