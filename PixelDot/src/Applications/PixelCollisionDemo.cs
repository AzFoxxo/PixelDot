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
using PixelDot.Collision;

namespace Applications;

/// <summary>
/// Interactive visual demonstration of the PixelDot Pixel-Perfect 2D Collision System.
/// Features controllable player, hollow shapes, star asteroids, trigger zones, broad/narrow phase visualizers,
/// and live lifecycle event tracking.
/// </summary>
[RunnableApplication("Pixel Collision Demo", "1.0.0", "Az Foxxo", "Interactive visual demonstration of pixel-perfect 2D collisions")]
public class PixelCollisionDemo : Application
{
    private CollisionManager _collisionManager = null!;

    private PixelDataCollider _player = null!;
    private PixelDataCollider _hollowRing = null!;
    private PixelDataCollider _starAsteroid = null!;
    private PixelDataCollider _patrolEnemy = null!;
    private PixelDataCollider _gemTrigger = null!;

    private PixelData _playerData = null!;
    private PixelData _ringData = null!;
    private PixelData _starData = null!;
    private PixelData _enemyData = null!;
    private PixelData _gemData = null!;

    private int _enemyDir = 1;
    private int _gemPulse;
    private readonly List<string> _eventLog = [];
    private readonly List<CollisionResult> _currentCollisions = [];

    // Stats
    private int _collisionEnterCount;
    private int _triggerEnterCount;

    public override void OnInit()
    {
        Text.SetFont("Regular");
        Text.SetSize(7);

        _collisionManager = new CollisionManager();

        // 1. Create Hollow Ring Sprite (14x14 hollow circle)
        // Demonstrates that objects can overlap in broad phase and enter the hole with 0 pixel collision!
        _ringData = CreateHollowRingData(14, Palette.GetColour(8, PaletteLayer.GUI));
        _hollowRing = new PixelDataCollider("HollowRing", 50, 30, _ringData)
        {
            Layer = CollisionLayer.Environment,
            CollisionMask = CollisionLayer.All
        };

        // 2. Create Star Asteroid (13x13 diamond/star shape)
        // Demonstrates non-rectangular shapes passing through corner bounding boxes without colliding
        _starData = CreateStarData(13, Palette.GetColour(29, PaletteLayer.GUI));
        _starAsteroid = new PixelDataCollider("Star", 85, 25, _starData)
        {
            Layer = CollisionLayer.Environment,
            CollisionMask = CollisionLayer.All
        };

        // 3. Create Gem Pickup Trigger (9x9 diamond)
        _gemData = CreateGemData(9, Palette.GetColour(46, PaletteLayer.GUI));
        _gemTrigger = new PixelDataCollider("GemTrigger", 120, 32, _gemData)
        {
            Layer = CollisionLayer.Trigger,
            CollisionMask = CollisionLayer.All,
            IsTrigger = true
        };

        // 4. Create Patrolling Enemy (10x8 alien)
        _enemyData = CreateEnemyData(10, 8, Palette.GetColour(23, PaletteLayer.GUI));
        _patrolEnemy = new PixelDataCollider("Enemy", 30, 60, _enemyData)
        {
            Layer = CollisionLayer.Enemy,
            CollisionMask = CollisionLayer.Player | CollisionLayer.Projectile
        };

        // 5. Create Player Ship (10x10 triangular arrow with transparent notch)
        _playerData = CreateShipData(10, 10, Palette.GetColour(4, PaletteLayer.GUI));
        _player = new PixelDataCollider("Player", 15, 30, _playerData)
        {
            Layer = CollisionLayer.Player,
            CollisionMask = CollisionLayer.All
        };

        // Register all colliders with manager
        _collisionManager.Add(_player);
        _collisionManager.Add(_hollowRing);
        _collisionManager.Add(_starAsteroid);
        _collisionManager.Add(_gemTrigger);
        _collisionManager.Add(_patrolEnemy);

        // Subscribe to lifecycle events
        _collisionManager.OnCollisionEnter += res =>
        {
            _collisionEnterCount++;
            AddLog($"[ENTER] {res.ColliderA.Name} <-> {res.ColliderB.Name} ({res.OverlappingPixelCount} px)");
        };

        _collisionManager.OnCollisionExit += res =>
        {
            AddLog($"[EXIT] {res.ColliderA.Name} <-> {res.ColliderB.Name}");
        };

        _collisionManager.OnTriggerEnter += res =>
        {
            _triggerEnterCount++;
            AddLog($"[TRIGGER ENTER] {res.ColliderA.Name} <-> {res.ColliderB.Name}");
        };

        _collisionManager.OnTriggerExit += res =>
        {
            AddLog($"[TRIGGER EXIT] {res.ColliderA.Name} <-> {res.ColliderB.Name}");
        };

        AddLog("Collision System Ready. Use WASD/Arrows to move.");
    }

    public override void OnUpdate()
    {
        HandlePlayerInput();
        UpdateEnemyPatrol();

        // Run collision detection cycle
        _collisionManager.Update();

        // Cache active collisions for rendering
        _currentCollisions.Clear();
        foreach (var col in _collisionManager.GetCollisions(_player))
        {
            _currentCollisions.Add(col);
        }

        // Render scene
        RenderScene();
    }

    private void HandlePlayerInput()
    {
        var speed = 1;

        if (Input.IsHeld(Keys.Left) || Input.IsHeld(Keys.KeyboardA) || Input.IsHeld(Keys.ArrowLeft))
        {
            _player.X -= speed;
        }
        if (Input.IsHeld(Keys.Right) || Input.IsHeld(Keys.KeyboardD) || Input.IsHeld(Keys.ArrowRight))
        {
            _player.X += speed;
        }
        if (Input.IsHeld(Keys.Up) || Input.IsHeld(Keys.KeyboardW) || Input.IsHeld(Keys.ArrowUp))
        {
            _player.Y -= speed;
        }
        if (Input.IsHeld(Keys.Down) || Input.IsHeld(Keys.KeyboardS) || Input.IsHeld(Keys.ArrowDown))
        {
            _player.Y += speed;
        }

        // Clamp inside screen bounds
        _player.X = Math.Clamp(_player.X, 2, Core.ScreenWidth - _player.Width - 2);
        _player.Y = Math.Clamp(_player.Y, 12, Core.ScreenHeight - _player.Height - 20);

        // Key 'T': Toggle player trigger mode
        if (Input.IsPressed(Keys.KeyboardT))
        {
            _player.IsTrigger = !_player.IsTrigger;
            AddLog($"Player Trigger Mode: {_player.IsTrigger}");
        }

        // Key 'R': Reset player position
        if (Input.IsPressed(Keys.KeyboardR) || Input.IsPressed(Keys.R))
        {
            _player.X = 15;
            _player.Y = 30;
            AddLog("Player position reset.");
        }
    }

    private void UpdateEnemyPatrol()
    {
        _patrolEnemy.X += _enemyDir;
        if (_patrolEnemy.X > 120)
        {
            _enemyDir = -1;
        }
        else if (_patrolEnemy.X < 20)
        {
            _enemyDir = 1;
        }

        _gemPulse = (_gemPulse + 1) % 60;
    }

    private void RenderScene()
    {
        // 1. Background grid
        var bg = Palette.GetColour(0, PaletteLayer.GUI);
        Core.DrawRectangle(0, 0, Core.ScreenWidth, Core.ScreenHeight, bg, 1, true);

        // Header Toolbar
        Core.DrawRectangle(0, 0, Core.ScreenWidth, 11, Palette.GetColour(1, PaletteLayer.GUI), 1, true);
        Core.DrawPixel(0, 11, Palette.GetColour(2, PaletteLayer.GUI), Core.ScreenWidth, 1);
        Text.Draw("PixelDot Collision System Demo", 4, 2, Palette.GetColour(5, PaletteLayer.GUI));
        Text.Draw($"Collisions: {_collisionEnterCount} | Triggers: {_triggerEnterCount}", Core.ScreenWidth - 4, 2, Text.Right, Palette.GetColour(4, PaletteLayer.GUI));

        // 2. Draw Colliders and Broad-Phase Bounding Boxes
        DrawColliderWithBounds(_hollowRing, _ringData);
        DrawColliderWithBounds(_starAsteroid, _starData);
        DrawColliderWithBounds(_gemTrigger, _gemData);
        DrawColliderWithBounds(_patrolEnemy, _enemyData);

        // Player (color changes if colliding or trigger mode)
        var playerCol = _player.IsTrigger
            ? Palette.GetColour(37, PaletteLayer.GUI)
            : (_currentCollisions.Count > 0 ? Palette.GetColour(20, PaletteLayer.GUI) : Palette.GetColour(5, PaletteLayer.GUI));

        DrawColliderPixels(_player, _playerData, playerCol);
        DrawBoundingBox(_player, _currentCollisions.Count > 0 ? Palette.GetColour(23, PaletteLayer.GUI) : Palette.GetColour(2, PaletteLayer.GUI));

        // 3. Highlight Exact Pixel-Perfect Overlaps
        foreach (var hit in _currentCollisions)
        {
            // Draw overlap bounding box in bright outline
            var b = hit.OverlapBounds;
            Core.DrawRectangle(b.X, b.Y, b.Width, b.Height, Palette.GetColour(23, PaletteLayer.GUI), 1, false);

            // Highlight overlapping pixels in blinking red/yellow
            var flashColor = (_gemPulse / 10 % 2 == 0)
                ? Palette.GetColour(23, PaletteLayer.GUI)
                : Palette.GetColour(36, PaletteLayer.GUI);

            for (var y = b.Top; y < b.Bottom; y++)
            {
                for (var x = b.Left; x < b.Right; x++)
                {
                    if (_player.IsSolidWorld(x, y) && hit.GetOther(_player).IsSolidWorld(x, y))
                    {
                        Core.DrawPixel(x, y, flashColor);
                    }
                }
            }
        }

        // 4. Render Event Log Window at bottom
        RenderEventLog();

        // 5. Controls help
        Text.Draw("[WASD/Arrows] Move | [T] Trigger Mode | [R] Reset", 4, Core.ScreenHeight - 9, Palette.GetColour(3, PaletteLayer.GUI));
    }

    private void DrawColliderWithBounds(PixelDataCollider collider, PixelData data)
    {
        DrawColliderPixels(collider, data);
        DrawBoundingBox(collider, Palette.GetColour(1, PaletteLayer.GUI));
    }

    private static void DrawColliderPixels(PixelDataCollider collider, PixelData data, Color? overrideColor = null)
    {
        for (var y = 0; y < collider.Height; y++)
        {
            for (var x = 0; x < collider.Width; x++)
            {
                if (collider.IsSolid(x, y))
                {
                    var col = overrideColor ?? data.GetPixel(x, y);
                    Core.DrawPixel(collider.WorldX + x, collider.WorldY + y, col);
                }
            }
        }
    }

    private static void DrawBoundingBox(PixelDataCollider collider, Color color)
    {
        // 1px faint bounding box to show broad-phase bounds
        Core.DrawRectangle(collider.WorldX - 1, collider.WorldY - 1, collider.Width + 2, collider.Height + 2, color, 1, false);
    }

    private void RenderEventLog()
    {
        var logY = Core.ScreenHeight - 27;
        Core.DrawRectangle(0, logY - 1, Core.ScreenWidth, 17, Palette.GetColour(1, PaletteLayer.GUI), 1, true);
        Core.DrawPixel(0, logY - 1, Palette.GetColour(2, PaletteLayer.GUI), Core.ScreenWidth, 1);

        var start = Math.Max(0, _eventLog.Count - 2);
        var lineY = logY + 1;
        for (var i = start; i < _eventLog.Count; i++)
        {
            var msg = _eventLog[i];
            var col = msg.StartsWith("[ENTER]")
                ? Palette.GetColour(20, PaletteLayer.GUI)
                : (msg.StartsWith("[TRIGGER") ? Palette.GetColour(46, PaletteLayer.GUI) : Palette.GetColour(4, PaletteLayer.GUI));

            Text.Draw(msg, 4, lineY, col);
            lineY += 7;
        }
    }

    private void AddLog(string message)
    {
        _eventLog.Add(message);
        if (_eventLog.Count > 20)
        {
            _eventLog.RemoveAt(0);
        }
        Logger.LogInfo(message);
    }

    #region Shape Generators

    private static PixelData CreateHollowRingData(int size, Color color)
    {
        var data = new PixelData(size, size);
        var center = (size - 1) / 2f;
        var rOuter = size / 2f;
        var rInner = rOuter - 2.5f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist <= rOuter && dist >= rInner)
                {
                    data.SetPixel(x, y, color);
                }
            }
        }
        return data;
    }

    private static PixelData CreateStarData(int size, Color color)
    {
        var data = new PixelData(size, size);
        var mid = size / 2;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var manhattan = Math.Abs(x - mid) + Math.Abs(y - mid);
                if (manhattan <= mid)
                {
                    data.SetPixel(x, y, color);
                }
            }
        }
        return data;
    }

    private static PixelData CreateGemData(int size, Color color)
    {
        var data = new PixelData(size, size);
        var mid = size / 2;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (Math.Abs(x - mid) + Math.Abs(y - mid) <= mid)
                {
                    data.SetPixel(x, y, color);
                }
            }
        }
        return data;
    }

    private static PixelData CreateEnemyData(int w, int h, Color color)
    {
        var data = new PixelData(w, h);
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                // Space invader style shape
                var solid = (y >= 2 && y <= 5) ||
                            (y == 0 && (x == 2 || x == 7)) ||
                            (y == 1 && (x == 3 || x == 6)) ||
                            (y == 6 && (x == 1 || x == 8 || x == 3 || x == 6)) ||
                            (y == 7 && (x == 0 || x == 9));

                if (solid && !(y == 3 && (x == 3 || x == 6))) // Eye holes
                {
                    data.SetPixel(x, y, color);
                }
            }
        }
        return data;
    }

    private static PixelData CreateShipData(int w, int h, Color color)
    {
        var data = new PixelData(w, h);
        var mid = w / 2;

        for (var y = 0; y < h; y++)
        {
            var halfW = (y + 1) / 2;
            for (var x = mid - halfW; x <= mid + halfW; x++)
            {
                if (x >= 0 && x < w)
                {
                    // Back notch cutout
                    if (y >= h - 2 && x >= mid - 1 && x <= mid + 1)
                    {
                        continue;
                    }
                    data.SetPixel(x, y, color);
                }
            }
        }
        return data;
    }

    #endregion
}
