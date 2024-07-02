# Mini Golf Simulator

![cut1-ezgif com-optimize](https://github.com/aiden10/golf/assets/51337166/12798f5d-6bfc-4bc3-aca4-e01b9f410a8f)

## About
This is an attempt to create a mini golf simulator using OpenCV for ball tracking and Unity to translate the recorded movements into a virtual environment.
After a ball is detected, the movements are tracked and the speed, angle, direction, and rise are calculated. Those values are then served via a FastAPI endpoint which the Unity side constantly fetches.

### Features
- Real time ball tracking 
- Course Selection
- Multiple Players
- Color customization
- Hole detection
- Scoreboard

## Setup
Setting it up requires the IP Webcam app on a mobile device or another external camera. The URL environment variable can be modified or you can modify the capture source to be that of your camera.
The trackbars can be modified until they detect the ball's colors accurately. A bright and distinctly colored ball works best. Once a ball is detected you'll see a dark blue circle around it, after it has been still for long enough the circle will turn green, after which any movements to the ball will be considered a hit and the circle will turn light blue.

## Notes
I plan to try adding more courses, improve the UI and also compile the Unity side into an exe. Along with other features to improve the user experience. Currently, the setup is a bit convoluted and requires modifiying the code which isn't the most accessible, but I'm not sure when I'll make these improvements yet. There's also the matter of requiring a Python script to handle the ball tracking and I'm unsure if there's a way to incorporate that into Unity.
