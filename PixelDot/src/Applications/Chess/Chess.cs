using PixelDot;

namespace Applications;

[RunnableApplication("Chess", "1.0.0", "PixelDot", "Two-player chess game")]
public class Chess : Application
{
    // -------------------------------------------------------------------------
    // Board constants
    // -------------------------------------------------------------------------
    private const int BoardSize = 8;

    // -------------------------------------------------------------------------
    // Piece encoding
    // None = 0, Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6
    // Positive = White, Negative = Black
    // -------------------------------------------------------------------------
    private enum PieceType { None = 0, Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6 }
    private enum PieceColour { None, White, Black }

    private readonly record struct Piece(PieceType Type, PieceColour Colour)
    {
        public static readonly Piece Empty = new(PieceType.None, PieceColour.None);
        public bool IsEmpty => Type == PieceType.None;
    }

    // -------------------------------------------------------------------------
    // Game state
    // -------------------------------------------------------------------------
    private enum GameState { Playing, Promotion, GameOver }

    private GameState state = GameState.Playing;
    private PieceColour currentTurn = PieceColour.White;
    private readonly Piece[,] board = new Piece[BoardSize, BoardSize]; // [col, row]  col=0→a, row=0→rank8 (top)

    // Selection
    private int selectedCol = -1;
    private int selectedRow = -1;
    private bool hasPieceSelected;
    private readonly List<(int col, int row)> legalMoves = [];

    // En passant target square (column, row) of the pawn that can be captured
    private int enPassantCol = -1;
    private int enPassantRow = -1;

    // Castling rights
    private bool whiteCanCastleKingside  = true;
    private bool whiteCanCastleQueenside = true;
    private bool blackCanCastleKingside  = true;
    private bool blackCanCastleQueenside = true;

    // Promotion
    private int promotionCol;
    private int promotionRow;
    private int promotionMenuSel; // 0=Queen,1=Rook,2=Bishop,3=Knight

    // Status message
    private string statusMsg = "WHITE'S TURN";

    // Game over reason
    private string gameOverMsg = "";

    // -------------------------------------------------------------------------
    // Layout (computed in OnInit / on every draw to handle different resolutions)
    // -------------------------------------------------------------------------
    private int cellSize;
    private int boardPixelX;
    private int boardPixelY;

    // -------------------------------------------------------------------------
    // Input debounce
    // -------------------------------------------------------------------------
    private bool wasMouseLeft;
    private bool wasRestart;

    // -------------------------------------------------------------------------
    // Colours (using palette indices from default NES-Advanced palette)
    // -------------------------------------------------------------------------
    // We'll define our own explicit SFML colours so we're palette-independent
    private static readonly Color ColBg         = new(0x26, 0x23, 0x2f);  // very dark
    private static readonly Color ColLightSquare = new(0xdf, 0x9c, 0x6e); // warm tan
    private static readonly Color ColDarkSquare  = new(0x8e, 0x4e, 0x11); // brown
    private static readonly Color ColSelected    = new(0xea, 0xd1, 0x1e); // yellow
    private static readonly Color ColLegalDot    = new(0x0b, 0xcb, 0x12); // green
    private static readonly Color ColWhitePiece  = new(0xfc, 0xfc, 0xfc); // white
    private static readonly Color ColBlackPiece  = new(0x26, 0x23, 0x2f); // very dark
    private static readonly Color ColPieceOutline = new(0x59, 0x6d, 0x62); // grey-green
    private static readonly Color ColText        = new(0xfc, 0xfc, 0xfc);
    private static readonly Color ColTextDim     = new(0x92, 0x9c, 0x74);
    private static readonly Color ColAccent      = new(0xcc, 0x4b, 0xb9); // magenta
    private static readonly Color ColCheck       = new(0xe8, 0x18, 0x13); // red
    private static readonly Color ColPanelBg     = new(0x31, 0x40, 0x47);
    private static readonly Color ColBorder      = new(0x59, 0x6d, 0x62);
    private static readonly Color ColPromoHl     = new(0xf0, 0xaf, 0x66); // orange

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------
    public override void OnInit()
    {
        Logger.LogInfo("Chess initialised");
        ResetGame();
    }

    public override void OnUpdate()
    {
        CalculateLayout();
        HandleInput();
        DrawGame();
    }

    // -------------------------------------------------------------------------
    // Layout
    // -------------------------------------------------------------------------
    private void CalculateLayout()
    {
        // Leave a margin of ~40px each side for the info panels, and some top/bottom padding
        var availW = Core.ScreenWidth  - 80;
        var availH = Core.ScreenHeight - 20;
        cellSize = Math.Max(4, Math.Min(availW, availH) / BoardSize);

        var boardW = cellSize * BoardSize;
        var boardH = cellSize * BoardSize;
        boardPixelX = (Core.ScreenWidth  - boardW) / 2;
        boardPixelY = (Core.ScreenHeight - boardH) / 2;
    }

    // -------------------------------------------------------------------------
    // Game reset
    // -------------------------------------------------------------------------
    private void ResetGame()
    {
        // Clear board
        for (var c = 0; c < BoardSize; c++)
            for (var r = 0; r < BoardSize; r++)
                board[c, r] = Piece.Empty;

        // Place pawns
        for (var c = 0; c < BoardSize; c++)
        {
            board[c, 1] = new Piece(PieceType.Pawn, PieceColour.Black);
            board[c, 6] = new Piece(PieceType.Pawn, PieceColour.White);
        }

        // Place back-rank pieces
        PlaceBackRank(0, PieceColour.Black);
        PlaceBackRank(7, PieceColour.White);

        currentTurn = PieceColour.White;
        hasPieceSelected = false;
        selectedCol = -1;
        selectedRow = -1;
        legalMoves.Clear();
        enPassantCol = -1;
        enPassantRow = -1;
        whiteCanCastleKingside  = true;
        whiteCanCastleQueenside = true;
        blackCanCastleKingside  = true;
        blackCanCastleQueenside = true;
        state = GameState.Playing;
        statusMsg = "WHITE'S TURN";
        gameOverMsg = "";
        promotionMenuSel = 0;
    }

    private void PlaceBackRank(int row, PieceColour colour)
    {
        board[0, row] = new Piece(PieceType.Rook,   colour);
        board[1, row] = new Piece(PieceType.Knight, colour);
        board[2, row] = new Piece(PieceType.Bishop, colour);
        board[3, row] = new Piece(PieceType.Queen,  colour);
        board[4, row] = new Piece(PieceType.King,   colour);
        board[5, row] = new Piece(PieceType.Bishop, colour);
        board[6, row] = new Piece(PieceType.Knight, colour);
        board[7, row] = new Piece(PieceType.Rook,   colour);
    }

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------
    private void HandleInput()
    {
        var nowLeft    = Input.IsPressed(MouseButtons.Left);
        var nowRestart = Input.IsPressed(Keys.R) || Input.IsPressed(Keys.Enter);

        if (nowRestart && !wasRestart && state == GameState.GameOver)
        {
            ResetGame();
        }

        if (nowLeft && !wasMouseLeft)
        {
            var mx = Input.GetMousePositionX();
            var my = Input.GetMousePositionY();

            if (state == GameState.Playing)
            {
                HandleBoardClick(mx, my);
            }
            else if (state == GameState.Promotion)
            {
                HandlePromotionClick(mx, my);
            }
        }

        // Keyboard promotion selection
        if (state == GameState.Promotion)
        {
            if (Input.IsPressed(Keys.ArrowLeft)  && !wasMouseLeft) promotionMenuSel = (promotionMenuSel + 3) % 4;
            if (Input.IsPressed(Keys.ArrowRight) && !wasMouseLeft) promotionMenuSel = (promotionMenuSel + 1) % 4;
            if (Input.IsPressed(Keys.Enter)      && !wasRestart)   ConfirmPromotion();
        }

        wasMouseLeft = nowLeft;
        wasRestart   = nowRestart;
    }

    private void HandleBoardClick(int mx, int my)
    {
        var col = (mx - boardPixelX) / cellSize;
        var row = (my - boardPixelY) / cellSize;

        // Outside board
        if (col < 0 || col >= BoardSize || row < 0 || row >= BoardSize)
        {
            Deselect();
            return;
        }

        if (hasPieceSelected)
        {
            // Check if the click is a legal move target
            var isLegal = legalMoves.Contains((col, row));
            if (isLegal)
            {
                ApplyMove(selectedCol, selectedRow, col, row);
                return;
            }

            // Click own piece → re-select
            var clicked = board[col, row];
            if (!clicked.IsEmpty && clicked.Colour == currentTurn)
            {
                SelectPiece(col, row);
                return;
            }

            // Otherwise deselect
            Deselect();
        }
        else
        {
            var piece = board[col, row];
            if (!piece.IsEmpty && piece.Colour == currentTurn)
            {
                SelectPiece(col, row);
            }
        }
    }

    private void HandlePromotionClick(int mx, int my)
    {
        // Promotion menu is a horizontal strip of 4 options centred on the board
        var menuW   = cellSize * 4;
        var menuH   = cellSize + 4;
        var menuX   = boardPixelX + ((BoardSize * cellSize - menuW) / 2);
        var menuY   = (Core.ScreenHeight - menuH) / 2;

        if (mx >= menuX && mx < menuX + menuW && my >= menuY && my < menuY + menuH)
        {
            var idx = (mx - menuX) / cellSize;
            if (idx >= 0 && idx < 4)
            {
                promotionMenuSel = idx;
                ConfirmPromotion();
            }
        }
    }

    private void SelectPiece(int col, int row)
    {
        selectedCol = col;
        selectedRow = row;
        hasPieceSelected = true;
        legalMoves.Clear();
        ComputeLegalMoves(col, row, legalMoves);
    }

    private void Deselect()
    {
        hasPieceSelected = false;
        selectedCol = -1;
        selectedRow = -1;
        legalMoves.Clear();
    }

    // -------------------------------------------------------------------------
    // Move execution
    // -------------------------------------------------------------------------
    private void ApplyMove(int fromCol, int fromRow, int toCol, int toRow)
    {
        var piece = board[fromCol, fromRow];

        // Track new en-passant square
        int newEpCol = -1, newEpRow = -1;

        // Castling
        if (piece.Type == PieceType.King)
        {
            var dc = toCol - fromCol;
            if (Math.Abs(dc) == 2)
            {
                // King-side or queen-side castle
                var rookFromCol = dc > 0 ? 7 : 0;
                var rookToCol   = dc > 0 ? toCol - 1 : toCol + 1;
                board[rookToCol, toRow] = board[rookFromCol, fromRow];
                board[rookFromCol, fromRow] = Piece.Empty;
            }

            // Revoke castling rights for this colour
            if (piece.Colour == PieceColour.White)
            {
                whiteCanCastleKingside  = false;
                whiteCanCastleQueenside = false;
            }
            else
            {
                blackCanCastleKingside  = false;
                blackCanCastleQueenside = false;
            }
        }

        // Revoke rook castling rights when a rook moves
        if (piece.Type == PieceType.Rook)
        {
            if (piece.Colour == PieceColour.White)
            {
                if (fromCol == 7 && fromRow == 7) whiteCanCastleKingside  = false;
                if (fromCol == 0 && fromRow == 7) whiteCanCastleQueenside = false;
            }
            else
            {
                if (fromCol == 7 && fromRow == 0) blackCanCastleKingside  = false;
                if (fromCol == 0 && fromRow == 0) blackCanCastleQueenside = false;
            }
        }

        // En passant capture
        if (piece.Type == PieceType.Pawn && toCol == enPassantCol && toRow == enPassantRow)
        {
            // Remove the captured pawn (it's behind the target square relative to movement direction)
            var capturedPawnRow = fromRow; // same rank as moving pawn, different file
            board[toCol, capturedPawnRow] = Piece.Empty;
        }

        // Set en-passant target if pawn moved two squares
        if (piece.Type == PieceType.Pawn && Math.Abs(toRow - fromRow) == 2)
        {
            newEpCol = toCol;
            newEpRow = (fromRow + toRow) / 2;
        }

        // Revoke castling rights when rook is captured
        if (!board[toCol, toRow].IsEmpty && board[toCol, toRow].Type == PieceType.Rook)
        {
            if (toCol == 7 && toRow == 7) whiteCanCastleKingside  = false;
            if (toCol == 0 && toRow == 7) whiteCanCastleQueenside = false;
            if (toCol == 7 && toRow == 0) blackCanCastleKingside  = false;
            if (toCol == 0 && toRow == 0) blackCanCastleQueenside = false;
        }

        // Execute the move
        board[toCol, toRow]     = piece;
        board[fromCol, fromRow] = Piece.Empty;

        enPassantCol = newEpCol;
        enPassantRow = newEpRow;

        Deselect();

        // Check for pawn promotion
        if (piece.Type == PieceType.Pawn && (toRow == 0 || toRow == 7))
        {
            promotionCol = toCol;
            promotionRow = toRow;
            promotionMenuSel = 0;
            state = GameState.Promotion;
            statusMsg = "PROMOTE PAWN";
            return;
        }

        SwitchTurn();
    }

    private void ConfirmPromotion()
    {
        PieceType[] options = [PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight];
        board[promotionCol, promotionRow] = new Piece(options[promotionMenuSel], currentTurn);
        state = GameState.Playing;
        SwitchTurn();
    }

    private void SwitchTurn()
    {
        currentTurn = currentTurn == PieceColour.White ? PieceColour.Black : PieceColour.White;

        // Check for check / checkmate / stalemate
        var inCheck    = IsKingInCheck(currentTurn);
        var hasAnyMove = HasAnyLegalMove(currentTurn);

        if (!hasAnyMove)
        {
            if (inCheck)
            {
                var winner = currentTurn == PieceColour.White ? "BLACK" : "WHITE";
                gameOverMsg = $"{winner} WINS! CHECKMATE";
            }
            else
            {
                gameOverMsg = "STALEMATE - DRAW";
            }
            state = GameState.GameOver;
            statusMsg = "GAME OVER";
        }
        else
        {
            statusMsg = inCheck
                ? $"{(currentTurn == PieceColour.White ? "WHITE" : "BLACK")} IN CHECK!"
                : $"{(currentTurn == PieceColour.White ? "WHITE" : "BLACK")}'S TURN";
        }
    }

    // -------------------------------------------------------------------------
    // Legal move generation
    // -------------------------------------------------------------------------
    private void ComputeLegalMoves(int col, int row, List<(int, int)> moves)
    {
        var piece = board[col, row];
        if (piece.IsEmpty) return;

        var pseudo = new List<(int, int)>();
        GeneratePseudoLegalMoves(col, row, piece, pseudo);

        // Filter: exclude moves that leave own king in check
        foreach (var (tc, tr) in pseudo)
        {
            if (!MoveLeavesKingInCheck(col, row, tc, tr, piece.Colour))
                moves.Add((tc, tr));
        }
    }

    private void GeneratePseudoLegalMoves(int col, int row, Piece piece, List<(int, int)> moves)
    {
        switch (piece.Type)
        {
            case PieceType.Pawn:   GeneratePawnMoves(col, row, piece.Colour, moves);   break;
            case PieceType.Knight: GenerateKnightMoves(col, row, piece.Colour, moves); break;
            case PieceType.Bishop: GenerateSlidingMoves(col, row, piece.Colour, moves, true, false);  break;
            case PieceType.Rook:   GenerateSlidingMoves(col, row, piece.Colour, moves, false, true);  break;
            case PieceType.Queen:  GenerateSlidingMoves(col, row, piece.Colour, moves, true, true);   break;
            case PieceType.King:   GenerateKingMoves(col, row, piece.Colour, moves);   break;
        }
    }

    private void GeneratePawnMoves(int col, int row, PieceColour colour, List<(int, int)> moves)
    {
        var dir     = colour == PieceColour.White ? -1 : 1;  // row decreases for white
        var startRow = colour == PieceColour.White ? 6 : 1;

        // One step forward
        var nr = row + dir;
        if (InBounds(col, nr) && board[col, nr].IsEmpty)
        {
            moves.Add((col, nr));
            // Two steps from start
            if (row == startRow && board[col, nr + dir].IsEmpty)
                moves.Add((col, nr + dir));
        }

        // Captures
        foreach (var dc in new[] { -1, 1 })
        {
            var nc = col + dc;
            if (!InBounds(nc, nr)) continue;

            var target = board[nc, nr];
            if (!target.IsEmpty && target.Colour != colour)
                moves.Add((nc, nr));

            // En passant
            if (nc == enPassantCol && nr == enPassantRow)
                moves.Add((nc, nr));
        }
    }

    private void GenerateKnightMoves(int col, int row, PieceColour colour, List<(int, int)> moves)
    {
        int[] dx = [-2, -2, -1, -1, 1, 1, 2, 2];
        int[] dy = [-1,  1, -2,  2, -2, 2, -1, 1];
        for (var i = 0; i < 8; i++)
        {
            var nc = col + dx[i];
            var nr = row + dy[i];
            if (!InBounds(nc, nr)) continue;
            var target = board[nc, nr];
            if (target.IsEmpty || target.Colour != colour)
                moves.Add((nc, nr));
        }
    }

    private void GenerateSlidingMoves(int col, int row, PieceColour colour, List<(int, int)> moves, bool diagonal, bool straight)
    {
        (int dc, int dr)[] dirs = [];
        if (diagonal && straight) dirs = [(-1,-1),(-1,0),(-1,1),(0,-1),(0,1),(1,-1),(1,0),(1,1)];
        else if (diagonal)        dirs = [(-1,-1),(-1,1),(1,-1),(1,1)];
        else                      dirs = [(-1,0),(1,0),(0,-1),(0,1)];

        foreach (var (dc, dr) in dirs)
        {
            var nc = col + dc;
            var nr = row + dr;
            while (InBounds(nc, nr))
            {
                var target = board[nc, nr];
                if (target.IsEmpty)
                {
                    moves.Add((nc, nr));
                }
                else
                {
                    if (target.Colour != colour) moves.Add((nc, nr));
                    break;
                }
                nc += dc;
                nr += dr;
            }
        }
    }

    private void GenerateKingMoves(int col, int row, PieceColour colour, List<(int, int)> moves)
    {
        for (var dc = -1; dc <= 1; dc++)
        for (var dr = -1; dr <= 1; dr++)
        {
            if (dc == 0 && dr == 0) continue;
            var nc = col + dc;
            var nr = row + dr;
            if (!InBounds(nc, nr)) continue;
            var target = board[nc, nr];
            if (target.IsEmpty || target.Colour != colour)
                moves.Add((nc, nr));
        }

        // Castling
        var backRow = colour == PieceColour.White ? 7 : 0;
        if (row == backRow && col == 4)
        {
            var kingside  = colour == PieceColour.White ? whiteCanCastleKingside  : blackCanCastleKingside;
            var queenside = colour == PieceColour.White ? whiteCanCastleQueenside : blackCanCastleQueenside;

            // King-side: squares 5 and 6 must be empty, king must not pass through check
            if (kingside && board[5, backRow].IsEmpty && board[6, backRow].IsEmpty
                && !IsSquareAttackedBy(4, backRow, Opposite(colour))
                && !IsSquareAttackedBy(5, backRow, Opposite(colour))
                && !IsSquareAttackedBy(6, backRow, Opposite(colour)))
            {
                moves.Add((6, backRow));
            }

            // Queen-side: squares 1, 2, 3 empty; king traverses 2 and 3
            if (queenside && board[1, backRow].IsEmpty && board[2, backRow].IsEmpty && board[3, backRow].IsEmpty
                && !IsSquareAttackedBy(4, backRow, Opposite(colour))
                && !IsSquareAttackedBy(3, backRow, Opposite(colour))
                && !IsSquareAttackedBy(2, backRow, Opposite(colour)))
            {
                moves.Add((2, backRow));
            }
        }
    }

    // -------------------------------------------------------------------------
    // Check detection helpers
    // -------------------------------------------------------------------------
    private bool MoveLeavesKingInCheck(int fromCol, int fromRow, int toCol, int toRow, PieceColour colour)
    {
        // Make move on a temporary copy
        var saved        = board[toCol, toRow];
        var savedEpCol   = enPassantCol;
        var savedEpRow   = enPassantRow;
        var movingPiece  = board[fromCol, fromRow];

        // Handle en-passant capture removal
        int epCapCol = -1, epCapRow = -1;
        if (movingPiece.Type == PieceType.Pawn && toCol == enPassantCol && toRow == enPassantRow)
        {
            epCapCol = toCol;
            epCapRow = fromRow;
        }

        board[toCol, toRow]     = movingPiece;
        board[fromCol, fromRow] = Piece.Empty;
        if (epCapCol >= 0) board[epCapCol, epCapRow] = Piece.Empty;

        var inCheck = IsKingInCheck(colour);

        // Undo
        board[fromCol, fromRow] = movingPiece;
        board[toCol, toRow]     = saved;
        if (epCapCol >= 0) board[epCapCol, epCapRow] = new Piece(PieceType.Pawn, Opposite(colour));
        enPassantCol = savedEpCol;
        enPassantRow = savedEpRow;

        return inCheck;
    }

    private bool IsKingInCheck(PieceColour colour)
    {
        // Find king
        for (var c = 0; c < BoardSize; c++)
        for (var r = 0; r < BoardSize; r++)
        {
            var p = board[c, r];
            if (p.Type == PieceType.King && p.Colour == colour)
                return IsSquareAttackedBy(c, r, Opposite(colour));
        }
        return false;
    }

    private bool IsSquareAttackedBy(int col, int row, PieceColour attacker)
    {
        // Check all opponent pieces and see if they can attack (col, row)
        for (var c = 0; c < BoardSize; c++)
        for (var r = 0; r < BoardSize; r++)
        {
            var p = board[c, r];
            if (p.IsEmpty || p.Colour != attacker) continue;

            var attacks = new List<(int, int)>();
            GeneratePseudoLegalMoves(c, r, p, attacks);
            if (attacks.Contains((col, row))) return true;
        }
        return false;
    }

    private bool HasAnyLegalMove(PieceColour colour)
    {
        for (var c = 0; c < BoardSize; c++)
        for (var r = 0; r < BoardSize; r++)
        {
            var p = board[c, r];
            if (p.IsEmpty || p.Colour != colour) continue;
            var moves = new List<(int, int)>();
            ComputeLegalMoves(c, r, moves);
            if (moves.Count > 0) return true;
        }
        return false;
    }

    private static PieceColour Opposite(PieceColour c) =>
        c == PieceColour.White ? PieceColour.Black : PieceColour.White;

    private static bool InBounds(int c, int r) =>
        c >= 0 && c < BoardSize && r >= 0 && r < BoardSize;

    // -------------------------------------------------------------------------
    // Drawing
    // -------------------------------------------------------------------------
    private void DrawGame()
    {
        DrawBackground();
        DrawBoard();
        DrawLegalMoveHints();
        DrawPieces();
        DrawCoordinateLabels();
        DrawSidePanel();
        DrawStatusBar();

        if (state == GameState.Promotion)
            DrawPromotionMenu();
        else if (state == GameState.GameOver)
            DrawGameOverOverlay();
    }

    private void DrawBackground()
    {
        Core.DrawRectangle(0, 0, Core.ScreenWidth, Core.ScreenHeight, ColBg, 1, true);
        Core.DrawRectangle(0, 0, Core.ScreenWidth, 2, ColBorder, 1, true);
        Core.DrawRectangle(0, Core.ScreenHeight - 2, Core.ScreenWidth, 2, ColBorder, 1, true);
    }

    private void DrawBoard()
    {
        for (var c = 0; c < BoardSize; c++)
        for (var r = 0; r < BoardSize; r++)
        {
            var isLight  = (c + r) % 2 == 0;
            var isSelected = hasPieceSelected && c == selectedCol && r == selectedRow;

            Color col;
            if (isSelected)
                col = ColSelected;
            else
                col = isLight ? ColLightSquare : ColDarkSquare;

            var px = boardPixelX + c * cellSize;
            var py = boardPixelY + r * cellSize;
            Core.DrawRectangle(px, py, cellSize, cellSize, col, 1, true);
        }

        // Board border
        Core.DrawRectangle(boardPixelX - 1, boardPixelY - 1, BoardSize * cellSize + 2, BoardSize * cellSize + 2, ColBorder, 1, false);
    }

    private void DrawLegalMoveHints()
    {
        if (!hasPieceSelected) return;

        foreach (var (c, r) in legalMoves)
        {
            var px = boardPixelX + c * cellSize;
            var py = boardPixelY + r * cellSize;

            if (!board[c, r].IsEmpty)
            {
                // Capture: highlight square edge
                Core.DrawRectangle(px, py, cellSize, cellSize, ColLegalDot, 1, false);
            }
            else
            {
                // Empty square: small dot in centre
                var dotSize = Math.Max(2, cellSize / 4);
                var dotX = px + (cellSize - dotSize) / 2;
                var dotY = py + (cellSize - dotSize) / 2;
                Core.DrawRectangle(dotX, dotY, dotSize, dotSize, ColLegalDot, 1, true);
            }
        }
    }

    private void DrawPieces()
    {
        // Check highlighting — find king in check
        var inCheck = state == GameState.Playing && IsKingInCheck(currentTurn);
        int checkKingCol = -1, checkKingRow = -1;
        if (inCheck)
        {
            for (var c = 0; c < BoardSize; c++)
            for (var r = 0; r < BoardSize; r++)
            {
                var p = board[c, r];
                if (p.Type == PieceType.King && p.Colour == currentTurn)
                { checkKingCol = c; checkKingRow = r; }
            }
        }

        for (var c = 0; c < BoardSize; c++)
        for (var r = 0; r < BoardSize; r++)
        {
            var piece = board[c, r];
            if (piece.IsEmpty) continue;

            var px = boardPixelX + c * cellSize;
            var py = boardPixelY + r * cellSize;

            // Flash check square
            if (c == checkKingCol && r == checkKingRow)
                Core.DrawRectangle(px, py, cellSize, cellSize, ColCheck, 1, true);

            DrawPieceSprite(px, py, piece);
        }
    }

    private void DrawPieceSprite(int px, int py, Piece piece)
    {
        var bodyCol    = piece.Colour == PieceColour.White ? ColWhitePiece : ColBlackPiece;
        var outlineCol = piece.Colour == PieceColour.White ? ColBlackPiece : ColWhitePiece;

        var margin = Math.Max(1, cellSize / 6);
        var inner  = cellSize - margin * 2;
        if (inner < 1) inner = 1;

        var ix = px + margin;
        var iy = py + margin;

        // Draw outline then fill
        Core.DrawRectangle(ix - 1, iy - 1, inner + 2, inner + 2, outlineCol, 1, true);
        Core.DrawRectangle(ix, iy, inner, inner, bodyCol, 1, true);

        // Draw piece symbol using our tiny pixel font (centred)
        var sym = GetPieceSymbol(piece.Type);
        var tw  = GetPixelTextWidth(sym);
        var tx  = px + (cellSize - tw) / 2;
        var ty  = py + (cellSize - 5) / 2;

        if (cellSize >= 8)
            DrawPixelText(sym, tx, ty, outlineCol);
    }

    private static string GetPieceSymbol(PieceType t) => t switch
    {
        PieceType.Pawn   => "P",
        PieceType.Knight => "N",
        PieceType.Bishop => "B",
        PieceType.Rook   => "R",
        PieceType.Queen  => "Q",
        PieceType.King   => "K",
        _                => " "
    };

    private void DrawCoordinateLabels()
    {
        if (cellSize < 6) return; // Not enough space

        // File labels (a-h) below the board
        string[] files = ["A", "B", "C", "D", "E", "F", "G", "H"];
        for (var c = 0; c < BoardSize; c++)
        {
            var lx = boardPixelX + c * cellSize + (cellSize - 3) / 2;
            var ly = boardPixelY + BoardSize * cellSize + 2;
            DrawPixelText(files[c], lx, ly, ColTextDim);
        }

        // Rank labels (8-1) left of board
        for (var r = 0; r < BoardSize; r++)
        {
            var rank = (8 - r).ToString();
            var lx = boardPixelX - 5;
            var ly = boardPixelY + r * cellSize + (cellSize - 5) / 2;
            DrawPixelText(rank, lx, ly, ColTextDim);
        }
    }

    private void DrawSidePanel()
    {
        // Right-side info panel
        var panelX = boardPixelX + BoardSize * cellSize + 6;
        var panelW = Core.ScreenWidth - panelX - 2;
        if (panelW < 20) return;

        var panelY = boardPixelY;
        var panelH = BoardSize * cellSize;

        Core.DrawRectangle(panelX, panelY, panelW, panelH, ColPanelBg, 1, true);
        Core.DrawRectangle(panelX, panelY, panelW, panelH, ColBorder,  1, false);

        var cx = panelX + 2;
        var cy = panelY + 4;

        DrawPixelText("CHESS", cx, cy, ColAccent);
        cy += 10;

        DrawPixelText("TURN", cx, cy, ColTextDim);
        cy += 7;
        var turnStr = currentTurn == PieceColour.White ? "WHITE" : "BLACK";
        DrawPixelText(turnStr, cx, cy, ColText);
        cy += 10;

        // Captured pieces summary (count per side)
        var whiteLost = 15 - CountPieces(PieceColour.White);
        var blackLost = 15 - CountPieces(PieceColour.Black);

        DrawPixelText("CAPT", cx, cy, ColTextDim);
        cy += 7;
        DrawPixelText($"W:{blackLost}", cx, cy, ColText);
        cy += 7;
        DrawPixelText($"B:{whiteLost}", cx, cy, ColText);
        cy += 10;

        // Controls hint
        DrawPixelText("CLICK", cx, cy, ColTextDim);
        cy += 7;
        DrawPixelText("PIECE", cx, cy, ColTextDim);
        cy += 7;
        DrawPixelText("THEN", cx, cy, ColTextDim);
        cy += 7;
        DrawPixelText("DEST", cx, cy, ColTextDim);
        cy += 10;

        if (state == GameState.GameOver)
        {
            DrawPixelText("[R]", cx, cy, ColAccent);
            cy += 7;
            DrawPixelText("RESET", cx, cy, ColAccent);
        }
    }

    private int CountPieces(PieceColour colour)
    {
        // Count non-king pieces
        var count = 0;
        for (var c = 0; c < BoardSize; c++)
        for (var r = 0; r < BoardSize; r++)
        {
            var p = board[c, r];
            if (!p.IsEmpty && p.Colour == colour && p.Type != PieceType.King)
                count++;
        }
        return count;
    }

    private void DrawStatusBar()
    {
        // Draw status bar above the board
        var sy = boardPixelY - 10;
        if (sy < 2) return;

        var sw = GetPixelTextWidth(statusMsg);
        var sx = boardPixelX + (BoardSize * cellSize - sw) / 2;

        var statusCol = state == GameState.GameOver ? ColCheck
            : statusMsg.Contains("CHECK") ? ColCheck
            : ColText;

        DrawPixelText(statusMsg, Math.Max(2, sx), sy, statusCol);
    }

    private void DrawPromotionMenu()
    {
        var menuW   = cellSize * 4;
        var menuH   = cellSize + 8;
        var menuX   = boardPixelX + ((BoardSize * cellSize - menuW) / 2);
        var menuY   = (Core.ScreenHeight - menuH) / 2;

        // Background
        Core.DrawRectangle(menuX - 2, menuY - 2, menuW + 4, menuH + 4, ColBg, 1, true);
        Core.DrawRectangle(menuX - 2, menuY - 2, menuW + 4, menuH + 4, ColAccent, 1, false);

        // Prompt text
        var prompt = "PROMOTE:";
        DrawPixelText(prompt, menuX + (menuW - GetPixelTextWidth(prompt)) / 2, menuY - 8, ColText);

        // 4 promotion options: Queen, Rook, Bishop, Knight
        PieceType[] options = [PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight];

        for (var i = 0; i < 4; i++)
        {
            var ox = menuX + i * cellSize;
            var oy = menuY + 4;
            var isHl = i == promotionMenuSel;

            Core.DrawRectangle(ox, oy, cellSize, cellSize, isHl ? ColPromoHl : ColPanelBg, 1, true);
            Core.DrawRectangle(ox, oy, cellSize, cellSize, ColBorder, 1, false);

            var sym = GetPieceSymbol(options[i]);
            var tx  = ox + (cellSize - GetPixelTextWidth(sym)) / 2;
            var ty  = oy + (cellSize - 5) / 2;
            DrawPixelText(sym, tx, ty, isHl ? ColBg : ColText);
        }
    }

    private void DrawGameOverOverlay()
    {
        var msgW = GetPixelTextWidth(gameOverMsg);
        var ow   = Math.Max(msgW + 10, 60);
        var oh   = 30;
        var ox   = (Core.ScreenWidth  - ow) / 2;
        var oy   = (Core.ScreenHeight - oh) / 2;

        Core.DrawRectangle(ox, oy, ow, oh, ColBg,    1, true);
        Core.DrawRectangle(ox, oy, ow, oh, ColCheck, 1, false);

        DrawPixelText(gameOverMsg, ox + (ow - msgW) / 2, oy + 7,  ColText);
        var hint = "R=RESTART";
        DrawPixelText(hint,        ox + (ow - GetPixelTextWidth(hint)) / 2, oy + 18, ColAccent);
    }

    // -------------------------------------------------------------------------
    // Retro 3×5 pixel font (copied from Tetris, same codebase convention)
    // -------------------------------------------------------------------------
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
                            Core.DrawPixel(cx + c, y + r, color, 1, 1);
                    }
                }
            }
            cx += 4;
        }
    }

    private static int GetPixelTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Length * 4 - 1;
    }
}
