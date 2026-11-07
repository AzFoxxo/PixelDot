using PixelDot;

namespace Applications;

[RunnableApplication("Tetris", "1.0.0", "PixelDot", "Tetris game")]
public class Tetris : Application
{
    private const int BoardWidth = 10;
    private const int BoardHeight = 20;

    private enum GameState
    {
        Playing,
        LineClear,
        Paused,
        GameOver
    }

    private GameState state = GameState.Playing;

    // Board grid: 0 = empty, 1..7 = tetromino type / color
    private readonly int[,] board = new int[BoardWidth, BoardHeight];

    // Tetromino piece definitions: 7 pieces, 4 rotations, 4 blocks (x, y)
    // Pieces: 1: I, 2: J, 3: L, 4: O, 5: S, 6: T, 7: Z
    private static readonly (int x, int y)[][][] Pieces =
    [
        // Dummy 0-index
        [],
        // 1: I piece
        [
            [(0, 1), (1, 1), (2, 1), (3, 1)],
            [(2, 0), (2, 1), (2, 2), (2, 3)],
            [(0, 2), (1, 2), (2, 2), (3, 2)],
            [(1, 0), (1, 1), (1, 2), (1, 3)]
        ],
        // 2: J piece
        [
            [(0, 0), (0, 1), (1, 1), (2, 1)],
            [(1, 0), (2, 0), (1, 1), (1, 2)],
            [(0, 1), (1, 1), (2, 1), (2, 2)],
            [(1, 0), (1, 1), (0, 2), (1, 2)]
        ],
        // 3: L piece
        [
            [(2, 0), (0, 1), (1, 1), (2, 1)],
            [(1, 0), (1, 1), (1, 2), (2, 2)],
            [(0, 1), (1, 1), (2, 1), (0, 2)],
            [(0, 0), (1, 0), (1, 1), (1, 2)]
        ],
        // 4: O piece
        [
            [(1, 0), (2, 0), (1, 1), (2, 1)],
            [(1, 0), (2, 0), (1, 1), (2, 1)],
            [(1, 0), (2, 0), (1, 1), (2, 1)],
            [(1, 0), (2, 0), (1, 1), (2, 1)]
        ],
        // 5: S piece
        [
            [(1, 0), (2, 0), (0, 1), (1, 1)],
            [(1, 0), (1, 1), (2, 1), (2, 2)],
            [(1, 1), (2, 1), (0, 2), (1, 2)],
            [(0, 0), (0, 1), (1, 1), (1, 2)]
        ],
        // 6: T piece
        [
            [(1, 0), (0, 1), (1, 1), (2, 1)],
            [(1, 0), (1, 1), (2, 1), (1, 2)],
            [(0, 1), (1, 1), (2, 1), (1, 2)],
            [(1, 0), (0, 1), (1, 1), (1, 2)]
        ],
        // 7: Z piece
        [
            [(0, 0), (1, 0), (1, 1), (2, 1)],
            [(2, 0), (1, 1), (2, 1), (1, 2)],
            [(0, 1), (1, 1), (1, 2), (2, 2)],
            [(1, 0), (0, 1), (1, 1), (0, 2)]
        ]
    ];

    // Colors for each piece type: [body, highlight, shadow]
    private static readonly (Color Body, Color Highlight, Color Shadow)[] PieceColours =
    [
        (Color.Black, Color.Black, Color.Black), // 0: None
        (Palette.GetColour(8),  Palette.GetColour(48), Palette.GetColour(7)),  // 1: I - Cyan
        (Palette.GetColour(7),  Palette.GetColour(8),  Palette.GetColour(6)),  // 2: J - Blue
        (Palette.GetColour(28), Palette.GetColour(29), Palette.GetColour(27)), // 3: L - Orange
        (Palette.GetColour(75), Palette.GetColour(76), Palette.GetColour(74)), // 4: O - Yellow
        (Palette.GetColour(83), Palette.GetColour(84), Palette.GetColour(82)), // 5: S - Green
        (Palette.GetColour(16), Palette.GetColour(17), Palette.GetColour(15)), // 6: T - Purple / Magenta
        (Palette.GetColour(23), Palette.GetColour(25), Palette.GetColour(22))  // 7: Z - Red
    ];

    // Current piece state
    private int currentPiece;
    private int currentRotation;
    private int currentX;
    private int currentY;

    // Hold & Next
    private int holdPiece;
    private bool canHold = true;
    private readonly Queue<int> bag = new();
    private readonly Queue<int> nextPieces = new();

    // Random generator
    private readonly Random random = new();

    // Score & progression
    private int score;
    private int highScore;
    private int lines;
    private int level = 1;

    // Gravity & timing
    private int dropTimer;
    private int lockDelayTimer;
    private const int MaxLockDelay = 30; // ~0.5s at 60 FPS
    private bool isTouchingFloor;

    // Line clear animation
    private readonly List<int> clearingLines = [];
    private int clearAnimTimer;
    private const int ClearAnimDuration = 12;

    // Audio
    private Sample? themeMusic;
    private bool isMuted;
    private bool wasMute;

    // Input state & DAS (Delayed Auto Shift)
    private bool wasLeft, wasRight, wasDown, wasRotCW, wasRotCCW, wasHardDrop, wasHold, wasPause, wasRestart;
    private int dasTimerLeft;
    private int dasTimerRight;
    private const int DasInitialDelay = 14;
    private const int DasRepeatRate = 3;

    // Rendering layout cached properties
    private int blockSize = 3;
    private int boardPixelX;
    private int boardPixelY;
    private int boardPixelWidth;
    private int boardPixelHeight;

    public override void OnInit()
    {

        // Load the palette
        // Palette.LoadFromFile("/home/az/Downloads/slso8.hex");
        Logger.LogInfo("Tetris Initialised");
        CalculateLayout();
        LoadMusic();
        ResetGame();
    }

    public override void OnEnd()
    {
        Audio.Stop(1);
    }

    private void LoadMusic()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "src", "Applications", "Tetris game", "tetris.ogg"),
            "src/Applications/Tetris/tetris.wav",
            "PixelDot/src/Applications/Tetris/tetris.wav",
            Path.Combine(AppContext.BaseDirectory, "tetris.wav"),
            "tetris.wav"
        ];

        var foundPath = candidates.FirstOrDefault(File.Exists);

        if (foundPath is not null)
        {
            try
            {
                // themeMusic = Audio.LoadSample(foundPath);
                if (themeMusic is not null)
                {
                    Audio.Play(1, themeMusic, loop: true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Could not start audio playback: {ex.Message}");
            }
        }
    }

    private void CalculateLayout()
    {
        // Dynamically compute block size to fit any resolution
        blockSize = Math.Max(2, (Core.ScreenHeight - 12) / BoardHeight);

        // Ensure board leaves at least 32 pixels on each side for HUD
        while (blockSize > 2 && (Core.ScreenWidth - (BoardWidth * blockSize)) < 64)
        {
            blockSize--;
        }

        boardPixelWidth = BoardWidth * blockSize;
        boardPixelHeight = BoardHeight * blockSize;
        boardPixelX = (Core.ScreenWidth - boardPixelWidth) / 2;
        boardPixelY = (Core.ScreenHeight - boardPixelHeight) / 2;
    }

    private void ResetGame()
    {
        Array.Clear(board, 0, board.Length);
        score = 0;
        lines = 0;
        level = 1;
        holdPiece = 0;
        canHold = true;
        bag.Clear();
        nextPieces.Clear();
        clearingLines.Clear();

        FillNextQueue();
        SpawnPiece();
        state = GameState.Playing;

        if (!isMuted && themeMusic is not null)
        {
            if (!Audio.IsPlaying(1))
            {
                Audio.Play(1, themeMusic, loop: true);
            }
        }
    }

    private void RefillBag()
    {
        var pieces = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
        for (var i = pieces.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (pieces[i], pieces[j]) = (pieces[j], pieces[i]);
        }

        foreach (var p in pieces)
        {
            bag.Enqueue(p);
        }
    }

    private int PullFromBag()
    {
        if (bag.Count == 0)
        {
            RefillBag();
        }

        return bag.Dequeue();
    }

    private void FillNextQueue()
    {
        while (nextPieces.Count < 3)
        {
            nextPieces.Enqueue(PullFromBag());
        }
    }

    private void SpawnPiece()
    {
        FillNextQueue();
        currentPiece = nextPieces.Dequeue();
        currentRotation = 0;
        currentX = 3; // Center horizontally
        currentY = 0;
        canHold = true;
        lockDelayTimer = 0;
        isTouchingFloor = false;

        // Check if spawn location is blocked -> Game Over
        if (!IsValidPosition(currentPiece, currentRotation, currentX, currentY))
        {
            state = GameState.GameOver;
            Audio.Pause(1);
            if (score > highScore)
            {
                highScore = score;
            }
            Logger.LogInfo($"Game Over! Final Score: {score}");
        }
    }

    public override void OnUpdate()
    {
        HandleInput();

        switch (state)
        {
            case GameState.Playing:
                UpdatePlaying();
                break;
            case GameState.LineClear:
                UpdateLineClear();
                break;
            case GameState.Paused:
            case GameState.GameOver:
                break;
        }

        DrawGame();
    }

    private void HandleInput()
    {
        var nowLeft = Input.IsPressed(Keys.ArrowLeft);
        var nowRight = Input.IsPressed(Keys.ArrowRight);
        var nowDown = Input.IsPressed(Keys.ArrowDown);
        var nowRotCW = Input.IsPressed(Keys.ArrowUp) || Input.IsPressed(Keys.B) ||
                          Input.IsPressed(Keys.X);
        var nowRotCCW = Input.IsPressed(Keys.A) || Input.IsPressed(Keys.Z) ||
                          Input.IsPressed(Keys.Q);
        var nowHardDrop = Input.IsPressed(Keys.Space) || Input.IsPressed(Keys.Jump);
        var nowHold = Input.IsPressed(Keys.C) || Input.IsPressed(Keys.LShift) ||
                          Input.IsPressed(Keys.RShift);
        var nowPause = Input.IsPressed(Keys.P) || Input.IsPressed(Keys.Escape);
        var nowRestart = Input.IsPressed(Keys.Enter) || Input.IsPressed(Keys.Start) ||
                          Input.IsPressed(Keys.R);
        var nowMute = Input.IsPressed(Keys.M);

        // Pause toggle
        if (nowPause && !wasPause)
        {
            if (state == GameState.Playing)
            {
                state = GameState.Paused;
                Audio.Pause(1);
            }
            else if (state == GameState.Paused)
            {
                state = GameState.Playing;
                if (!isMuted)
                {
                    Audio.Resume(1);
                }
            }
        }

        // Mute toggle
        if (nowMute && !wasMute)
        {
            isMuted = !isMuted;
            if (isMuted)
            {
                Audio.Pause(1);
            }
            else
            {
                if (state != GameState.Paused)
                {
                    if (Audio.IsPlaying(1))
                    {
                        // Already playing
                    }
                    else if (themeMusic is not null)
                    {
                        Audio.Play(1, themeMusic, loop: true);
                    }
                }
            }
        }

        // Restart on Game Over
        if (nowRestart && !wasRestart && state == GameState.GameOver)
        {
            ResetGame();
        }

        if (state == GameState.Playing)
        {
            // Left movement with DAS
            if (nowLeft)
            {
                if (!wasLeft)
                {
                    TryMove(-1, 0);
                    dasTimerLeft = DasInitialDelay;
                }
                else
                {
                    dasTimerLeft--;
                    if (dasTimerLeft <= 0)
                    {
                        TryMove(-1, 0);
                        dasTimerLeft = DasRepeatRate;
                    }
                }
            }

            // Right movement with DAS
            if (nowRight)
            {
                if (!wasRight)
                {
                    TryMove(1, 0);
                    dasTimerRight = DasInitialDelay;
                }
                else
                {
                    dasTimerRight--;
                    if (dasTimerRight <= 0)
                    {
                        TryMove(1, 0);
                        dasTimerRight = DasRepeatRate;
                    }
                }
            }

            // Soft Drop (down)
            if (nowDown)
            {
                if (TryMove(0, 1))
                {
                    score += 1; // 1 point per soft drop cell
                    dropTimer = 0;
                }
            }

            // Rotate Clockwise
            if (nowRotCW && !wasRotCW)
            {
                TryRotate(1);
            }

            // Rotate Counter-Clockwise
            if (nowRotCCW && !wasRotCCW)
            {
                TryRotate(-1);
            }

            // Hard Drop
            if (nowHardDrop && !wasHardDrop)
            {
                PerformHardDrop();
            }

            // Hold piece
            if (nowHold && !wasHold)
            {
                PerformHold();
            }
        }

        wasLeft = nowLeft;
        wasRight = nowRight;
        wasDown = nowDown;
        wasRotCW = nowRotCW;
        wasRotCCW = nowRotCCW;
        wasHardDrop = nowHardDrop;
        wasHold = nowHold;
        wasPause = nowPause;
        wasRestart = nowRestart;
        wasMute = nowMute;
    }

    private void UpdatePlaying()
    {
        // Gravity speed formula based on level (frames per drop)
        var dropInterval = Math.Max(2, 48 - ((level - 1) * 4));
        dropTimer++;

        var onGround = !IsValidPosition(currentPiece, currentRotation, currentX, currentY + 1);

        if (onGround)
        {
            if (!isTouchingFloor)
            {
                isTouchingFloor = true;
                lockDelayTimer = 0;
            }

            lockDelayTimer++;
            if (lockDelayTimer >= MaxLockDelay)
            {
                LockPiece();
            }
        }
        else
        {
            isTouchingFloor = false;
            if (dropTimer >= dropInterval)
            {
                dropTimer = 0;
                currentY++;
            }
        }
    }

    private bool TryMove(int dx, int dy)
    {
        if (IsValidPosition(currentPiece, currentRotation, currentX + dx, currentY + dy))
        {
            currentX += dx;
            currentY += dy;

            // Reset lock delay on successful movement if on ground
            if (isTouchingFloor)
            {
                lockDelayTimer = 0;
            }

            return true;
        }

        return false;
    }

    private void TryRotate(int direction)
    {
        var newRot = (currentRotation + direction + 4) % 4;

        // Standard SRS-style wall kick offsets
        int[] kickX = [0, -1, 1, -2, 2];
        int[] kickY = [0, 0, 0, -1, -1];

        for (var i = 0; i < kickX.Length; i++)
        {
            if (IsValidPosition(currentPiece, newRot, currentX + kickX[i], currentY + kickY[i]))
            {
                currentRotation = newRot;
                currentX += kickX[i];
                currentY += kickY[i];

                if (isTouchingFloor)
                {
                    lockDelayTimer = 0;
                }

                return;
            }
        }
    }

    private void PerformHardDrop()
    {
        var dropDistance = 0;
        while (IsValidPosition(currentPiece, currentRotation, currentX, currentY + 1))
        {
            currentY++;
            dropDistance++;
        }

        score += dropDistance * 2; // 2 points per hard drop cell
        LockPiece();
    }

    private void PerformHold()
    {
        if (!canHold)
        {
            return;
        }

        canHold = false;
        var temp = holdPiece;
        holdPiece = currentPiece;

        if (temp == 0)
        {
            SpawnPiece();
        }
        else
        {
            currentPiece = temp;
            currentRotation = 0;
            currentX = 3;
            currentY = 0;
            lockDelayTimer = 0;
            isTouchingFloor = false;
        }
    }

    private void LockPiece()
    {
        var blocks = Pieces[currentPiece][currentRotation];
        foreach (var (bx, by) in blocks)
        {
            var bxPos = currentX + bx;
            var byPos = currentY + by;
            if (bxPos >= 0 && bxPos < BoardWidth && byPos >= 0 && byPos < BoardHeight)
            {
                board[bxPos, byPos] = currentPiece;
            }
        }

        // Check for completed lines
        clearingLines.Clear();
        for (var y = 0; y < BoardHeight; y++)
        {
            var isFull = true;
            for (var x = 0; x < BoardWidth; x++)
            {
                if (board[x, y] == 0)
                {
                    isFull = false;
                    break;
                }
            }

            if (isFull)
            {
                clearingLines.Add(y);
            }
        }

        if (clearingLines.Count > 0)
        {
            state = GameState.LineClear;
            clearAnimTimer = ClearAnimDuration;
        }
        else
        {
            SpawnPiece();
        }
    }

    private void UpdateLineClear()
    {
        clearAnimTimer--;
        if (clearAnimTimer <= 0)
        {
            // Bottom-up row compaction: safely collapses remaining rows downward
            var writeY = BoardHeight - 1;
            for (var y = BoardHeight - 1; y >= 0; y--)
            {
                if (!clearingLines.Contains(y))
                {
                    if (writeY != y)
                    {
                        for (var x = 0; x < BoardWidth; x++)
                        {
                            board[x, writeY] = board[x, y];
                        }
                    }
                    writeY--;
                }
            }

            for (var y = writeY; y >= 0; y--)
            {
                for (var x = 0; x < BoardWidth; x++)
                {
                    board[x, y] = 0;
                }
            }

            // Award points based on cleared lines
            var lineScores = new[] { 0, 100, 300, 500, 800 };
            var count = Math.Clamp(clearingLines.Count, 1, 4);
            score += lineScores[count] * level;
            lines += count;
            level = (lines / 10) + 1;

            if (score > highScore)
            {
                highScore = score;
            }

            clearingLines.Clear();
            state = GameState.Playing;
            SpawnPiece();
        }
    }

    private bool IsValidPosition(int piece, int rotation, int posX, int posY)
    {
        var blocks = Pieces[piece][rotation];
        foreach (var (bx, by) in blocks)
        {
            var x = posX + bx;
            var y = posY + by;

            if (x < 0 || x >= BoardWidth || y >= BoardHeight)
            {
                return false;
            }

            if (y >= 0 && board[x, y] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private int GetGhostY()
    {
        var ghostY = currentY;
        while (IsValidPosition(currentPiece, currentRotation, currentX, ghostY + 1))
        {
            ghostY++;
        }

        return ghostY;
    }

    #region Rendering

    private void DrawGame()
    {
        // 1. Draw outer background with subtle grid or retro framing
        DrawBackground();

        // 2. Draw playfield border & background
        DrawPlayfield();

        // 3. Draw placed blocks on the board
        DrawPlacedBlocks();

        // 4. Draw active & ghost piece when playing
        if (state == GameState.Playing)
        {
            DrawGhostPiece();
            DrawCurrentPiece();
        }

        // 5. Draw line clear flash effect
        if (state == GameState.LineClear)
        {
            DrawLineClearFlash();
        }

        // 6. Draw side panels (HOLD, NEXT, SCORE, LEVEL, LINES)
        DrawSidePanels();

        // 7. Draw bottom controls hint
        DrawControlsHint();

        // 8. Draw overlays for Game Over and Pause
        if (state == GameState.GameOver)
        {
            DrawGameOverOverlay();
        }
        else if (state == GameState.Paused)
        {
            DrawPauseOverlay();
        }
    }

    private void DrawControlsHint()
    {
        if (Core.ScreenHeight - (boardPixelY + boardPixelHeight) >= 8)
        {
            var hint = isMuted ? "MOVE:ARROWS  DROP:SPC  [MUTED]" : "MOVE:ARROWS  DROP:SPC  M:MUTE";
            var hx = (Core.ScreenWidth - GetPixelTextWidth(hint)) / 2;
            var hy = Core.ScreenHeight - 7;
            DrawPixelText(hint, Math.Max(2, hx), hy, Palette.GetColour(3));
        }
    }

    private void DrawBackground()
    {
        // Background base
        Core.DrawRectangle(0, 0, Core.ScreenWidth, Core.ScreenHeight, Palette.GetColour(0), 1, true);

        // Header retro accent line
        Core.DrawRectangle(0, 0, Core.ScreenWidth, 2, Palette.GetColour(2), 1, true);
        Core.DrawRectangle(0, Core.ScreenHeight - 2, Core.ScreenWidth, 2, Palette.GetColour(2), 1, true);
    }

    private void DrawPlayfield()
    {
        // Playfield interior background
        Core.DrawRectangle(boardPixelX, boardPixelY, boardPixelWidth, boardPixelHeight, Palette.GetColour(1), 1, true);

        // Double border frame
        var borderOuter = Palette.GetColour(4);
        var borderInner = Palette.GetColour(2);

        // Outer border
        Core.DrawRectangle(boardPixelX - 2, boardPixelY - 2, boardPixelWidth + 4, boardPixelHeight + 4, borderOuter, 1, false);
        // Inner border
        Core.DrawRectangle(boardPixelX - 1, boardPixelY - 1, boardPixelWidth + 2, boardPixelHeight + 2, borderInner, 1, false);

        // Subtle playfield grid dots/lines
        for (var x = 1; x < BoardWidth; x++)
        {
            var px = boardPixelX + (x * blockSize);
            for (var y = 1; y < BoardHeight; y++)
            {
                var py = boardPixelY + (y * blockSize);
                Core.DrawPixel(px, py, Palette.GetColour(0), 1, 1);
            }
        }
    }

    private void DrawPlacedBlocks()
    {
        for (var x = 0; x < BoardWidth; x++)
        {
            for (var y = 0; y < BoardHeight; y++)
            {
                var pieceType = board[x, y];
                if (pieceType > 0)
                {
                    DrawBlock(boardPixelX + (x * blockSize), boardPixelY + (y * blockSize), pieceType);
                }
            }
        }
    }

    private void DrawCurrentPiece()
    {
        var blocks = Pieces[currentPiece][currentRotation];
        foreach (var (bx, by) in blocks)
        {
            var x = currentX + bx;
            var y = currentY + by;
            if (y >= 0)
            {
                DrawBlock(boardPixelX + (x * blockSize), boardPixelY + (y * blockSize), currentPiece);
            }
        }
    }

    private void DrawGhostPiece()
    {
        var ghostY = GetGhostY();
        if (ghostY == currentY)
        {
            return; // Don't draw if overlapping active piece
        }

        var blocks = Pieces[currentPiece][currentRotation];
        var ghostColour = Palette.GetColour(2);

        foreach (var (bx, by) in blocks)
        {
            var x = currentX + bx;
            var y = ghostY + by;
            if (y >= 0)
            {
                var px = boardPixelX + (x * blockSize);
                var py = boardPixelY + (y * blockSize);
                Core.DrawRectangle(px, py, blockSize, blockSize, ghostColour, 1, false);
            }
        }
    }

    private void DrawLineClearFlash()
    {
        var flashColour = (clearAnimTimer % 4 < 2) ? Palette.GetColour(5) : Palette.GetColour(76);
        foreach (var line in clearingLines)
        {
            var py = boardPixelY + (line * blockSize);
            Core.DrawRectangle(boardPixelX, py, boardPixelWidth, blockSize, flashColour, 1, true);
        }
    }

    private void DrawBlock(int px, int py, int pieceType)
    {
        var (body, highlight, shadow) = PieceColours[pieceType];

        // Draw solid block base
        Core.DrawPixel(px, py, body, blockSize, blockSize);

        if (blockSize >= 3)
        {
            // 3D Bevel highlight: top and left edge
            Core.DrawPixel(px, py, highlight, blockSize - 1, 1);
            Core.DrawPixel(px, py, highlight, 1, blockSize - 1);

            // 3D Bevel shadow: bottom and right edge
            Core.DrawPixel(px, py + blockSize - 1, shadow, blockSize, 1);
            Core.DrawPixel(px + blockSize - 1, py, shadow, 1, blockSize);
        }
    }

    private void DrawSidePanels()
    {
        // ------------------ LEFT SIDE PANEL ------------------
        var leftPanelCenterX = boardPixelX / 2;

        // Title "TETRIS"
        DrawPixelText("TETRIS", Math.Max(4, leftPanelCenterX - (GetPixelTextWidth("TETRIS") / 2)), 6, Palette.GetColour(75));

        // HOLD Box
        var holdBoxSize = blockSize * 4;
        var holdBoxX = Math.Max(4, leftPanelCenterX - (holdBoxSize / 2));
        var holdBoxY = 16;

        DrawPixelText("HOLD", Math.Max(4, leftPanelCenterX - (GetPixelTextWidth("HOLD") / 2)), holdBoxY, Palette.GetColour(4));
        DrawBox(holdBoxX, holdBoxY + 8, holdBoxSize, holdBoxSize);
        if (holdPiece > 0)
        {
            DrawCenteredPiece(holdPiece, holdBoxX, holdBoxY + 8, holdBoxSize, holdBoxSize, canHold ? 1f : 0.5f);
        }

        // STATS: LEVEL & LINES
        var statsY = holdBoxY + holdBoxSize + 14;
        DrawPixelText("LVL", Math.Max(4, leftPanelCenterX - (GetPixelTextWidth("LVL") / 2)), statsY, Palette.GetColour(3));
        DrawPixelText(level.ToString(), Math.Max(4, leftPanelCenterX - (GetPixelTextWidth(level.ToString()) / 2)), statsY + 7, Palette.GetColour(5));

        DrawPixelText("LINES", Math.Max(4, leftPanelCenterX - (GetPixelTextWidth("LINES") / 2)), statsY + 16, Palette.GetColour(3));
        DrawPixelText(lines.ToString(), Math.Max(4, leftPanelCenterX - (GetPixelTextWidth(lines.ToString()) / 2)), statsY + 23, Palette.GetColour(5));

        // ------------------ RIGHT SIDE PANEL ------------------
        var rightPanelCenterX = boardPixelX + boardPixelWidth + ((Core.ScreenWidth - (boardPixelX + boardPixelWidth)) / 2);

        // NEXT Box
        var nextBoxSize = blockSize * 4;
        var nextBoxX = rightPanelCenterX - (nextBoxSize / 2);
        var nextBoxY = 16;

        DrawPixelText("NEXT", rightPanelCenterX - (GetPixelTextWidth("NEXT") / 2), nextBoxY, Palette.GetColour(4));
        DrawBox(nextBoxX, nextBoxY + 8, nextBoxSize, nextBoxSize);

        if (nextPieces.Count > 0)
        {
            DrawCenteredPiece(nextPieces.Peek(), nextBoxX, nextBoxY + 8, nextBoxSize, nextBoxSize);
        }

        // SCORE & BEST
        var scoreY = nextBoxY + nextBoxSize + 14;
        DrawPixelText("SCORE", rightPanelCenterX - (GetPixelTextWidth("SCORE") / 2), scoreY, Palette.GetColour(3));
        DrawPixelText(score.ToString(), rightPanelCenterX - (GetPixelTextWidth(score.ToString()) / 2), scoreY + 7, Palette.GetColour(5));

        DrawPixelText("BEST", rightPanelCenterX - (GetPixelTextWidth("BEST") / 2), scoreY + 16, Palette.GetColour(3));
        DrawPixelText(highScore.ToString(), rightPanelCenterX - (GetPixelTextWidth(highScore.ToString()) / 2), scoreY + 23, Palette.GetColour(5));
    }

    private void DrawBox(int x, int y, int width, int height)
    {
        Core.DrawRectangle(x, y, width, height, Palette.GetColour(1), 1, true);
        Core.DrawRectangle(x, y, width, height, Palette.GetColour(2), 1, false);
    }

    private void DrawCenteredPiece(int pieceType, int boxX, int boxY, int boxW, int boxH, float brightness = 1f)
    {
        var blocks = Pieces[pieceType][0];
        var minX = int.MaxValue;
        var maxX = int.MinValue;
        var minY = int.MaxValue;
        var maxY = int.MinValue;

        foreach (var (bx, by) in blocks)
        {
            if (bx < minX) minX = bx;
            if (bx > maxX) maxX = bx;
            if (by < minY) minY = by;
            if (by > maxY) maxY = by;
        }

        var pieceW = (maxX - minX + 1) * blockSize;
        var pieceH = (maxY - minY + 1) * blockSize;
        var startX = boxX + ((boxW - pieceW) / 2) - (minX * blockSize);
        var startY = boxY + ((boxH - pieceH) / 2) - (minY * blockSize);

        foreach (var (bx, by) in blocks)
        {
            var px = startX + (bx * blockSize);
            var py = startY + (by * blockSize);

            if (brightness < 0.9f)
            {
                // Dimmed for disabled hold
                Core.DrawRectangle(px, py, blockSize, blockSize, Palette.GetColour(2), 1, true);
            }
            else
            {
                DrawBlock(px, py, pieceType);
            }
        }
    }

    private void DrawGameOverOverlay()
    {
        var overlayW = Math.Min(96, Core.ScreenWidth - 10);
        var overlayH = 46;
        var ox = (Core.ScreenWidth - overlayW) / 2;
        var oy = (Core.ScreenHeight - overlayH) / 2;

        // Dark banner with bright border
        Core.DrawRectangle(ox, oy, overlayW, overlayH, Palette.GetColour(0), 1, true);
        Core.DrawRectangle(ox, oy, overlayW, overlayH, Palette.GetColour(23), 1, false);
        Core.DrawRectangle(ox + 1, oy + 1, overlayW - 2, overlayH - 2, Palette.GetColour(28), 1, false);

        DrawPixelText("GAME OVER", ox + ((overlayW - GetPixelTextWidth("GAME OVER")) / 2), oy + 6, Palette.GetColour(23));

        var scoreStr = $"SCORE: {score}";
        DrawPixelText(scoreStr, ox + ((overlayW - GetPixelTextWidth(scoreStr)) / 2), oy + 18, Palette.GetColour(5));

        DrawPixelText("PRESS ENTER", ox + ((overlayW - GetPixelTextWidth("PRESS ENTER")) / 2), oy + 28, Palette.GetColour(75));
        DrawPixelText("TO RETRY", ox + ((overlayW - GetPixelTextWidth("TO RETRY")) / 2), oy + 36, Palette.GetColour(4));
    }

    private void DrawPauseOverlay()
    {
        var overlayW = 60;
        var overlayH = 24;
        var ox = (Core.ScreenWidth - overlayW) / 2;
        var oy = (Core.ScreenHeight - overlayH) / 2;

        Core.DrawRectangle(ox, oy, overlayW, overlayH, Palette.GetColour(0), 1, true);
        Core.DrawRectangle(ox, oy, overlayW, overlayH, Palette.GetColour(8), 1, false);

        DrawPixelText("PAUSED", ox + ((overlayW - GetPixelTextWidth("PAUSED")) / 2), oy + 5, Palette.GetColour(5));
        DrawPixelText("[P] RESUME", ox + ((overlayW - GetPixelTextWidth("[P] RESUME")) / 2), oy + 14, Palette.GetColour(4));
    }

    #endregion

    #region Retro 3x5 Pixel Font

    private static readonly Dictionary<char, byte[]> Font3x5 = new()
    {
        { 'A', [0b111, 0b101, 0b111, 0b101, 0b101] },
        { 'B', [0b110, 0b101, 0b110, 0b101, 0b110] },
        { 'C', [0b111, 0b100, 0b100, 0b100, 0b111] },
        { 'D', [0b110, 0b101, 0b101, 0b101, 0b110] },
        { 'E', [0b111, 0b100, 0b110, 0b100, 0b111] },
        { 'F', [0b111, 0b100, 0b110, 0b100, 0b100] },
        { 'G', [0b111, 0b100, 0b101, 0b101, 0b111] },
        { 'H', [0b101, 0b101, 0b111, 0b101, 0b101] },
        { 'I', [0b111, 0b010, 0b010, 0b010, 0b111] },
        { 'J', [0b001, 0b001, 0b001, 0b101, 0b111] },
        { 'K', [0b101, 0b110, 0b100, 0b110, 0b101] },
        { 'L', [0b100, 0b100, 0b100, 0b100, 0b111] },
        { 'M', [0b101, 0b111, 0b101, 0b101, 0b101] },
        { 'N', [0b110, 0b101, 0b101, 0b101, 0b101] },
        { 'O', [0b111, 0b101, 0b101, 0b101, 0b111] },
        { 'P', [0b111, 0b101, 0b111, 0b100, 0b100] },
        { 'Q', [0b111, 0b101, 0b101, 0b111, 0b001] },
        { 'R', [0b111, 0b101, 0b110, 0b101, 0b101] },
        { 'S', [0b111, 0b100, 0b111, 0b001, 0b111] },
        { 'T', [0b111, 0b010, 0b010, 0b010, 0b010] },
        { 'U', [0b101, 0b101, 0b101, 0b101, 0b111] },
        { 'V', [0b101, 0b101, 0b101, 0b101, 0b010] },
        { 'W', [0b101, 0b101, 0b101, 0b111, 0b101] },
        { 'X', [0b101, 0b101, 0b010, 0b101, 0b101] },
        { 'Y', [0b101, 0b101, 0b111, 0b010, 0b010] },
        { 'Z', [0b111, 0b001, 0b010, 0b100, 0b111] },
        { '0', [0b111, 0b101, 0b101, 0b101, 0b111] },
        { '1', [0b010, 0b110, 0b010, 0b010, 0b111] },
        { '2', [0b111, 0b001, 0b111, 0b100, 0b111] },
        { '3', [0b111, 0b001, 0b111, 0b001, 0b111] },
        { '4', [0b101, 0b101, 0b111, 0b001, 0b001] },
        { '5', [0b111, 0b100, 0b111, 0b001, 0b111] },
        { '6', [0b111, 0b100, 0b111, 0b101, 0b111] },
        { '7', [0b111, 0b001, 0b010, 0b010, 0b010] },
        { '8', [0b111, 0b101, 0b111, 0b101, 0b111] },
        { '9', [0b111, 0b101, 0b111, 0b001, 0b111] },
        { ':', [0b000, 0b010, 0b000, 0b010, 0b000] },
        { '-', [0b000, 0b000, 0b111, 0b000, 0b000] },
        { '!', [0b010, 0b010, 0b010, 0b000, 0b010] },
        { '.', [0b000, 0b000, 0b000, 0b000, 0b010] },
        { '[', [0b110, 0b100, 0b100, 0b100, 0b110] },
        { ']', [0b011, 0b001, 0b001, 0b001, 0b011] },
        { ' ', [0b000, 0b000, 0b000, 0b000, 0b000] }
    };

    private static void DrawPixelText(string text, int x, int y, Color color)
    {
        var cx = x;
        foreach (var rawCh in text.ToUpperInvariant())
        {
            if (Font3x5.TryGetValue(rawCh, out var rows))
            {
                for (var r = 0; r < 5; r++)
                {
                    var rowBits = rows[r];
                    for (var c = 0; c < 3; c++)
                    {
                        if ((rowBits & (1 << (2 - c))) != 0)
                        {
                            Core.DrawPixel(cx + c, y + r, color, 1, 1);
                        }
                    }
                }
            }

            cx += 4; // 3 width + 1 spacing
        }
    }

    private static int GetPixelTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return (text.Length * 4) - 1;
    }

    #endregion
}
