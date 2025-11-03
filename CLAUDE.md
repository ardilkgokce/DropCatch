# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 2022.3.62f3 project called "DropCatch" - a Kinect-enabled 2D catching game where players use physical hand movements to control a basket and catch falling objects. The project integrates Microsoft Kinect v2 SDK for motion detection and hand tracking.

## Core Architecture

### Kinect Integration
- **KinectManager**: Central singleton managing Kinect sensor data, user detection, and joint tracking
- **HandObjectChecker**: Detects when hands are holding objects using depth analysis and fill ratios
- **Joint Tracking**: Now prioritizes elbow tracking over hand tracking for better stability
- Located in `Assets/K2Examples/KinectScripts/` - extensive Kinect v2 wrapper library

### Game Components
- **GameManager2D**: Main game controller handling score, timer, combo system, and calibration flow
- **BasketController2D**: Translates Kinect elbow/hand positions to 2D basket movement with calibration
- **FallingObject2D**: Physics-based falling objects with collision detection
- **ObjectSpawner2D**: Spawns objects with increasing difficulty over time
- **PhysicalBasketDetector**: Advanced detection system for determining when player is "holding" basket with multiple modes
- **BasketDebugUI**: Real-time debug interface displaying detailed hand tracking and basket detection status
- **KinectSmoothingManager**: Manages smoothing parameters to reduce Kinect tracking jitter

### Key Systems
1. **Calibration System**: User must be detected and calibrate their center position before gameplay
2. **Basket Detection System**: PhysicalBasketDetector determines if player is "holding" the basket
   - **Easy Mode**: Basket turns green when both elbows/hands are visible (default)
   - **Advanced Mode**: Checks hand distance, height, state, and position for realistic detection
   - **Position Memory**: Maintains basket position briefly when joints disappear to prevent jumping
   - **Tracking Priority**: Elbows → Hands (elbows are more stable and visible)
3. **Movement Mapping**: Kinect 3D coordinates → 2D screen coordinates with smooth interpolation
4. **Combo System**: Consecutive catches multiply score up to 5x
5. **Visual Feedback**: Basket color indicates state - white (normal), blue (hands visible but not holding), green (holding)

## Common Development Commands

### Unity Editor
- Open project in Unity 2022.3.62f3
- Main game scene: `Assets/Scenes/BasketCatchGame.unity`
- Test scene: `Assets/Scenes/SampleScene.unity`

### Build Process
- Unity builds through File → Build Settings
- Target platform settings in ProjectSettings/
- No external build scripts present

### Testing
- Play mode testing in Unity Editor
- Requires Kinect v2 sensor for full functionality
- Keyboard Space key for calibration in test mode

## Code Structure

### Core Scripts Location
- Game logic: `Assets/Scripts/`
- Kinect integration: `Assets/K2Examples/KinectScripts/`
- Demo examples: `Assets/K2Examples/KinectDemos/`

### Kinect Coordinate System
- Kinect uses meter-based 3D coordinates
- Game converts to Unity world coordinates with scale factors
- Calibration establishes center offset for relative positioning

### Dependencies
- Microsoft Kinect v2 SDK (via included DLLs)
- Unity TextMeshPro for UI
- Standard Unity physics and rendering systems

## Important Configuration

### Kinect Settings
- Sensor height: 1.0m default in KinectManager
- User detection distance limits configurable
- Hand tracking thresholds in HandObjectChecker

### Game Parameters
- Basket movement speed and range in BasketController2D
- Spawn rates and difficulty curves in ObjectSpawner2D
- Scoring and combo system in GameManager2D
- Detection sensitivity and modes in PhysicalBasketDetector
  - easyDetectionMode: true = 2 hands visible (simple), false = advanced conditions
  - positionMemoryTime: How long to remember hand positions (default 2s)
  - detectionFrameThreshold: Frames needed for stable detection (default 3)

### Visual Feedback
- Color-coded basket states:
  - White: Normal state (no hands detected)
  - Blue: Both hands visible but not in holding position
  - Green: Basket is being held (catches enabled)
- Scale changes when basket is "held" (1.2x size)
- Combo multiplier display
- Debug UI overlay showing detailed tracking information (toggleable)

## Native Dependencies

The project includes numerous DLLs for Kinect functionality:
- Kinect20.Face.dll, KinectUnityAddin.dll, etc.
- Platform-specific builds required for deployment
- Development requires Kinect v2 SDK installation

## Language and Localization

Code comments and UI text are in Turkish, indicating target audience localization.