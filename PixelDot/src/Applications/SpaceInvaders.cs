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
/// Classic Arcade Space Invaders demo featuring:
/// - Pixel-perfect collisions for player, invaders, missiles, and bombs
/// - Destructible pixel-perfect bunkers with dynamic crater carving
/// - Animated multi-tier alien swarm that speeds up as it dwindles
/// - Mystery flying saucer (UFO) with bonus score
/// - Full lifecycle collision and trigger event management via <see cref="CollisionManager"/>
/// </summary>
[RunnableApplication("Space Invaders", "1.0.0", "PixelDot", "Classic arcade Space Invaders with pixel-perfect collisions and destructible bunkers")]
public class SpaceInvaders : Application
{
    private enum GameState
    {
        Title,
        Playing,
        PlayerDying,
        WaveCleared,
        GameOver
    }

    private enum InvaderType
    {
        Squid = 0,   // Top row: 30 pts
        Crab = 1,    // Middle rows: 20 pts
        Octopus = 2  // Bottom rows: 10 pts
    }

    private sealed class Invader
    {
        public InvaderType Type;
        public int Row;
        public int Col;
        public float RelX;
        public float RelY;
        public bool Alive = true;
        public PixelDataCollider Collider = null!;
    }

    private sealed class Bunker
    {
        public int X;
        public int Y;
        public PixelData Data = null!;
        public PixelDataCollider Collider = null!;
    }

    private sealed class Projectile
    {
        public float X;
        public float Y;
        public float SpeedY;
        public bool IsPlayer;
        public bool Alive = true;
        public PixelData Data = null!;
        public PixelDataCollider Collider = null!;
    }

    private sealed class Explosion
    {
        public float X;
        public float Y;
        public int Timer;
        public Color Color;
    }

    private sealed class Ufo
    {
        public float X;
        public float Y;
        public float Speed;
        public bool Active;
        public PixelData Data = null!;
        public PixelDataCollider Collider = null!;
    }

    // -------------------------------------------------------------------------
    // Core Managers & State
    // -------------------------------------------------------------------------
    private CollisionManager _collision = null!;
    private GameState _state = GameState.Title;

    private int _score;
    private int _highScore = 1000;
    private int _lives = 3;
    private int _wave = 1;

    // Player
    private float _playerX;
    private float _playerY;
    private const float PlayerSpeed = 1.35f;
    private PixelData _playerData = null!;
    private PixelDataCollider _playerCollider = null!;
    private int _deathTimer;

    // Invader Swarm
    private readonly List<Invader> _invaders = [];
    private float _swarmX;
    private float _swarmY;
    private float _swarmSpeedX = 0.5f;
    private int _swarmDirection = 1;
    private int _animFrame;
    private int _stepTimer;
    private int _invaderShotCooldown;

    // Projectiles & Entities
    private readonly List<Projectile> _projectiles = [];
    private readonly List<Bunker> _bunkers = [];
    private readonly List<Explosion> _explosions = [];
    private Ufo _ufo = null!;
    private int _ufoSpawnTimer;

    // Sprites & Animations
    private PixelData[][] _invaderFrames = null!;
    private PixelData _ufoData = null!;
    private PixelData _laserData = null!;
    private PixelData _bombData = null!;

    // Visual Palette Cache
    private Color _colPlayer;
    private Color _colInvaderSquid;
    private Color _colInvaderCrab;
    private Color _colInvaderOctopus;
    private Color _colBunker;
    private Color _colUfo;
    private Color _colLaser;
    private Color _colBomb;
    private Color _colText;
    private Color _colHudDim;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void OnInit()
    {
        Text.SetFont("Regular");
        Text.SetSize(7);

        InitColors();
        InitSprites();

        _collision = new CollisionManager();

        // Setup Collision Event Handlers
        _collision.OnCollisionEnter += HandleCollisionEnter;
        _collision.OnTriggerEnter += HandleTriggerEnter;

        StartGame();
        _state = GameState.Title;
    }

    public override void OnUpdate()
    {
        HandleGlobalInput();

        switch (_state)
        {
            case GameState.Title:
                UpdateTitle();
                break;
            case GameState.Playing:
                UpdatePlaying();
                break;
            case GameState.PlayerDying:
                UpdatePlayerDying();
                break;
            case GameState.WaveCleared:
                UpdateWaveCleared();
                break;
            case GameState.GameOver:
                UpdateGameOver();
                break;
        }

        RenderGame();
    }

    // -------------------------------------------------------------------------
    // Initialisation Helpers
    // -------------------------------------------------------------------------

    private void InitColors()
    {
        _colPlayer = Palette.GetColour(46, PaletteLayer.GUI);         // Cyan green
        _colInvaderSquid = Palette.GetColour(29, PaletteLayer.GUI);    // Gold yellow
        _colInvaderCrab = Palette.GetColour(49, PaletteLayer.GUI);     // Mint green
        _colInvaderOctopus = Palette.GetColour(20, PaletteLayer.GUI);  // Magenta pink
        _colBunker = Palette.GetColour(44, PaletteLayer.GUI);          // Shield green
        _colUfo = Palette.GetColour(23, PaletteLayer.GUI);             // Bright crimson
        _colLaser = Palette.GetColour(9, PaletteLayer.GUI);            // Electric cyan
        _colBomb = Palette.GetColour(28, PaletteLayer.GUI);            // Orange/red
        _colText = Palette.GetColour(5, PaletteLayer.GUI);             // Pure white
        _colHudDim = Palette.GetColour(3, PaletteLayer.GUI);           // Dim white
    }

    private void InitSprites()
    {
        // Player Ship (11x7)
        _playerData = CreatePixelDataFromAscii(
            11, 7, _colPlayer,
            ".....X.....",
            "....XXX....",
            "....XXX....",
            ".XXXXXXXXX.",
            "XXXXXXXXXXX",
            "XXXXXXXXXXX",
            "XXXXXXXXXXX"
        );

        // Invader 0: Squid (8x8)
        _invaderFrames = new PixelData[3][];
        _invaderFrames[0] =
        [
            CreatePixelDataFromAscii(8, 8, _colInvaderSquid,
                "...XX...",
                "..XXXX..",
                ".XXXXXX.",
                "XX.XX.XX",
                "XXXXXXXX",
                "..X..X..",
                ".X.XX.X.",
                "X.X..X.X"),
            CreatePixelDataFromAscii(8, 8, _colInvaderSquid,
                "...XX...",
                "..XXXX..",
                ".XXXXXX.",
                "XX.XX.XX",
                "XXXXXXXX",
                ".X.XX.X.",
                "X......X",
                ".X....X.")
        ];

        // Invader 1: Crab (11x8)
        _invaderFrames[1] =
        [
            CreatePixelDataFromAscii(11, 8, _colInvaderCrab,
                "..X.....X..",
                "...X...X...",
                "..XXXXXXX..",
                ".XX.XXX.XX.",
                "XXXXXXXXXXX",
                "X.XXXXXXX.X",
                "X.X.....X.X",
                "...XX.XX..."),
            CreatePixelDataFromAscii(11, 8, _colInvaderCrab,
                "..X.....X..",
                "X..X...X..X",
                "X.XXXXXXX.X",
                "XXX.XXX.XXX",
                "XXXXXXXXXXX",
                ".XXXXXXXXX.",
                "..X.....X..",
                ".X.......X.")
        ];

        // Invader 2: Octopus (12x8)
        _invaderFrames[2] =
        [
            CreatePixelDataFromAscii(12, 8, _colInvaderOctopus,
                "....XXXX....",
                "..XXXXXXXX..",
                ".XXXXXXXXXX.",
                "XX..XXXX..XX",
                "XXXXXXXXXXXX",
                "...XX..XX...",
                "..XX.XX.XX..",
                ".XX......XX."),
            CreatePixelDataFromAscii(12, 8, _colInvaderOctopus,
                "....XXXX....",
                "..XXXXXXXX..",
                ".XXXXXXXXXX.",
                "XX..XXXX..XX",
                "XXXXXXXXXXXX",
                "..X.XXXX.X..",
                ".X..X..X..X.",
                "..XX....XX..")
        ];

        // UFO (16x7)
        _ufoData = CreatePixelDataFromAscii(16, 7, _colUfo,
            ".....XXXXXX.....",
            "...XXXXXXXXXX...",
            "..XXXXXXXXXXXX..",
            ".XX.XX.XX.XX.XX.",
            "XXXXXXXXXXXXXXXX",
            "...XXX..XXX.....",
            "....X....X......"
        );

        // Projectiles (1x4 and 2x5)
        _laserData = PixelData.CreateSolid(1, 4, _colLaser);
        _bombData = CreatePixelDataFromAscii(2, 5, _colBomb,
            ".X",
            "X.",
            ".X",
            "X.",
            ".X"
        );
    }

    private void StartGame()
    {
        _score = 0;
        _lives = 3;
        _wave = 1;
        SetupWave();
    }

    private void SetupWave()
    {
        _collision.Clear();
        _projectiles.Clear();
        _explosions.Clear();
        _invaders.Clear();

        // 1. Setup Player
        _playerX = (Core.ScreenWidth - 11) / 2f;
        _playerY = Core.ScreenHeight - 16;
        _playerCollider = new PixelDataCollider("Player", (int)_playerX, (int)_playerY, _playerData)
        {
            Layer = CollisionLayer.Player,
            CollisionMask = CollisionLayer.Enemy | CollisionLayer.Trigger
        };
        _collision.Add(_playerCollider);

        // 2. Setup Invader Formation (5 columns x 4 rows)
        _swarmX = 14;
        _swarmY = 22;
        _swarmDirection = 1;
        _swarmSpeedX = 0.45f + (_wave * 0.08f);
        _stepTimer = 0;
        _animFrame = 0;
        _invaderShotCooldown = 40;

        const int cols = 6;
        const int rows = 4;
        const int spacingX = 15;
        const int spacingY = 11;

        for (var r = 0; r < rows; r++)
        {
            var type = r == 0 ? InvaderType.Squid : (r < 3 ? InvaderType.Crab : InvaderType.Octopus);
            for (var c = 0; c < cols; c++)
            {
                var inv = new Invader
                {
                    Type = type,
                    Row = r,
                    Col = c,
                    RelX = c * spacingX,
                    RelY = r * spacingY,
                    Alive = true
                };

                var frameData = _invaderFrames[(int)type][0];
                inv.Collider = new PixelDataCollider($"Invader_{r}_{c}", (int)(_swarmX + inv.RelX), (int)(_swarmY + inv.RelY), frameData)
                {
                    Layer = CollisionLayer.Enemy,
                    CollisionMask = CollisionLayer.Player | CollisionLayer.Projectile,
                    Tag = inv
                };

                _invaders.Add(inv);
                _collision.Add(inv.Collider);
            }
        }

        // 3. Setup Bunkers (3 destructible shields)
        _bunkers.Clear();
        var bunkerCount = 3;
        var bunkerW = 16;
        var bunkerH = 10;
        var totalBunkerSpan = Core.ScreenWidth - 20;
        var gap = (totalBunkerSpan - (bunkerCount * bunkerW)) / (bunkerCount - 1);

        for (var i = 0; i < bunkerCount; i++)
        {
            var bx = 10 + (i * (bunkerW + gap));
            var by = Core.ScreenHeight - 30;
            var bunkerData = CreateBunkerData(bunkerW, bunkerH, _colBunker);

            var bunker = new Bunker
            {
                X = bx,
                Y = by,
                Data = bunkerData,
                Collider = new PixelDataCollider($"Bunker_{i}", bx, by, bunkerData)
                {
                    Layer = CollisionLayer.Environment,
                    CollisionMask = CollisionLayer.Projectile | CollisionLayer.Trigger,
                    Tag = null
                }
            };
            bunker.Collider.Tag = bunker;

            _bunkers.Add(bunker);
            _collision.Add(bunker.Collider);
        }

        // 4. Setup UFO
        _ufo = new Ufo
        {
            X = -20,
            Y = 14,
            Speed = 1.0f,
            Active = false,
            Data = _ufoData,
            Collider = new PixelDataCollider("UFO", -30, 14, _ufoData)
            {
                Layer = CollisionLayer.Enemy,
                CollisionMask = CollisionLayer.Projectile,
                Enabled = false
            }
        };
        _ufo.Collider.Tag = _ufo;
        _collision.Add(_ufo.Collider);
        _ufoSpawnTimer = 400;
    }

    // -------------------------------------------------------------------------
    // Game Updates
    // -------------------------------------------------------------------------

    private void HandleGlobalInput()
    {
        if (Input.IsPressed(Keys.Enter) || Input.IsPressed(Keys.Start) || Input.IsPressed(Keys.KeyboardEnter))
        {
            if (_state is GameState.Title or GameState.GameOver)
            {
                StartGame();
                _state = GameState.Playing;
            }
        }
    }

    private void UpdateTitle()
    {
        // Animate title screen invaders
        if (++_stepTimer >= 30)
        {
            _stepTimer = 0;
            _animFrame = 1 - _animFrame;
        }
    }

    private void UpdatePlaying()
    {
        UpdatePlayerMovement();
        UpdateSwarm();
        UpdateUfo();
        UpdateProjectiles();
        UpdateExplosions();

        // Run Pixel Collision Engine
        _collision.Update();

        // Check wave clear
        if (_invaders.All(inv => !inv.Alive))
        {
            _state = GameState.WaveCleared;
            _deathTimer = 60;
        }

        // Update High Score
        if (_score > _highScore)
        {
            _highScore = _score;
        }
    }

    private void UpdatePlayerMovement()
    {
        // Left / Right controls
        if (Input.IsHeld(Keys.Left) || Input.IsHeld(Keys.KeyboardA) || Input.IsHeld(Keys.ArrowLeft))
        {
            _playerX -= PlayerSpeed;
        }
        if (Input.IsHeld(Keys.Right) || Input.IsHeld(Keys.KeyboardD) || Input.IsHeld(Keys.ArrowRight))
        {
            _playerX += PlayerSpeed;
        }

        _playerX = Math.Clamp(_playerX, 4, Core.ScreenWidth - 11 - 4);
        _playerCollider.X = (int)_playerX;
        _playerCollider.Y = (int)_playerY;

        // Shoot player laser (limit to 1 active player laser at a time like classic arcade)
        var hasActiveLaser = _projectiles.Any(p => p.IsPlayer && p.Alive);
        if (!hasActiveLaser && (Input.IsPressed(Keys.Space) || Input.IsPressed(Keys.Jump) || Input.IsPressed(Keys.KeyboardSpace) || Input.IsPressed(Keys.KeyboardW)))
        {
            SpawnPlayerLaser();
        }
    }

    private void SpawnPlayerLaser()
    {
        var lx = (int)(_playerX + 5);
        var ly = (int)(_playerY - 4);

        var laser = new Projectile
        {
            X = lx,
            Y = ly,
            SpeedY = -3.2f,
            IsPlayer = true,
            Alive = true,
            Data = _laserData,
            Collider = new PixelDataCollider("PlayerLaser", lx, ly, _laserData)
            {
                Layer = CollisionLayer.Projectile,
                CollisionMask = CollisionLayer.Enemy | CollisionLayer.Environment
            }
        };
        laser.Collider.Tag = laser;

        _projectiles.Add(laser);
        _collision.Add(laser.Collider);
    }

    private void UpdateSwarm()
    {
        var aliveCount = _invaders.Count(inv => inv.Alive);
        if (aliveCount == 0) return;

        // Speed increases dramatically as invaders are eliminated
        var speedMultiplier = 1.0f + ((_invaders.Count - aliveCount) * 0.08f);
        var currentSpeed = _swarmSpeedX * speedMultiplier;

        _swarmX += currentSpeed * _swarmDirection;

        // Check horizontal screen boundaries of living invaders
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;

        foreach (var inv in _invaders)
        {
            if (!inv.Alive) continue;
            var ix = _swarmX + inv.RelX;
            var iy = _swarmY + inv.RelY;
            if (ix < minX) minX = ix;
            if (ix + inv.Collider.Width > maxX) maxX = ix + inv.Collider.Width;
            if (iy + inv.Collider.Height > maxY) maxY = iy + inv.Collider.Height;

            // Update collider coordinates
            inv.Collider.X = (int)ix;
            inv.Collider.Y = (int)iy;
        }

        // Drop down and reverse if hitting walls
        if (maxX >= Core.ScreenWidth - 4 && _swarmDirection > 0)
        {
            _swarmDirection = -1;
            _swarmY += 4;
            _animFrame = 1 - _animFrame;
        }
        else if (minX <= 4 && _swarmDirection < 0)
        {
            _swarmDirection = 1;
            _swarmY += 4;
            _animFrame = 1 - _animFrame;
        }

        // Alien animation toggle
        if (++_stepTimer >= Math.Max(8, 36 - (_invaders.Count - aliveCount)))
        {
            _stepTimer = 0;
            _animFrame = 1 - _animFrame;
            foreach (var inv in _invaders)
            {
                if (inv.Alive)
                {
                    inv.Collider.LoadPixelData(_invaderFrames[(int)inv.Type][_animFrame]);
                }
            }
        }

        // Invaders reached bunker/player level -> Game Over!
        if (maxY >= _playerY - 2)
        {
            TriggerPlayerDeath();
            _lives = 0;
            _state = GameState.GameOver;
            return;
        }

        // Enemy Bomb Dropping
        if (--_invaderShotCooldown <= 0)
        {
            _invaderShotCooldown = Math.Max(25, 60 - (_wave * 5));
            DropRandomEnemyBomb();
        }
    }

    private void DropRandomEnemyBomb()
    {
        // Pick the bottom-most alive invader from a random column
        var aliveColumns = _invaders
            .Where(inv => inv.Alive)
            .GroupBy(inv => inv.Col)
            .ToList();

        if (aliveColumns.Count == 0) return;

        var randomCol = aliveColumns[Random.Shared.Next(aliveColumns.Count)];
        var bottomInvader = randomCol.OrderByDescending(inv => inv.Row).First();

        var bx = (int)(bottomInvader.Collider.WorldX + (bottomInvader.Collider.Width / 2) - 1);
        var by = bottomInvader.Collider.WorldY + bottomInvader.Collider.Height + 1;

        var bomb = new Projectile
        {
            X = bx,
            Y = by,
            SpeedY = 1.6f + (_wave * 0.15f),
            IsPlayer = false,
            Alive = true,
            Data = _bombData,
            Collider = new PixelDataCollider("EnemyBomb", bx, by, _bombData)
            {
                Layer = CollisionLayer.Trigger,
                CollisionMask = CollisionLayer.Player | CollisionLayer.Environment,
                IsTrigger = true
            }
        };
        bomb.Collider.Tag = bomb;

        _projectiles.Add(bomb);
        _collision.Add(bomb.Collider);
    }

    private void UpdateUfo()
    {
        if (_ufo.Active)
        {
            _ufo.X += _ufo.Speed;
            _ufo.Collider.X = (int)_ufo.X;

            if (_ufo.X > Core.ScreenWidth + 10)
            {
                _ufo.Active = false;
                _ufo.Collider.Enabled = false;
                _ufoSpawnTimer = Random.Shared.Next(500, 900);
            }
        }
        else
        {
            if (--_ufoSpawnTimer <= 0)
            {
                _ufo.Active = true;
                _ufo.X = -20;
                _ufo.Collider.X = (int)_ufo.X;
                _ufo.Collider.Enabled = true;
            }
        }
    }

    private void UpdateProjectiles()
    {
        for (var i = _projectiles.Count - 1; i >= 0; i--)
        {
            var p = _projectiles[i];
            if (!p.Alive)
            {
                _collision.Remove(p.Collider);
                _projectiles.RemoveAt(i);
                continue;
            }

            p.Y += p.SpeedY;
            p.Collider.X = (int)p.X;
            p.Collider.Y = (int)p.Y;

            // Screen boundary check
            if (p.Y < 12 || p.Y > Core.ScreenHeight - 2)
            {
                p.Alive = false;
                _collision.Remove(p.Collider);
                _projectiles.RemoveAt(i);
            }
        }
    }

    private void UpdateExplosions()
    {
        for (var i = _explosions.Count - 1; i >= 0; i--)
        {
            if (--_explosions[i].Timer <= 0)
            {
                _explosions.RemoveAt(i);
            }
        }
    }

    private void UpdatePlayerDying()
    {
        UpdateExplosions();
        if (--_deathTimer <= 0)
        {
            if (_lives > 0)
            {
                _playerX = (Core.ScreenWidth - 11) / 2f;
                _playerCollider.X = (int)_playerX;
                _playerCollider.Enabled = true;
                _state = GameState.Playing;
            }
            else
            {
                _state = GameState.GameOver;
            }
        }
    }

    private void UpdateWaveCleared()
    {
        UpdateExplosions();
        if (--_deathTimer <= 0)
        {
            _wave++;
            SetupWave();
            _state = GameState.Playing;
        }
    }

    private void UpdateGameOver()
    {
        UpdateExplosions();
    }

    // -------------------------------------------------------------------------
    // Collision Event Callbacks
    // -------------------------------------------------------------------------

    private void HandleCollisionEnter(CollisionResult hit)
    {
        // 1. Player Laser hitting Invader
        if (hit.ColliderA.Tag is Projectile { IsPlayer: true } pA && hit.ColliderB.Tag is Invader invB)
        {
            KillInvader(invB, pA, hit);
            return;
        }
        if (hit.ColliderB.Tag is Projectile { IsPlayer: true } pB && hit.ColliderA.Tag is Invader invA)
        {
            KillInvader(invA, pB, hit);
            return;
        }

        // 2. Player Laser hitting UFO
        if (hit.ColliderA.Tag is Projectile { IsPlayer: true } projA && hit.ColliderB.Tag is Ufo ufoB)
        {
            KillUfo(ufoB, projA, hit);
            return;
        }
        if (hit.ColliderB.Tag is Projectile { IsPlayer: true } projB && hit.ColliderA.Tag is Ufo ufoA)
        {
            KillUfo(ufoA, projB, hit);
            return;
        }

        // 3. Player Laser hitting Bunker (Destructible pixel carving!)
        if (hit.ColliderA.Tag is Projectile { IsPlayer: true } lA && hit.ColliderB.Tag is Bunker bB)
        {
            DamageBunker(bB, lA, hit);
            return;
        }
        if (hit.ColliderB.Tag is Projectile { IsPlayer: true } lB && hit.ColliderA.Tag is Bunker bA)
        {
            DamageBunker(bA, lB, hit);
            return;
        }
    }

    private void HandleTriggerEnter(CollisionResult hit)
    {
        // 1. Enemy Bomb hitting Player
        if ((hit.ColliderA.Tag is Projectile { IsPlayer: false } bombA && ReferenceEquals(hit.ColliderB, _playerCollider)) ||
            (hit.ColliderB.Tag is Projectile { IsPlayer: false } bombB && ReferenceEquals(hit.ColliderA, _playerCollider)))
        {
            var bomb = (hit.ColliderA.Tag as Projectile) ?? (hit.ColliderB.Tag as Projectile)!;
            bomb.Alive = false;
            _collision.Remove(bomb.Collider);

            TriggerPlayerDeath();
            return;
        }

        // 2. Enemy Bomb hitting Bunker (Destructible pixel carving!)
        if (hit.ColliderA.Tag is Projectile { IsPlayer: false } bmA && hit.ColliderB.Tag is Bunker bkrB)
        {
            DamageBunker(bkrB, bmA, hit);
            return;
        }
        if (hit.ColliderB.Tag is Projectile { IsPlayer: false } bmB && hit.ColliderA.Tag is Bunker bkrA)
        {
            DamageBunker(bkrA, bmB, hit);
            return;
        }
    }

    private void KillInvader(Invader invader, Projectile laser, CollisionResult hit)
    {
        laser.Alive = false;
        _collision.Remove(laser.Collider);

        invader.Alive = false;
        invader.Collider.Enabled = false;
        _collision.Remove(invader.Collider);

        // Add Score based on type
        var points = invader.Type switch
        {
            InvaderType.Squid => 30,
            InvaderType.Crab => 20,
            _ => 10
        };
        _score += points;

        SpawnExplosion(invader.Collider.WorldX + 4, invader.Collider.WorldY + 3, _colText, 14);
    }

    private void KillUfo(Ufo ufo, Projectile laser, CollisionResult hit)
    {
        laser.Alive = false;
        _collision.Remove(laser.Collider);

        ufo.Active = false;
        ufo.Collider.Enabled = false;

        var ufoPoints = (Random.Shared.Next(1, 4)) * 100;
        _score += ufoPoints;

        SpawnExplosion(ufo.Collider.WorldX + 8, ufo.Collider.WorldY + 3, _colUfo, 24);
    }

    private void DamageBunker(Bunker bunker, Projectile projectile, CollisionResult hit)
    {
        projectile.Alive = false;
        _collision.Remove(projectile.Collider);

        // Pixel-perfect destruction: carve a crater into the bunker's PixelData at the contact point!
        var contactX = hit.ContactPointX - bunker.Collider.WorldX;
        var contactY = hit.ContactPointY - bunker.Collider.WorldY;

        CarveCrater(bunker.Data, contactX, contactY, radius: 3);

        // Reload the updated collision mask directly from the altered PixelData!
        bunker.Collider.LoadPixelData(bunker.Data);

        SpawnExplosion(hit.ContactPointX, hit.ContactPointY, _colBunker, 8);
    }

    private static void CarveCrater(PixelData data, int cx, int cy, int radius)
    {
        var transparent = new Color(0, 0, 0, 0);
        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                if ((dx * dx) + (dy * dy) <= (radius * radius))
                {
                    data.SetPixel(cx + dx, cy + dy, transparent);
                }
            }
        }
    }

    private void TriggerPlayerDeath()
    {
        _lives--;
        _playerCollider.Enabled = false;
        _deathTimer = 60;
        _state = GameState.PlayerDying;

        SpawnExplosion(_playerCollider.WorldX + 5, _playerCollider.WorldY + 3, _colPlayer, 30);
    }

    private void SpawnExplosion(float x, float y, Color color, int duration)
    {
        _explosions.Add(new Explosion
        {
            X = x,
            Y = y,
            Timer = duration,
            Color = color
        });
    }

    // -------------------------------------------------------------------------
    // Rendering
    // -------------------------------------------------------------------------

    private void RenderGame()
    {
        // 1. Dark space background
        Core.DrawRectangle(0, 0, Core.ScreenWidth, Core.ScreenHeight, new Color(10, 10, 16), 1, true);

        // 2. HUD
        RenderHud();

        // 3. Destructible Bunkers
        foreach (var bunker in _bunkers)
        {
            DrawPixelData(bunker.Data, bunker.X, bunker.Y);
        }

        // 4. Invaders
        foreach (var inv in _invaders)
        {
            if (inv.Alive)
            {
                var frameData = _invaderFrames[(int)inv.Type][_animFrame];
                DrawPixelData(frameData, inv.Collider.WorldX, inv.Collider.WorldY);
            }
        }

        // 5. UFO
        if (_ufo.Active)
        {
            DrawPixelData(_ufo.Data, (int)_ufo.X, (int)_ufo.Y);
        }

        // 6. Projectiles
        foreach (var p in _projectiles)
        {
            if (p.Alive)
            {
                DrawPixelData(p.Data, (int)p.X, (int)p.Y);
            }
        }

        // 7. Player
        if (_playerCollider.Enabled)
        {
            DrawPixelData(_playerData, (int)_playerX, (int)_playerY);
        }

        // 8. Explosions
        foreach (var exp in _explosions)
        {
            var r = (exp.Timer % 4) + 1;
            Core.DrawCircle((int)exp.X, (int)exp.Y, r, exp.Color, 1, false);
            Core.DrawPixel((int)exp.X, (int)exp.Y, _colText);
        }

        // 9. Overlay Screens
        RenderOverlays();
    }

    private void RenderHud()
    {
        // Top score bar
        Text.Draw($"SCORE: {_score:D5}", 4, 2, _colText);
        Text.Draw($"HI: {_highScore:D5}", Core.ScreenWidth / 2, 2, Text.Centre, _colHudDim);
        Text.Draw($"WAVE: {_wave}", Core.ScreenWidth - 4, 2, Text.Right, _colPlayer);

        // Divider below header
        Core.DrawPixel(0, 10, Palette.GetColour(2, PaletteLayer.GUI), Core.ScreenWidth, 1);

        // Bottom green ground line
        var groundY = Core.ScreenHeight - 6;
        Core.DrawPixel(0, groundY, _colBunker, Core.ScreenWidth, 1);

        // Lives indicator icons at bottom-left
        Text.Draw($"{_lives}", 4, groundY - 7, _colText);
        for (var i = 0; i < _lives - 1; i++)
        {
            DrawPixelData(_playerData, 14 + (i * 13), groundY - 8);
        }
    }

    private void RenderOverlays()
    {
        if (_state == GameState.Title)
        {
            // Title screen
            var cx = Core.ScreenWidth / 2;
            Text.Draw("SPACE INVADERS", cx, 30, Text.Centre, _colText);

            // Display invader points legend
            DrawPixelData(_invaderFrames[0][0], cx - 35, 45);
            Text.Draw("= 30 PTS", cx - 20, 46, _colInvaderSquid);

            DrawPixelData(_invaderFrames[1][0], cx - 36, 56);
            Text.Draw("= 20 PTS", cx - 20, 57, _colInvaderCrab);

            DrawPixelData(_invaderFrames[2][0], cx - 37, 67);
            Text.Draw("= 10 PTS", cx - 20, 68, _colInvaderOctopus);

            DrawPixelData(_ufoData, cx - 38, 78);
            Text.Draw("= ??? PTS", cx - 20, 79, _colUfo);

            if (_animFrame == 0)
            {
                Text.Draw("PRESS ENTER TO PLAY", cx, Core.ScreenHeight - 18, Text.Centre, _colPlayer);
            }
        }
        else if (_state == GameState.WaveCleared)
        {
            Text.Draw("WAVE CLEARED!", Core.ScreenWidth / 2, 45, Text.Centre, _colPlayer);
        }
        else if (_state == GameState.GameOver)
        {
            var cx = Core.ScreenWidth / 2;
            Text.Draw("GAME OVER", cx, 40, Text.Centre, _colUfo);
            Text.Draw($"FINAL SCORE: {_score}", cx, 52, Text.Centre, _colText);

            if ((_deathTimer / 15) % 2 == 0)
            {
                Text.Draw("PRESS ENTER TO RETRY", cx, 66, Text.Centre, _colPlayer);
            }
        }
    }

    private static void DrawPixelData(PixelData data, int px, int py)
    {
        for (var y = 0; y < data.Height; y++)
        {
            for (var x = 0; x < data.Width; x++)
            {
                var col = data.GetPixel(x, y);
                if (col.A > 0)
                {
                    Core.DrawPixel(px + x, py + y, col);
                }
            }
        }
    }

    #region Sprite Construction Helpers

    private static PixelData CreatePixelDataFromAscii(int width, int height, Color color, params string[] lines)
    {
        var data = new PixelData(width, height);
        var transparent = new Color(0, 0, 0, 0);

        for (var y = 0; y < height && y < lines.Length; y++)
        {
            var line = lines[y];
            for (var x = 0; x < width && x < line.Length; x++)
            {
                data.SetPixel(x, y, line[x] == 'X' ? color : transparent);
            }
        }
        return data;
    }

    private static PixelData CreateBunkerData(int width, int height, Color color)
    {
        var data = new PixelData(width, height, color);
        var transparent = new Color(0, 0, 0, 0);

        // Slant top-left corner
        data.SetPixel(0, 0, transparent);
        data.SetPixel(1, 0, transparent);
        data.SetPixel(0, 1, transparent);

        // Slant top-right corner
        data.SetPixel(width - 1, 0, transparent);
        data.SetPixel(width - 2, 0, transparent);
        data.SetPixel(width - 1, 1, transparent);

        // Cut out bottom arch
        var archW = 6;
        var archH = 4;
        var archX = (width - archW) / 2;
        var archY = height - archH;

        for (var y = archY; y < height; y++)
        {
            for (var x = archX; x < archX + archW; x++)
            {
                data.SetPixel(x, y, transparent);
            }
        }

        return data;
    }

    #endregion
}
