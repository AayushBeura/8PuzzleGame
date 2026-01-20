![ChatGPT Image Jan 21, 2026, 01_39_39 AM](https://github.com/user-attachments/assets/a477f9ad-f56d-4763-8766-dd3968efbc83)
# 8-Puzzle Solver (Unity)

A clean sliding 8-puzzle game built in Unity for mobile. Arrange tiles 1–8 with one empty space, or let the built-in solver show you the optimal solution step by step.

## Gameplay

- Tap a tile adjacent to the empty cell to slide it into the empty space.
- Goal layout:  
  `1 2 3`  
  `4 5 6`  
  `7 8 []`
- Use the Auto-Solve button when stuck to watch the shortest solution sequence.
- Move counter shows how many moves you used versus the theoretical minimum.

## Features

- Automatic grid layout that adapts to screen size and aspect ratio.
- Auto-solver based on A* search with a Manhattan-distance heuristic.
- Optional distance/heuristic hints for each tile.
- Simple UI designed for touch controls and mobile performance.

## Running The Game

### Players (Android)

1. Install the provided APK on your Android device.
2. Launch the app and tap **New Game** to start a puzzle.
3. Use the Auto-Solve button if you want to see the optimal path.

### Developers

1. Open the project in Unity (2021.3 or later recommended).
2. Load the main scene and press **Play** in the Editor, or
3. Use **Build Settings → Android → Build** to create an APK.

## Notes

- Best played in portrait mode (or your chosen orientation).
- Designed for personal/educational use; update the license section if you distribute it.
