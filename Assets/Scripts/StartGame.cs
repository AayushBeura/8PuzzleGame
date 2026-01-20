using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StartGame : MonoBehaviour
{
    [Header("Start Screen")]
    public GameObject titleObject;
    public GameObject playSprite;
    public float titleSlideOffset = 5f;
    public float titleSlideDuration = 0.3f;
    public float playFadeDuration = 0.3f;

    [Header("Puzzle Grid - Just Upload Sprite")]
    public Sprite tileSprite;
    public float tileGap = 0.1f;
    public Color tileColor = Color.white;
    public Color numberColor = Color.black;
    public int fontSize = 48;

    [Header("Cool Features")]
    public GameObject autoSolveSprite;
    public GameObject toggleManhattanSpriteOFF;
    public GameObject toggleManhattanSpriteON;
    public bool showManhattanDistances = false;
    public Color manhattanTextColor = Color.red;
    public float autoSolveStepDelay = 0.3f;

    [Header("Completion")]
    public GameObject nextButtonSprite;  // Just next button, no dialog

    private GameObject[,] tiles = new GameObject[3, 3];
    private Vector2Int emptyCell;
    private Transform gridParent;
    private float tileSize;

    [Header("HUD")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI movesText;
    public TextMeshProUGUI minMovesStartText;
    public TextMeshProUGUI minMovesCurrentText;


    [Header("Completion Stats")]
    public TextMeshProUGUI completionTimeText;  // Time taken
    public TextMeshProUGUI completionMovesText; // Moves taken


    private bool gameStarted = false;
    private float elapsedTime = 0f;
    private int moves = 0;
    private bool isAutoSolving = false;
    private int minMovesAtStart = 0;
    private bool puzzleCompleted = false;
    private bool isAnimatingMove = false;

    void Start()
    {
        // Mobile optimization
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        QualitySettings.vSyncCount = 0;

        // FIX BACKGROUND CONFLICT
        GameObject background = GameObject.Find("BackgroundImage");
        if (background != null)
        {
            SpriteRenderer bgSR = background.GetComponent<SpriteRenderer>();
            if (bgSR != null)
            {
                bgSR.sortingOrder = -100; // WAY behind everything
                Debug.Log("Background sorting set to -100");
            }

            Vector3 bgPos = background.transform.position;
            bgPos.z = 10f; // Far back (positive Z = away from camera in ortho)
            background.transform.position = bgPos;
            Debug.Log($"Background moved to Z={bgPos.z}");
        }

        if (autoSolveSprite != null)
        {
            autoSolveSprite.SetActive(false);
        }

        if (toggleManhattanSpriteOFF != null)
        {
            toggleManhattanSpriteOFF.SetActive(false);
        }

        if (toggleManhattanSpriteON != null)
        {
            toggleManhattanSpriteON.SetActive(false);
        }

        if (nextButtonSprite != null)
        {
            nextButtonSprite.SetActive(false);
        }

        if (completionTimeText != null)
        {
            completionTimeText.gameObject.SetActive(false);
        }

        if (completionMovesText != null)
        {
            completionMovesText.gameObject.SetActive(false);
        }

        // FIX FLICKERING - Separate Z positions for title and play button
        if (titleObject != null)
        {
            Vector3 pos = titleObject.transform.position;
            pos.z = -2f; // Title at -2
            titleObject.transform.position = pos;

            SpriteRenderer sr = titleObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 5;
            }

            Debug.Log($"Title positioned at Z={pos.z}");
        }

        if (playSprite != null)
        {
            Vector3 pos = playSprite.transform.position;
            pos.z = -3f; // Play button at -3 (in front of title)
            playSprite.transform.position = pos;

            SpriteRenderer sr = playSprite.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 10; // Higher sorting order
            }

            Debug.Log($"Play button positioned at Z={pos.z}");
        }

        SetupButton(autoSolveSprite, "AutoSolve");
        SetupButton(toggleManhattanSpriteOFF, "ManhattanOFF");
        SetupButton(toggleManhattanSpriteON, "ManhattanON");
        SetupButton(nextButtonSprite, "NextButton");
    }

    void SetupButton(GameObject button, string buttonName)
    {
        if (button == null)
        {
            Debug.LogWarning($"{buttonName} button is NULL!");
            return;
        }

        BoxCollider2D collider = button.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = button.AddComponent<BoxCollider2D>();
            Debug.Log($"Added BoxCollider2D to {buttonName}");
        }

        SpriteRenderer sr = button.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100;
            Debug.Log($"{buttonName} sorting order set to 100");
        }

        Vector3 pos = button.transform.position;
        pos.z = -5f;
        button.transform.position = pos;
        Debug.Log($"{buttonName} position: {button.transform.position}");
    }

    void Update()
    {
        if (!gameStarted)
        {
            HandleStartScreen();
        }
        else
        {
            HandleGameInputAndHUD();
        }
    }

    void HandleStartScreen()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 click = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // DON'T set click.z to 0 - keep it as is OR check 2D bounds

            if (playSprite != null)
            {
                SpriteRenderer sr = playSprite.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Bounds bounds = sr.bounds;

                    // 2D click detection (ignore Z axis)
                    Vector2 clickPos2D = new Vector2(click.x, click.y);
                    Vector2 boundsCenter2D = new Vector2(bounds.center.x, bounds.center.y);

                    bool containsX = Mathf.Abs(clickPos2D.x - boundsCenter2D.x) <= bounds.extents.x;
                    bool containsY = Mathf.Abs(clickPos2D.y - boundsCenter2D.y) <= bounds.extents.y;

                    if (containsX && containsY)
                    {
                        Debug.Log("Play button clicked!");
                        StartCoroutine(StartGameRoutine());
                    }
                }
            }
        }
    }

    IEnumerator StartGameRoutine()
    {
        if (playSprite != null)
        {
            SpriteRenderer sr = playSprite.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color startColor = sr.color;
                Color endColor = startColor;
                endColor.a = 0;

                float t = 0;
                while (t < playFadeDuration)
                {
                    t += Time.deltaTime;
                    sr.color = Color.Lerp(startColor, endColor, t / playFadeDuration);
                    yield return null;
                }

                sr.color = endColor;
                playSprite.SetActive(false);
            }
        }

        if (titleObject != null)
        {
            Vector3 startPos = titleObject.transform.position;
            Vector3 endPos = startPos + Vector3.up * titleSlideOffset;

            float t = 0;
            while (t < titleSlideDuration)
            {
                t += Time.deltaTime;
                titleObject.transform.position = Vector3.Lerp(startPos, endPos, t / titleSlideDuration);
                yield return null;
            }

            titleObject.transform.position = endPos;
        }

        StartNewPuzzle();
    }

    void StartNewPuzzle()
    {
        Debug.Log("=== STARTING NEW PUZZLE ===");

        if (gridParent != null)
        {
            Destroy(gridParent.gameObject);
        }

        CreateGridAuto();

        // DEBUG: Find what's conflicting
        DebugAllSprites();
        ShuffleGrid(100);

        moves = 0;
        elapsedTime = 0f;
        gameStarted = true;
        puzzleCompleted = false;
        isAutoSolving = false;
        isAnimatingMove = false;

        minMovesAtStart = CalculateTotalManhattan();
        Debug.Log($"Initial min moves: {minMovesAtStart}");

        UpdateManhattanDisplay();
        UpdateMinMoves();

        // Hide next button when starting new puzzle
        if (nextButtonSprite != null)
        {
            nextButtonSprite.SetActive(false);
        }

        // Hide completion stats
        if (completionTimeText != null)
        {
            completionTimeText.gameObject.SetActive(false);
        }

        if (completionMovesText != null)
        {
            completionMovesText.gameObject.SetActive(false);
        }

        if (autoSolveSprite != null)
        {
            autoSolveSprite.SetActive(true);

            Vector3 pos = autoSolveSprite.transform.position;
            pos.z = -5f;
            autoSolveSprite.transform.position = pos;

            Debug.Log($"Auto-solve button SHOWN at position: {autoSolveSprite.transform.position}");
        }

        if (toggleManhattanSpriteOFF != null)
        {
            toggleManhattanSpriteOFF.SetActive(true);
            Vector3 pos = toggleManhattanSpriteOFF.transform.position;
            pos.z = -5f;
            toggleManhattanSpriteOFF.transform.position = pos;
        }

        if (toggleManhattanSpriteON != null)
        {
            toggleManhattanSpriteON.SetActive(false);
        }
    }

    void CreateGridAuto()
    {
        if (tileSprite == null)
        {
            Debug.LogError("❌ No tile sprite assigned!");
            return;
        }

        GameObject parentObj = new GameObject("PuzzleGrid");
        gridParent = parentObj.transform;
        gridParent.position = Vector3.zero;

        Camera cam = Camera.main;
        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        float padding = 0.9f;
        float availableWidth = screenWidth * padding;
        float availableHeight = screenHeight * padding;

        float maxTileSize = Mathf.Min(availableWidth, availableHeight) / 3.5f;
        tileSize = maxTileSize;

        float totalSpacing = tileSize + tileGap;
        float startX = -totalSpacing;
        float startY = totalSpacing;

        int num = 1;
        int tileIndex = 0;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (num <= 8)
                {
                    // Start at Z = -1, then go deeper for each tile
                    Vector3 pos = new Vector3(
                        startX + col * totalSpacing,
                        startY - row * totalSpacing,
                        -1f - (tileIndex * 0.1f) // Tile 1 at -1, Tile 2 at -1.1, etc.
                    );

                    GameObject tile = CreateTile(num, pos, tileIndex * 10); // Big sorting gaps
                    tiles[row, col] = tile;

                    Debug.Log($"Tile {num} created at Z={pos.z}, Sorting={tileIndex * 10}");

                    num++;
                    tileIndex++;
                }
                else
                {
                    tiles[row, col] = null;
                    emptyCell = new Vector2Int(row, col);
                }
            }
        }

        Debug.Log($"Grid created, tile size: {tileSize}");
    }
    void DebugAllSprites()
    {
        SpriteRenderer[] allSprites = FindObjectsOfType<SpriteRenderer>();

        Debug.Log($"===== FOUND {allSprites.Length} SPRITES =====");

        foreach (SpriteRenderer sr in allSprites)
        {
            Debug.Log($"Sprite: {sr.gameObject.name}, Z: {sr.transform.position.z}, Sorting: {sr.sortingOrder}, Layer: {sr.sortingLayerName}");
        }

        Debug.Log("===== END SPRITE LIST =====");
    }

    GameObject CreateTile(int number, Vector3 position, int sortingOffset)
    {
        GameObject tile = new GameObject($"Tile_{number}");
        tile.transform.parent = gridParent;
        tile.transform.position = position;

        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();

        if (tileSprite == null)
        {
            Debug.LogError($"❌ TILE SPRITE IS NULL for tile {number}!");
            return tile;
        }

        sr.sprite = tileSprite;
        sr.color = tileColor;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = sortingOffset; // UNIQUE sorting order per tile

        float spriteWidth = sr.sprite.bounds.size.x;
        float scale = tileSize / spriteWidth;
        tile.transform.localScale = Vector3.one * scale;

        BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();

        // Number text
        GameObject textObj = new GameObject("Number");
        textObj.transform.parent = tile.transform;
        textObj.transform.localPosition = new Vector3(0, 0, -0.2f); // In front of sprite
        textObj.transform.localScale = Vector3.one;

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = number.ToString();
        tmp.fontSize = fontSize;
        tmp.color = numberColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.sortingOrder = sortingOffset + 10; // Above sprite

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 200);

        // Manhattan text
        GameObject manhattanObj = new GameObject("Manhattan");
        manhattanObj.transform.parent = tile.transform;
        manhattanObj.transform.localPosition = new Vector3(0.6f, 0.6f, -0.3f); // Further in front
        manhattanObj.transform.localScale = Vector3.one;

        TextMeshPro manhattanTmp = manhattanObj.AddComponent<TextMeshPro>();
        manhattanTmp.text = "";
        manhattanTmp.fontSize = fontSize * 0.6f;
        manhattanTmp.color = manhattanTextColor;
        manhattanTmp.alignment = TextAlignmentOptions.Center;
        manhattanTmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
        manhattanTmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        manhattanTmp.fontStyle = FontStyles.Bold;
        manhattanTmp.sortingOrder = sortingOffset + 20; // Way above

        RectTransform manhattanRect = manhattanObj.GetComponent<RectTransform>();
        manhattanRect.sizeDelta = new Vector2(150, 150);

        return tile;
    }

    void HandleGameInputAndHUD()
    {
        if (!puzzleCompleted)
        {
            elapsedTime += Time.deltaTime;
        }

        if (timeText != null)
            timeText.text = $"Time: {(int)elapsedTime / 60:00}:{(int)elapsedTime % 60:00}";

        if (movesText != null)
            movesText.text = $"Moves: {moves}";

        if (isAutoSolving || isAnimatingMove)
            return;

        // Check if next button is active (puzzle completed)
        if (nextButtonSprite != null && nextButtonSprite.activeSelf)
        {
            HandleNextButtonInput();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 click = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            click.z = 0f;

            Debug.Log($"=== CLICK at {click} ===");

            if (CheckButtonClick(autoSolveSprite, click, "AUTO-SOLVE"))
            {
                Debug.Log("🚀 AUTO-SOLVE CLICKED!");
                StartCoroutine(AutoSolveSequence());
                return;
            }

            if (CheckButtonClick(toggleManhattanSpriteOFF, click, "Manhattan OFF"))
            {
                ToggleManhattanDistances();
                return;
            }

            if (CheckButtonClick(toggleManhattanSpriteON, click, "Manhattan ON"))
            {
                ToggleManhattanDistances();
                return;
            }

            CheckTileClicks(click);
        }
    }

    bool CheckButtonClick(GameObject button, Vector3 clickPos, string buttonName)
    {
        if (button != null && button.activeSelf)
        {
            SpriteRenderer sr = button.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Bounds bounds = sr.bounds;

                Vector2 clickPos2D = new Vector2(clickPos.x, clickPos.y);
                Vector2 boundsCenter2D = new Vector2(bounds.center.x, bounds.center.y);

                bool containsX = Mathf.Abs(clickPos2D.x - boundsCenter2D.x) <= bounds.extents.x;
                bool containsY = Mathf.Abs(clickPos2D.y - boundsCenter2D.y) <= bounds.extents.y;
                bool contains = containsX && containsY;

                Debug.Log($"{buttonName} - Center2D: {boundsCenter2D}, Extents: ({bounds.extents.x}, {bounds.extents.y}), Click2D: {clickPos2D}, Contains: {contains}");

                if (contains)
                {
                    Debug.Log($"✅ {buttonName} CLICKED!");
                    return true;
                }
            }
        }
        return false;
    }

    void HandleNextButtonInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 click = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            click.z = 0f;

            if (nextButtonSprite != null)
            {
                SpriteRenderer sr = nextButtonSprite.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Bounds bounds = sr.bounds;

                    Vector2 clickPos2D = new Vector2(click.x, click.y);
                    Vector2 boundsCenter2D = new Vector2(bounds.center.x, bounds.center.y);

                    bool containsX = Mathf.Abs(clickPos2D.x - boundsCenter2D.x) <= bounds.extents.x;
                    bool containsY = Mathf.Abs(clickPos2D.y - boundsCenter2D.y) <= bounds.extents.y;

                    Debug.Log($"Next button check - Click: {clickPos2D}, Center: {boundsCenter2D}, ContainsX: {containsX}, ContainsY: {containsY}");

                    if (containsX && containsY)
                    {
                        Debug.Log("✅ Next button clicked!");
                        nextButtonSprite.SetActive(false);
                        StartNewPuzzle();
                    }
                }
            }
        }
    }

    void CheckTileClicks(Vector3 clickPos)
    {
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (tiles[r, c] != null)
                {
                    SpriteRenderer sr = tiles[r, c].GetComponent<SpriteRenderer>();
                    if (sr != null && sr.bounds.Contains(clickPos))
                    {
                        Debug.Log($"Tile clicked at ({r}, {c})");
                        StartCoroutine(TryMoveTileWithAnimation(r, c, true));
                        return;
                    }
                }
            }
        }
    }

    IEnumerator AutoSolveSequence()
    {
        isAutoSolving = true;

        if (autoSolveSprite != null) autoSolveSprite.SetActive(false);
        if (toggleManhattanSpriteOFF != null) toggleManhattanSpriteOFF.SetActive(false);
        if (toggleManhattanSpriteON != null) toggleManhattanSpriteON.SetActive(false);

        Debug.Log("🧠 Computing solution...");

        List<Vector2Int> solution = FindSolution();

        if (solution == null || solution.Count == 0)
        {
            Debug.Log("Already solved!");
            isAutoSolving = false;
            if (autoSolveSprite != null) autoSolveSprite.SetActive(true);
            if (toggleManhattanSpriteOFF != null) toggleManhattanSpriteOFF.SetActive(true);
            yield break;
        }

        Debug.Log($"✅ Solution: {solution.Count} moves!");

        for (int i = 0; i < solution.Count; i++)
        {
            Vector2Int move = solution[i];
            Debug.Log($"Move {i + 1}/{solution.Count}: ({move.x}, {move.y})");

            yield return StartCoroutine(TryMoveTileWithAnimation(move.x, move.y, false));
            yield return new WaitForSeconds(autoSolveStepDelay);
        }

        isAutoSolving = false;
        Debug.Log("🎉 AUTO-SOLVE COMPLETE!");
    }

    IEnumerator TryMoveTileWithAnimation(int row, int col, bool countAsPlayerMove)
    {
        int emptyRow = emptyCell.x;
        int emptyCol = emptyCell.y;

        int distance = Mathf.Abs(emptyRow - row) + Mathf.Abs(emptyCol - col);
        if (distance != 1)
        {
            yield break;
        }

        isAnimatingMove = true;

        Vector3 emptyPos = GetGridPosition(emptyRow, emptyCol);
        yield return StartCoroutine(AnimateTileMove(tiles[row, col], emptyPos));

        tiles[emptyRow, emptyCol] = tiles[row, col];
        tiles[row, col] = null;
        emptyCell = new Vector2Int(row, col);

        moves++;

        UpdateManhattanDisplay();
        UpdateMinMoves();

        isAnimatingMove = false;

        CheckPuzzleCompletion();
    }

    IEnumerator AnimateTileMove(GameObject tile, Vector3 targetPos)
    {
        Vector3 startPos = tile.transform.position;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            tile.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        tile.transform.position = targetPos;
    }

    void CheckPuzzleCompletion()
    {
        int expected = 1;

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (r == 2 && c == 2)
                {
                    if (tiles[r, c] != null) return;
                }
                else
                {
                    if (tiles[r, c] == null) return;
                    int tileNum = GetTileNumber(tiles[r, c]);
                    if (tileNum != expected) return;
                    expected++;
                }
            }
        }

        OnPuzzleComplete();
    }

    void OnPuzzleComplete()
    {
        puzzleCompleted = true;

        Debug.Log("🏆 PUZZLE COMPLETED!");
        Debug.Log($"Time: {(int)elapsedTime / 60:00}:{(int)elapsedTime % 60:00}");
        Debug.Log($"Moves: {moves}");

        // Hide feature buttons
        if (autoSolveSprite != null) autoSolveSprite.SetActive(false);
        if (toggleManhattanSpriteOFF != null) toggleManhattanSpriteOFF.SetActive(false);
        if (toggleManhattanSpriteON != null) toggleManhattanSpriteON.SetActive(false);

        // Show ONLY next button at Z = 0.5
        if (nextButtonSprite != null)
        {
            nextButtonSprite.SetActive(true);

            // Position at Z = 0.5 (as requested)
            Vector3 pos = nextButtonSprite.transform.position;
            pos.z = 0.5f;
            nextButtonSprite.transform.position = pos;

            SpriteRenderer sr = nextButtonSprite.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 200; // Very high to be visible
            }

            Debug.Log($"✅ Next button shown at: {nextButtonSprite.transform.position}");
        }

        // Show completion stats
        if (completionTimeText != null)
        {
            completionTimeText.gameObject.SetActive(true);
            completionTimeText.text = $"Time Taken: {(int)elapsedTime / 60:00}:{(int)elapsedTime % 60:00}";
            Debug.Log($"Completion time displayed: {completionTimeText.text}");
        }

        if (completionMovesText != null)
        {
            completionMovesText.gameObject.SetActive(true);
            completionMovesText.text = $"Moves Taken: {moves}";
            Debug.Log($"Completion moves displayed: {completionMovesText.text}");
        }
    }

    Vector3 GetGridPosition(int row, int col)
    {
        float totalSpacing = tileSize + tileGap;
        float startX = -totalSpacing;
        float startY = totalSpacing;

        return gridParent.position + new Vector3(
            startX + col * totalSpacing,
            startY - row * totalSpacing,
            0
        );
    }

    void ShuffleGrid(int shuffleMoves)
    {
        for (int i = 0; i < shuffleMoves; i++)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();
            int r = emptyCell.x;
            int c = emptyCell.y;

            if (r > 0) validMoves.Add(new Vector2Int(r - 1, c));
            if (r < 2) validMoves.Add(new Vector2Int(r + 1, c));
            if (c > 0) validMoves.Add(new Vector2Int(r, c - 1));
            if (c < 2) validMoves.Add(new Vector2Int(r, c + 1));

            Vector2Int move = validMoves[Random.Range(0, validMoves.Count)];

            Vector3 emptyPos = GetGridPosition(r, c);
            tiles[move.x, move.y].transform.position = emptyPos;

            tiles[r, c] = tiles[move.x, move.y];
            tiles[move.x, move.y] = null;
            emptyCell = move;
        }
    }

    void ToggleManhattanDistances()
    {
        showManhattanDistances = !showManhattanDistances;

        Debug.Log($"Manhattan: {(showManhattanDistances ? "ON" : "OFF")}");

        if (showManhattanDistances)
        {
            if (toggleManhattanSpriteOFF != null) toggleManhattanSpriteOFF.SetActive(false);
            if (toggleManhattanSpriteON != null)
            {
                toggleManhattanSpriteON.SetActive(true);
                Vector3 pos = toggleManhattanSpriteON.transform.position;
                pos.z = -5f;
                toggleManhattanSpriteON.transform.position = pos;
            }
        }
        else
        {
            if (toggleManhattanSpriteOFF != null)
            {
                toggleManhattanSpriteOFF.SetActive(true);
                Vector3 pos = toggleManhattanSpriteOFF.transform.position;
                pos.z = -5f;
                toggleManhattanSpriteOFF.transform.position = pos;
            }
            if (toggleManhattanSpriteON != null) toggleManhattanSpriteON.SetActive(false);
        }

        UpdateManhattanDisplay();
    }

    void UpdateManhattanDisplay()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (tiles[row, col] != null)
                {
                    int tileNumber = GetTileNumber(tiles[row, col]);
                    if (tileNumber == -1) continue;

                    int targetRow = (tileNumber - 1) / 3;
                    int targetCol = (tileNumber - 1) % 3;
                    int manhattanDist = Mathf.Abs(row - targetRow) + Mathf.Abs(col - targetCol);

                    Transform manhattanTransform = tiles[row, col].transform.Find("Manhattan");
                    if (manhattanTransform != null)
                    {
                        TextMeshPro manhattanText = manhattanTransform.GetComponent<TextMeshPro>();
                        if (manhattanText != null)
                        {
                            manhattanText.text = showManhattanDistances ? manhattanDist.ToString() : "";
                        }
                    }
                }
            }
        }
    }

    int GetTileNumber(GameObject tile)
    {
        Transform numberTransform = tile.transform.Find("Number");
        if (numberTransform != null)
        {
            TextMeshPro tmp = numberTransform.GetComponent<TextMeshPro>();
            if (tmp != null && int.TryParse(tmp.text, out int num))
            {
                return num;
            }
        }
        return -1;
    }

    void UpdateMinMoves()
    {
        int currentManhattan = CalculateTotalManhattan();

        if (minMovesStartText != null)
            minMovesStartText.text = $"Min. Moves at Start: {minMovesAtStart}";

        if (minMovesCurrentText != null)
            minMovesCurrentText.text = $"Min. Moves at Current Stage: {currentManhattan}";
    }

    int CalculateTotalManhattan()
    {
        int total = 0;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (tiles[row, col] != null)
                {
                    int tileNumber = GetTileNumber(tiles[row, col]);
                    if (tileNumber == -1) continue;

                    int targetRow = (tileNumber - 1) / 3;
                    int targetCol = (tileNumber - 1) % 3;
                    total += Mathf.Abs(row - targetRow) + Mathf.Abs(col - targetCol);
                }
            }
        }

        return total;
    }

    List<Vector2Int> FindSolution()
    {
        PuzzleState startState = new PuzzleState(GetCurrentState(), emptyCell, null, 0);

        if (startState.IsSolved())
        {
            return new List<Vector2Int>();
        }

        PriorityQueue<PuzzleState> openSet = new PriorityQueue<PuzzleState>();
        HashSet<string> visited = new HashSet<string>();

        openSet.Enqueue(startState, startState.GetPriority());
        visited.Add(startState.GetHash());

        int iterations = 0;
        int maxIterations = 100000;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            PuzzleState current = openSet.Dequeue();

            if (current.IsSolved())
            {
                Debug.Log($"Solution found in {iterations} iterations");
                return current.GetPath();
            }

            foreach (PuzzleState neighbor in current.GetNeighbors())
            {
                string hash = neighbor.GetHash();
                if (!visited.Contains(hash))
                {
                    visited.Add(hash);
                    openSet.Enqueue(neighbor, neighbor.GetPriority());
                }
            }
        }

        return null;
    }

    int[,] GetCurrentState()
    {
        int[,] state = new int[3, 3];

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                if (tiles[r, c] == null)
                    state[r, c] = 0;
                else
                    state[r, c] = GetTileNumber(tiles[r, c]);
            }
        }

        return state;
    }

    class PuzzleState
    {
        public int[,] board;
        public Vector2Int emptyPos;
        public PuzzleState parent;
        public int moves;
        public Vector2Int lastMove;

        public PuzzleState(int[,] board, Vector2Int emptyPos, PuzzleState parent, int moves)
        {
            this.board = (int[,])board.Clone();
            this.emptyPos = emptyPos;
            this.parent = parent;
            this.moves = moves;
        }

        public bool IsSolved()
        {
            int expected = 1;

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (r == 2 && c == 2)
                        return board[r, c] == 0;

                    if (board[r, c] != expected)
                        return false;

                    expected++;
                }
            }

            return true;
        }

        public int GetManhattan()
        {
            int total = 0;

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int val = board[r, c];

                    if (val != 0)
                    {
                        int targetR = (val - 1) / 3;
                        int targetC = (val - 1) % 3;
                        total += Mathf.Abs(r - targetR) + Mathf.Abs(c - targetC);
                    }
                }
            }

            return total;
        }

        public int GetPriority()
        {
            return moves + GetManhattan();
        }

        public string GetHash()
        {
            string hash = "";
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    hash += board[r, c];
            return hash;
        }

        public List<PuzzleState> GetNeighbors()
        {
            List<PuzzleState> neighbors = new List<PuzzleState>();
            int r = emptyPos.x;
            int c = emptyPos.y;

            if (r > 0) neighbors.Add(Swap(r - 1, c));
            if (r < 2) neighbors.Add(Swap(r + 1, c));
            if (c > 0) neighbors.Add(Swap(r, c - 1));
            if (c < 2) neighbors.Add(Swap(r, c + 1));

            return neighbors;
        }

        PuzzleState Swap(int tileR, int tileC)
        {
            int[,] newBoard = (int[,])board.Clone();
            newBoard[emptyPos.x, emptyPos.y] = newBoard[tileR, tileC];
            newBoard[tileR, tileC] = 0;

            PuzzleState newState = new PuzzleState(newBoard, new Vector2Int(tileR, tileC), this, moves + 1);
            newState.lastMove = new Vector2Int(tileR, tileC);
            return newState;
        }

        public List<Vector2Int> GetPath()
        {
            List<Vector2Int> path = new List<Vector2Int>();
            PuzzleState current = this;

            while (current.parent != null)
            {
                path.Add(current.lastMove);
                current = current.parent;
            }

            path.Reverse();
            return path;
        }
    }

    class PriorityQueue<T>
    {
        private List<(T item, int priority)> elements = new List<(T, int)>();

        public int Count => elements.Count;

        public void Enqueue(T item, int priority)
        {
            elements.Add((item, priority));
            elements = elements.OrderBy(x => x.priority).ToList();
        }

        public T Dequeue()
        {
            var item = elements[0].item;
            elements.RemoveAt(0);
            return item;
        }
    }
}
