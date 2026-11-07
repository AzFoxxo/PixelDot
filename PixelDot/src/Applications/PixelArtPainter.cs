/*
 *   Copyright (c) 2026 Az Foxxo (@AzFoxxo on GitHub)
 *   All rights reserved.

 *   Permission is hereby granted, free of charge, to any person obtaining a copy
 *   of this software and associated documentation files (the "Software"), to deal
 *   in the Software without restriction, including without limitation the rights
 *   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *   copies of the Software, and to permit persons to whom the Software is
 *   furnished to do so, subject to the following conditions:

 *   The above copyright notice and this permission notice shall be included in all
 *   copies or substantial portions of the Software.

 *   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *   SOFTWARE.
 */

using PixelDot;

namespace Applications;

/// <summary>
/// PixelArtPainter — pixel art editor built on the PixelDot engine.
///
/// Key bindings (using physical keyboard keys):
///   Z            Pencil tool
///   X            Eraser tool
///   C            Fill (flood-fill) tool
///   Q            Brush size down
///   W            Brush size up
///   Arrow Up     Canvas grow (+4)
///   Arrow Down   Canvas shrink (-4)
///   M            Load palette from palette.txt
///   P            Clear canvas
///   R            Export pixel array → export_*.cs
///   LShift+Z     Undo (up to 64 steps)
///   RMB          Quick erase (any tool)
/// </summary>
[RunnableApplication("Pixel Art Painter", "1.0.0", "Az Foxxo", "Pixel art editor with palette, tools, undo and export")]
public class PixelArtPainter : Application
{
    // -------------------------------------------------------------------------
    // Tool enum
    // -------------------------------------------------------------------------
    private enum Tool { Pencil, Eraser, Fill }

    // -------------------------------------------------------------------------
    // Layout — all in logical pixels
    // -------------------------------------------------------------------------
    // Left palette panel
    private const int PANEL_W     = 24;   // wide enough for two 3px swatch columns + gaps
    private const int SWATCH_SIZE = 3;    // swatch side length
    private const int SWATCH_GAP  = 1;    // gap between swatches (px)

    // Right info/help panel
    // Font size 6 at any pixel scale renders ≈ CharSize×0.55 logical px per glyph.
    // Longest label "RMB   Erase" = 11 glyphs → 11×(6×0.55)≈36 lp. 48 lp gives good margin.
    private const int RIGHT_W = 48;

    // Top toolbar
    private const int TOOLBAR_H = 9;

    // -------------------------------------------------------------------------
    // Canvas state
    // -------------------------------------------------------------------------
    private int canvasW;
    private int canvasH;

    // -------------------------------------------------------------------------
    // Zoom & pan
    // -------------------------------------------------------------------------
    private int zoom    = 1;           // logical pixels rendered per canvas pixel
    private int viewX;                 // canvas-pixel offset of the viewport top-left
    private int viewY;

    private const int MIN_ZOOM = 1;
    private const int MAX_ZOOM = 16;

    // Available display area for the canvas (excludes both panels + toolbar)
    private int AvailW => Core.ScreenWidth  - PANEL_W - RIGHT_W;
    private int AvailH => Core.ScreenHeight - TOOLBAR_H;

    // Screen position of canvas pixel (0,0) — accounts for zoom, pan and centering
    private int OriginX =>
        PANEL_W + Math.Max(0, (AvailW - canvasW * zoom) / 2) - viewX * zoom;
    private int OriginY =>
        TOOLBAR_H + Math.Max(0, (AvailH - canvasH * zoom) / 2) - viewY * zoom;

    // Middle-mouse pan tracking
    private bool prevMiddle;
    private int  prevPanMX, prevPanMY;

    /// <summary>Palette index per pixel; 0xFF = transparent (checkerboard).</summary>
    private byte[] canvas = [];

    // -------------------------------------------------------------------------
    // Undo stack
    // -------------------------------------------------------------------------
    private const int MAX_UNDO = 64;
    private readonly Queue<byte[]> undoStack = new();

    // -------------------------------------------------------------------------
    // Drawing state
    // -------------------------------------------------------------------------
    private byte selectedColour;
    private int  brushSize  = 1;
    private Tool activeTool = Tool.Pencil;

    // -------------------------------------------------------------------------
    // Edge-detection booleans (previous-frame key states)
    // -------------------------------------------------------------------------
    private bool prevLeft, prevRight;
    private bool prevZ, prevX, prevC;
    private bool prevQ, prevW;
    private bool prevArrowUp, prevArrowDown;
    private bool prevM, prevP, prevR;
    // -------------------------------------------------------------------------
    // Status bar
    // -------------------------------------------------------------------------
    private string statusMsg   = "Welcome!  Z=Pencil  X=Eraser  C=Fill";
    private int    statusTimer = 300;

    // =========================================================================
    // Lifecycle
    // =========================================================================

    public override void OnInit()
    {
        Text.SetFont("DejaVu Sans");
        Text.SetAntiAliasing(false);
        ResetCanvas(32, 32);
        Logger.LogInfo("PixelArtPainter ready");
    }

    public override void OnUpdate()
    {
        HandleInput();
        Render();
    }

    public override void OnEnd() => Logger.LogInfo("PixelArtPainter closed");

    // =========================================================================
    // Input
    // =========================================================================

    private void HandleInput()
    {
        bool shiftHeld = Input.IsPressed(Keys.LShift);

        // ---- Tool selection (Z / X / C, only without shift) ----
        bool z = Input.IsPressed(Keys.Z);
        bool x = Input.IsPressed(Keys.X);
        bool c = Input.IsPressed(Keys.C);

        if (z && !prevZ && !shiftHeld) { activeTool = Tool.Pencil; Status("Pencil"); }
        if (x && !prevX)               { activeTool = Tool.Eraser; Status("Eraser"); }
        if (c && !prevC)               { activeTool = Tool.Fill;   Status("Fill"); }

        // ---- Undo: LShift + Z ----
        if (shiftHeld && z && !prevZ) Undo();

        prevZ = z; prevX = x; prevC = c;

        // ---- Brush size: Q = smaller (Keys.Q), W = larger (Keys.Up = Key.W) ----
        bool q = Input.IsPressed(Keys.Q);
        bool w = Input.IsPressed(Keys.Up);   // Keys.Up maps to physical W key

        if (q && !prevQ) { brushSize = Math.Max(1, brushSize - 1); Status($"Brush {brushSize}"); }
        if (w && !prevW) { brushSize = Math.Min(8, brushSize + 1); Status($"Brush {brushSize}"); }

        prevQ = q; prevW = w;

        // ---- Scroll wheel: zoom canvas (centred on mouse cursor) ----
        float scroll = Input.GetScrollDelta();
        if (scroll != 0f)
        {
            int mxs = Input.GetMousePositionX();
            int mys = Input.GetMousePositionY();

            // Canvas pixel currently under the mouse (fractional, pre-zoom)
            float cpx = (mxs - OriginX) / (float)zoom;
            float cpy = (mys - OriginY) / (float)zoom;

            int newZoom = scroll > 0
                ? Math.Min(MAX_ZOOM, zoom + 1)
                : Math.Max(MIN_ZOOM, zoom - 1);

            if (newZoom != zoom)
            {
                zoom = newZoom;
                // Re-anchor so the canvas pixel under the mouse stays there
                int newOriginX = PANEL_W + Math.Max(0, (AvailW - canvasW * zoom) / 2);
                int newOriginY = TOOLBAR_H + Math.Max(0, (AvailH - canvasH * zoom) / 2);
                viewX = (int)(cpx - (mxs - newOriginX) / (float)zoom);
                viewY = (int)(cpy - (mys - newOriginY) / (float)zoom);
                ClampView();
                Status($"Zoom {zoom}x");
            }
        }

        // ---- Middle-mouse drag: pan ----
        bool mmb = Input.IsPressed(MouseButtons.Middle);
        int mx = Input.GetMousePositionX();
        int my = Input.GetMousePositionY();

        if (mmb)
        {
            if (prevMiddle)
            {
                int dmx = mx - prevPanMX;
                int dmy = my - prevPanMY;
                // Convert screen-pixel drag to canvas-pixel pan (subtract because dragging right moves view left)
                viewX = Math.Max(0, viewX - dmx / zoom);
                viewY = Math.Max(0, viewY - dmy / zoom);
                ClampView();
            }
            prevPanMX = mx;
            prevPanMY = my;
        }
        prevMiddle = mmb;

        // ---- Canvas resize: Arrow Up / Arrow Down ----
        bool arrowUp   = Input.IsPressed(Keys.ArrowUp);
        bool arrowDown = Input.IsPressed(Keys.ArrowDown);

        if (arrowUp   && !prevArrowUp)   ResizeCanvas(canvasW + 4, canvasH + 4);
        if (arrowDown && !prevArrowDown) ResizeCanvas(canvasW - 4, canvasH - 4);

        prevArrowUp   = arrowUp;
        prevArrowDown = arrowDown;

        // ---- Commands ----
        bool m = Input.IsPressed(Keys.M);
        bool p = Input.IsPressed(Keys.P);
        bool r = Input.IsPressed(Keys.R);

        if (m && !prevM) LoadPalette();
        if (p && !prevP) ResetCanvas(canvasW, canvasH);
        if (r && !prevR) ExportCanvas();

        prevM = m; prevP = p; prevR = r;

        // ---- Mouse ----
        bool lmb = Input.IsPressed(MouseButtons.Left);
        bool rmb = Input.IsPressed(MouseButtons.Right);

        // mx/my already captured above for pan; reuse them
        bool firstFrame = lmb && !prevLeft;

        if (lmb)
        {
            if (mx < PANEL_W && firstFrame)
                PickPaletteColour(mx, my);
            else if (OnCanvas(mx, my))
            {
                if (firstFrame) PushUndo();
                ApplyTool(mx, my, firstFrame);
            }
        }

        if (rmb && OnCanvas(mx, my))
        {
            if (!prevRight) PushUndo();
            int cx = (mx - OriginX) / zoom;
            int cy = (my - OriginY) / zoom;
            PaintAt(cx, cy, 0xFF);
        }

        prevLeft  = lmb;
        prevRight = rmb;

        if (statusTimer > 0) statusTimer--;
    }

    // =========================================================================
    // Tools
    // =========================================================================

    private bool OnCanvas(int mx, int my)
    {
        // Must be inside the display area AND over a valid canvas pixel
        if (mx < PANEL_W || mx >= Core.ScreenWidth - RIGHT_W) return false;
        if (my < TOOLBAR_H) return false;
        int cx = (mx - OriginX) / zoom;
        int cy = (my - OriginY) / zoom;
        return cx >= 0 && cx < canvasW && cy >= 0 && cy < canvasH;
    }

    private void ApplyTool(int mx, int my, bool firstFrame)
    {
        int cx = (mx - OriginX) / zoom;
        int cy = (my - OriginY) / zoom;

        switch (activeTool)
        {
            case Tool.Pencil:
                PaintAt(cx, cy, selectedColour);
                break;

            case Tool.Eraser:
                PaintAt(cx, cy, 0xFF);
                break;

            case Tool.Fill:
                // Flood fill only on the initial click, not while dragging
                if (firstFrame)
                    FloodFill(cx, cy, selectedColour);
                break;
        }
    }

    private void PaintAt(int cx, int cy, byte colour)
    {
        int half = brushSize / 2;
        for (int dy = -half; dy < brushSize - half; dy++)
            for (int dx = -half; dx < brushSize - half; dx++)
            {
                int px = cx + dx;
                int py = cy + dy;
                if (px >= 0 && px < canvasW && py >= 0 && py < canvasH)
                    canvas[py * canvasW + px] = colour;
            }
    }

    private void FloodFill(int cx, int cy, byte fillColour)
    {
        if (cx < 0 || cx >= canvasW || cy < 0 || cy >= canvasH) return;

        byte target = canvas[cy * canvasW + cx];
        if (target == fillColour) return;

        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((cx, cy));

        while (queue.Count > 0)
        {
            var (px, py) = queue.Dequeue();
            if (px < 0 || px >= canvasW || py < 0 || py >= canvasH) continue;
            if (canvas[py * canvasW + px] != target) continue;

            canvas[py * canvasW + px] = fillColour;
            queue.Enqueue((px + 1, py));
            queue.Enqueue((px - 1, py));
            queue.Enqueue((px, py + 1));
            queue.Enqueue((px, py - 1));
        }
    }

    private void PickPaletteColour(int mx, int my)
    {
        int step = SWATCH_SIZE + SWATCH_GAP;
        int col  = mx / step;
        int row  = (my - TOOLBAR_H) / step;
        int perCol = Math.Max(1, (Core.ScreenHeight - TOOLBAR_H) / step);
        int idx  = col * perCol + row;

        var spriteColours = Palette.GetColours(PaletteLayer.Sprite);
        if (idx >= 0 && idx < spriteColours.Length)
        {
            selectedColour = (byte)(idx % 256);
            Status($"Colour #{selectedColour:X2}");
        }
    }

    // =========================================================================
    // Undo
    // =========================================================================

    private void PushUndo()
    {
        if (undoStack.Count >= MAX_UNDO) undoStack.Dequeue();
        undoStack.Enqueue((byte[])canvas.Clone());
    }

    private void Undo()
    {
        if (undoStack.Count == 0) { Status("Nothing to undo"); return; }
        canvas = undoStack.Dequeue();
        Status("Undo");
    }

    // =========================================================================
    // Canvas management
    // =========================================================================

    private void ClampView()
    {
        int visW = Math.Max(1, AvailW / zoom);
        int visH = Math.Max(1, AvailH / zoom);
        viewX = Math.Clamp(viewX, 0, Math.Max(0, canvasW - visW));
        viewY = Math.Clamp(viewY, 0, Math.Max(0, canvasH - visH));
    }

    private void ResetCanvas(int w, int h)
    {
        canvasW = w; canvasH = h;
        viewX   = 0; viewY   = 0; zoom = Math.Max(MIN_ZOOM, zoom);
        canvas  = new byte[canvasW * canvasH];
        Array.Fill(canvas, (byte)0xFF);
        undoStack.Clear();
        Status($"Canvas {canvasW}x{canvasH}");
    }

    private void ResizeCanvas(int nw, int nh)
    {
        // Unconstrained from screen size now — user can zoom out to see it all
        nw = Math.Max(4, nw);
        nh = Math.Max(4, nh);

        var next = new byte[nw * nh];
        Array.Fill(next, (byte)0xFF);
        int cw = Math.Min(nw, canvasW);
        int ch = Math.Min(nh, canvasH);
        for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
                next[y * nw + x] = canvas[y * canvasW + x];

        canvasW = nw; canvasH = nh; canvas = next;
        ClampView();
        Status($"Canvas {canvasW}x{canvasH}");
    }

    // =========================================================================
    // Palette loading
    // =========================================================================

    private void LoadPalette()
    {
        var path = File.OpenFileDialogue(
            title: "Load Sprite Palette",
            defaultPath: "palette.txt",
            filter: "Palette Files (*.txt;*.hex)|*.txt;*.hex|All Files (*.*)|*.*");

        if (string.IsNullOrEmpty(path))
        {
            Status("Palette load cancelled");
            return;
        }

        try
        {
            Palette.LoadFromFile(path, PaletteLayer.Sprite);
            int count = Palette.GetColours(PaletteLayer.Sprite).Length;
            selectedColour = (byte)Math.Min((int)selectedColour, count - 1);
            Status($"Sprite palette: {count} colours");
            Logger.LogInfo($"Loaded sprite palette from '{path}' ({count} colours)");
        }
        catch (FileNotFoundException) { Status("Palette file not found"); }
        catch (Exception ex)          { Status("Palette error"); Logger.LogError(ex.Message); }
    }

    // =========================================================================
    // Export
    // =========================================================================

    private void ExportCanvas()
    {
        try
        {
            var sb   = new System.Text.StringBuilder();
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string v  = $"sprite_{ts}";

            sb.AppendLine($"// PixelDot export — {canvasW}x{canvasH} — {ts}");
            sb.AppendLine($"// 0xFF = transparent, other values index into PaletteLayer.Sprite");
            sb.AppendLine($"// for (int y=0;y<H;y++) for (int x=0;x<W;x++)");
            sb.AppendLine($"//   if ({v}[y*W+x]!=0xFF)");
            sb.AppendLine($"//     Core.DrawPixel(ox+x,oy+y,Palette.GetColour({v}[y*W+x], PaletteLayer.Sprite));");
            sb.AppendLine();
            sb.AppendLine($"int W = {canvasW}, H = {canvasH};");
            sb.AppendLine($"byte[] {v} =");
            sb.AppendLine("[");
            for (int y = 0; y < canvasH; y++)
            {
                sb.Append("    ");
                for (int x = 0; x < canvasW; x++)
                {
                    byte val = canvas[y * canvasW + x];
                    sb.Append(val == 0xFF ? "0xFF" : $"0x{val:X2}");
                    if (!(y == canvasH - 1 && x == canvasW - 1)) sb.Append(", ");
                }
                sb.AppendLine();
            }
            sb.AppendLine("];");

            var file = File.SaveFileDialogue(
                title: "Export Sprite C# File",
                defaultPath: $"export_{ts}.cs",
                filter: "C# Source Files (*.cs)|*.cs|All Files (*.*)|*.*",
                defaultExtension: "cs");

            if (string.IsNullOrEmpty(file))
            {
                Status("Export cancelled");
                return;
            }

            File.WriteAllText(file, sb.ToString());
            Logger.LogInfo($"Exported to '{file}'");
            Logger.LogInfo(sb.ToString());
            Status($"Saved {Path.GetFileName(file)}");
        }
        catch (Exception ex) { Logger.LogError(ex.Message); Status("Export failed"); }
    }

    // =========================================================================
    // Rendering
    // =========================================================================

    private void Render()
    {
        RenderBackground();
        RenderCanvas();
        RenderCanvasBorder();
        RenderPalettePanel();
        RenderRightPanel();
        RenderToolbar();      // last → draws on top of everything
    }

    private void RenderBackground()
        => Core.DrawPixel(0, 0, Palette.GetColour(0, PaletteLayer.GUI), Core.ScreenWidth, Core.ScreenHeight);

    private void RenderCanvas()
    {
        Color chkA = new(45, 45, 45);
        Color chkB = new(60, 60, 60);

        int ox = OriginX;
        int oy = OriginY;

        // Display-area clip bounds (logical px)
        int clipL = PANEL_W;
        int clipR = Core.ScreenWidth  - RIGHT_W;
        int clipT = TOOLBAR_H;
        int clipB = Core.ScreenHeight;

        for (int y = 0; y < canvasH; y++)
        {
            int sy = oy + y * zoom;
            if (sy + zoom <= clipT || sy >= clipB) continue;

            for (int x = 0; x < canvasW; x++)
            {
                int sx = ox + x * zoom;
                if (sx + zoom <= clipL || sx >= clipR) continue;

                byte idx = canvas[y * canvasW + x];
                Color c  = idx == 0xFF
                    ? ((x + y) % 2 == 0 ? chkA : chkB)
                    : Palette.GetColour(idx, PaletteLayer.Sprite);

                Core.DrawPixel(sx, sy, c, zoom, zoom);
            }
        }
    }

    private void RenderCanvasBorder()
    {
        // Border around the entire display area, not just the zoomed canvas pixels
        int ox = OriginX;
        int oy = OriginY;
        int bw = canvasW * zoom;
        int bh = canvasH * zoom;

        Color b = Palette.GetColour(5, PaletteLayer.GUI);
        Core.DrawPixel(ox - 1, oy - 1, b, bw + 2, 1);
        Core.DrawPixel(ox - 1, oy + bh, b, bw + 2, 1);
        Core.DrawPixel(ox - 1, oy,      b, 1, bh);
        Core.DrawPixel(ox + bw, oy,     b, 1, bh);

        // Grid lines when zoomed in enough to be useful
        if (zoom >= 4)
        {
            Color grid = new(70, 70, 70);
            for (int gx = 1; gx < canvasW; gx++)
                Core.DrawPixel(ox + gx * zoom - 1, oy, grid, 1, bh);
            for (int gy = 1; gy < canvasH; gy++)
                Core.DrawPixel(ox, oy + gy * zoom - 1, grid, bw, 1);
        }
    }

    private void RenderPalettePanel()
    {
        // Panel background + right divider (GUI layer)
        Core.DrawPixel(0, 0, Palette.GetColour(1, PaletteLayer.GUI), PANEL_W, Core.ScreenHeight);
        Core.DrawPixel(PANEL_W, 0, Palette.GetColour(2, PaletteLayer.GUI), 1, Core.ScreenHeight);

        // Selected-colour preview strip inside toolbar area (Sprite layer)
        Core.DrawPixel(1, 1, Palette.GetColour(selectedColour, PaletteLayer.Sprite), PANEL_W - 2, TOOLBAR_H - 2);

        // Palette swatches (Sprite layer)
        int step    = SWATCH_SIZE + SWATCH_GAP;
        int perCol  = Math.Max(1, (Core.ScreenHeight - TOOLBAR_H) / step);
        var spriteColours = Palette.GetColours(PaletteLayer.Sprite);

        for (int i = 0; i < spriteColours.Length; i++)
        {
            int col = i / perCol;
            int row = i % perCol;
            int sx  = col * step;
            int sy  = TOOLBAR_H + row * step;

            if (sx + SWATCH_SIZE > PANEL_W) break;

            Core.DrawPixel(sx, sy, spriteColours[i], SWATCH_SIZE, SWATCH_SIZE);

            // Highlight the selected swatch with a 1px white border (GUI layer)
            if (i == selectedColour)
            {
                Color hl = Palette.GetColour(5, PaletteLayer.GUI);
                Core.DrawPixel(sx - 1, sy - 1, hl, SWATCH_SIZE + 2, 1);
                Core.DrawPixel(sx - 1, sy + SWATCH_SIZE, hl, SWATCH_SIZE + 2, 1);
                Core.DrawPixel(sx - 1, sy, hl, 1, SWATCH_SIZE);
                Core.DrawPixel(sx + SWATCH_SIZE, sy, hl, 1, SWATCH_SIZE);
            }
        }
    }

    private void RenderRightPanel()
    {
        int px = Core.ScreenWidth - RIGHT_W;

        // Panel background + left divider (GUI layer)
        Core.DrawPixel(px, 0, Palette.GetColour(1, PaletteLayer.GUI), RIGHT_W, Core.ScreenHeight);
        Core.DrawPixel(px, 0, Palette.GetColour(2, PaletteLayer.GUI), 1, Core.ScreenHeight);

        Color bright = Palette.GetColour(5, PaletteLayer.GUI);
        Color dim    = Palette.GetColour(3, PaletteLayer.GUI);

        int tx = px + 3;
        int ty = TOOLBAR_H + 3;

        // ---- State (font 7) ----
        Text.SetSize(7);
        string toolLabel = activeTool switch
        {
            Tool.Eraser => "Eraser",
            Tool.Fill   => "Fill",
            _           => "Pencil",
        };
        Text.Draw($"Tool:  {toolLabel}",        tx, ty, bright); ty += 10;
        Text.Draw($"Brush: {brushSize}",         tx, ty, bright); ty += 10;
        Text.Draw($"W:{canvasW} H:{canvasH}",    tx, ty, bright); ty += 13;

        // ---- Divider ----
        Core.DrawPixel(px + 2, ty, Palette.GetColour(2, PaletteLayer.GUI), RIGHT_W - 4, 1); ty += 5;

        // ---- Key guide (font 6 — smaller, guaranteed to fit) ----
        Text.SetSize(6);
        Text.Draw("Z   Pencil",  tx, ty, dim); ty += 9;
        Text.Draw("X   Eraser",  tx, ty, dim); ty += 9;
        Text.Draw("C   Fill",    tx, ty, dim); ty += 13;

        Text.Draw("Q   Brush-",  tx, ty, dim); ty += 9;
        Text.Draw("W   Brush+",  tx, ty, dim); ty += 13;

        Text.Draw("Up  Grow",    tx, ty, dim); ty += 9;
        Text.Draw("Dn  Shrink",  tx, ty, dim); ty += 13;

        Text.Draw("M   Palette", tx, ty, dim); ty += 9;
        Text.Draw("P   Clear",   tx, ty, dim); ty += 9;
        Text.Draw("R   Export",  tx, ty, dim); ty += 13;

        Text.Draw("Sh+Z  Undo",  tx, ty, dim); ty += 9;
        Text.Draw("RMB   Erase", tx, ty, dim);

        // Restore size for toolbar
        Text.SetSize(7);
    }

    private void RenderToolbar()
    {
        Core.DrawPixel(0, 0, Palette.GetColour(1, PaletteLayer.GUI), Core.ScreenWidth, TOOLBAR_H);
        Core.DrawPixel(0, TOOLBAR_H, Palette.GetColour(2, PaletteLayer.GUI), Core.ScreenWidth, 1);

        string title = statusTimer > 0
            ? statusMsg
            : $"PixelArtPainter  {canvasW}x{canvasH}  #{selectedColour:X2}  Brush {brushSize}";

        Text.SetSize(7);
        Text.Draw(title, PANEL_W + 3, 2, Palette.GetColour(5, PaletteLayer.GUI));
    }

    // =========================================================================
    // Utilities
    // =========================================================================

    private void Status(string msg, int frames = 180)
    {
        statusMsg   = msg;
        statusTimer = frames;
    }
}
