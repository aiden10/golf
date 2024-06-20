import cv2 
import numpy as np

def nothing(x):
    pass

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

cv2.namedWindow('Trackbars')
cv2.createTrackbar('LowerH', 'Trackbars', 0, 180, nothing)
cv2.createTrackbar('LowerS', 'Trackbars', 0, 255, nothing)
cv2.createTrackbar('LowerV', 'Trackbars', 200, 255, nothing)
cv2.createTrackbar('UpperH', 'Trackbars', 180, 180, nothing)
cv2.createTrackbar('UpperS', 'Trackbars', 55, 255, nothing)
cv2.createTrackbar('UpperV', 'Trackbars', 255, 255, nothing)

cap = cv2.VideoCapture(0)
while(True):
    ret, frame = cap.read()
    if frame is not None:
        process(frame)
        cv2.imshow('frame',frame)
            
    q = cv2.waitKey(1)
    if q == ord("q"):
        cap.release()
        cv2.destroyAllWindows()
        break
