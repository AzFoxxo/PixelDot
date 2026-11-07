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
/// Comprehensive automated test suite for the PixelDot 2D Collision System.
/// Validates pixel-perfect collision, broad-phase bounding-box rejection, transparency handling,
/// triggers, collision layer filtering, and enter/stay/exit lifecycle events.
/// </summary>
[RunnableApplication("Collision Tests", "1.0.0", "Az Foxxo", "Automated test runner for the PixelDot Collision System")]
public class CollisionTests : Application
{
    private static int _passCount;
    private static int _failCount;

    public override void OnInit()
    {
        Logger.LogInfo("=== Running PixelDot Collision System Test Suite ===");
        var allPassed = RunAllTests();
        Logger.LogInfo($"=== Test Results: {_passCount} Passed, {_failCount} Failed ===");

        if (!allPassed)
        {
            Logger.LogError("One or more collision tests failed!");
        }
        else
        {
            Logger.LogInfo("All collision tests passed successfully!");
        }
    }

    public override void OnUpdate()
    {
        // Visual indicator in test application
        Text.SetFont("Regular");
        Text.SetSize(8);
        var text = _failCount == 0
            ? $"All {_passCount} Collision Tests Passed!"
            : $"Collision Tests: {_failCount} Failed, {_passCount} Passed";

        var col = _failCount == 0
            ? new Color(0, 255, 0)
            : new Color(255, 0, 0);

        Text.Draw(text, 10, 10, col);
    }

    /// <summary>
    /// Executes all collision unit tests and returns true if all pass.
    /// </summary>
    public static bool RunAllTests()
    {
        _passCount = 0;
        _failCount = 0;

        TestBoundingBoxRejection();
        TestPixelPerfectCollision();
        TestTransparencyHandling();
        TestPositionOffsets();
        TestTriggers();
        TestCollisionFiltering();
        TestLifecycleEvents_EnterStayExit();
        TestDisabledColliderExit();
        TestRemovedColliderExit();
        TestEmptyPixelDataSafety();

        return _failCount == 0;
    }

    #region Individual Tests

    /// <summary>
    /// 1. Verifies that colliders whose rectangular bounds do not overlap exit immediately in the broad phase.
    /// </summary>
    public static void TestBoundingBoxRejection()
    {
        var a = new PixelDataCollider("A", 0, 0, PixelData.CreateSolid(10, 10));
        var b = new PixelDataCollider("B", 20, 20, PixelData.CreateSolid(10, 10));

        Assert(!a.CheckBounds(b), "Bounds should not overlap when separated.");
        Assert(!b.CheckBounds(a), "Bounds symmetry check should be false.");
        Assert(a.CheckCollision(b) is null, "CollisionResult must be null when bounds do not intersect.");
    }

    /// <summary>
    /// 2. Verifies pixel-perfect collision detection:
    /// Bounding boxes can overlap, but if solid pixels don't touch, no collision occurs.
    /// When solid pixels touch, exact overlap pixel count and bounds are computed.
    /// </summary>
    public static void TestPixelPerfectCollision()
    {
        // Collider A: 3x3 with solid top-left pixel only (0,0)
        // [X, ., .]
        // [., ., .]
        // [., ., .]
        var maskA = new bool[3 * 3];
        maskA[0] = true;
        var a = new PixelDataCollider("A", 0, 0);
        a.LoadMask(maskA, 3, 3);

        // Collider B: 3x3 with solid bottom-right pixel only (2,2)
        // [., ., .]
        // [., ., .]
        // [., ., X]
        var maskB = new bool[3 * 3];
        maskB[8] = true;
        var b = new PixelDataCollider("B", 0, 0);
        b.LoadMask(maskB, 3, 3);

        // Bounding boxes match completely, but solid pixels are at (0,0) and (2,2) -> no collision!
        Assert(a.CheckBounds(b), "Bounding boxes overlap.");
        var noHitResult = a.CheckCollision(b);
        Assert(noHitResult is null, "Pixel-perfect check should return null when solid pixels do not touch.");

        // Now move B so its solid pixel (2,2) sits on world (0,0):
        // World position of B = (-2, -2) -> its local pixel (2,2) is at world (0,0)
        b.X = -2;
        b.Y = -2;

        var hitResult = a.CheckCollision(b);
        Assert(hitResult is not null, "Collision should be detected when solid pixels overlap.");
        Assert(hitResult!.OverlappingPixelCount == 1, "Exactly 1 pixel should overlap.");
        Assert(hitResult.OverlapBounds.X == 0 && hitResult.OverlapBounds.Y == 0, "Overlap bound X,Y should be (0,0).");
        Assert(hitResult.OverlapBounds.Width == 1 && hitResult.OverlapBounds.Height == 1, "Overlap bound size should be 1x1.");
    }

    /// <summary>
    /// 3. Verifies that transparent pixels (alpha 0 or configured transparent colour) are ignored.
    /// </summary>
    public static void TestTransparencyHandling()
    {
        var transparentColor = new Color(255, 0, 255); // Magenta
        var solidColor = new Color(255, 255, 255);

        // 2x2 sprite: top row magenta (transparent), bottom row white (solid)
        var pixels = new Color[]
        {
            transparentColor, transparentColor,
            solidColor, solidColor
        };
        var data = new PixelData(2, 2, pixels);

        var a = new PixelDataCollider("A", 0, 0);
        a.LoadPixelData(data, transparentColor);

        // Solid collider C placed over the top row of A (which is transparent)
        var c = new PixelDataCollider("C", 0, 0, PixelData.CreateSolid(2, 1));
        var resultTop = a.CheckCollision(c);
        Assert(resultTop is null, "Top row is transparent; no collision should be registered.");

        // Place over the bottom row of A (which is solid)
        c.Y = 1;
        var resultBottom = a.CheckCollision(c);
        Assert(resultBottom is not null, "Bottom row is solid; collision should be detected.");
        Assert(resultBottom!.OverlappingPixelCount == 2, "Should overlap 2 solid pixels.");
    }

    /// <summary>
    /// 4. Verifies position offsets (OffsetX / OffsetY) in world space.
    /// </summary>
    public static void TestPositionOffsets()
    {
        var a = new PixelDataCollider("A", 0, 0, PixelData.CreateSolid(5, 5))
        {
            OffsetX = 10,
            OffsetY = 10
        };

        Assert(a.WorldX == 10 && a.WorldY == 10, "WorldX/Y should reflect OffsetX/Y.");

        // Collider at world (0,0)
        var b = new PixelDataCollider("B", 0, 0, PixelData.CreateSolid(5, 5));
        Assert(!a.CheckBounds(b), "With offset at (10,10), it should not overlap (0,0).");

        // Move B to (10,10)
        b.X = 10;
        b.Y = 10;
        Assert(a.CheckBounds(b), "Should overlap when positioned at matching world coordinates.");
        Assert(a.CheckCollision(b) is not null, "Collision should be detected with offsets.");
    }

    /// <summary>
    /// 5. Verifies triggers:
    /// - Overlap occurs
    /// - IsTrigger property is set
    /// - OnTriggerEnter / Stay / Exit fire, while OnCollisionEnter does NOT fire
    /// </summary>
    public static void TestTriggers()
    {
        var manager = new CollisionManager();

        var player = new PixelDataCollider("Player", 0, 0, PixelData.CreateSolid(4, 4))
        {
            IsTrigger = false,
            Layer = CollisionLayer.Player,
            CollisionMask = CollisionLayer.All
        };

        var pickup = new PixelDataCollider("Pickup", 2, 2, PixelData.CreateSolid(4, 4))
        {
            IsTrigger = true,
            Layer = CollisionLayer.Trigger,
            CollisionMask = CollisionLayer.All
        };

        manager.Add(player);
        manager.Add(pickup);

        var triggerEnterCount = 0;
        var collisionEnterCount = 0;
        CollisionResult? capturedResult = null;

        manager.OnTriggerEnter += res =>
        {
            triggerEnterCount++;
            capturedResult = res;
        };

        manager.OnCollisionEnter += _ =>
        {
            collisionEnterCount++;
        };

        manager.Update();

        Assert(triggerEnterCount == 1, "OnTriggerEnter should fire exactly once.");
        Assert(collisionEnterCount == 0, "OnCollisionEnter must NOT fire for trigger overlaps.");
        Assert(capturedResult is not null && capturedResult.IsTrigger, "CollisionResult.IsTrigger should be true.");
    }

    /// <summary>
    /// 6. Verifies collision layer filtering matrix:
    /// Only tests colliders whose masks allow interaction with the other's layer.
    /// </summary>
    public static void TestCollisionFiltering()
    {
        var manager = new CollisionManager();

        var player = new PixelDataCollider("Player", 0, 0, PixelData.CreateSolid(4, 4))
        {
            Layer = CollisionLayer.Player,
            CollisionMask = CollisionLayer.Enemy // Player only collides with enemies
        };

        var environment = new PixelDataCollider("Wall", 0, 0, PixelData.CreateSolid(4, 4))
        {
            Layer = CollisionLayer.Environment,
            CollisionMask = CollisionLayer.All
        };

        var enemy = new PixelDataCollider("Enemy", 0, 0, PixelData.CreateSolid(4, 4))
        {
            Layer = CollisionLayer.Enemy,
            CollisionMask = CollisionLayer.Player
        };

        manager.Add(player);
        manager.Add(environment);
        manager.Add(enemy);

        var collisionCount = 0;
        manager.OnCollisionEnter += _ => collisionCount++;

        manager.Update();

        // Player collides with Enemy (compatible masks).
        // Player does NOT collide with Environment (Player's mask excludes Environment).
        // Environment collides with Enemy (Environment mask is All, Enemy mask is Player only -> Enemy mask excludes Environment!).
        // Therefore, exactly 1 collision (Player <-> Enemy) should occur.
        Assert(collisionCount == 1, $"Expected exactly 1 collision, but got {collisionCount}.");
    }

    /// <summary>
    /// 7. Verifies enter -> stay -> exit lifecycle transitions across update cycles.
    /// </summary>
    public static void TestLifecycleEvents_EnterStayExit()
    {
        var manager = new CollisionManager();

        var a = new PixelDataCollider("A", 0, 0, PixelData.CreateSolid(4, 4))
        {
            Layer = CollisionLayer.Player,
            CollisionMask = CollisionLayer.All
        };

        var b = new PixelDataCollider("B", 2, 2, PixelData.CreateSolid(4, 4))
        {
            Layer = CollisionLayer.Enemy,
            CollisionMask = CollisionLayer.All
        };

        manager.Add(a);
        manager.Add(b);

        var enterCount = 0;
        var stayCount = 0;
        var exitCount = 0;

        manager.OnCollisionEnter += _ => enterCount++;
        manager.OnCollisionStay += _ => stayCount++;
        manager.OnCollisionExit += _ => exitCount++;

        // Frame 1: Initial collision
        manager.Update();
        Assert(enterCount == 1, "Enter should be 1 on initial overlap.");
        Assert(stayCount == 0, "Stay should be 0 on initial overlap.");
        Assert(exitCount == 0, "Exit should be 0 on initial overlap.");

        // Frame 2: Persisting collision
        manager.Update();
        Assert(enterCount == 1, "Enter should remain 1 on persistent overlap.");
        Assert(stayCount == 1, "Stay should be 1 on persistent overlap.");
        Assert(exitCount == 0, "Exit should remain 0.");

        // Frame 3: Move B away so they separate
        b.X = 100;
        manager.Update();
        Assert(enterCount == 1, "Enter should remain 1.");
        Assert(stayCount == 1, "Stay should remain 1.");
        Assert(exitCount == 1, "Exit should fire when colliders separate.");

        // Frame 4: Separated, no events
        manager.Update();
        Assert(enterCount == 1 && stayCount == 1 && exitCount == 1, "No further events when separated.");
    }

    /// <summary>
    /// 8. Verifies that disabling an active collider fires an Exit event on the next update.
    /// </summary>
    public static void TestDisabledColliderExit()
    {
        var manager = new CollisionManager();

        var a = new PixelDataCollider("A", 0, 0, PixelData.CreateSolid(4, 4));
        var b = new PixelDataCollider("B", 0, 0, PixelData.CreateSolid(4, 4));

        manager.Add(a);
        manager.Add(b);

        var exitCount = 0;
        manager.OnCollisionExit += _ => exitCount++;

        manager.Update(); // Enter
        Assert(exitCount == 0, "No exit on enter.");

        // Disable collider A
        a.Enabled = false;
        manager.Update();

        Assert(exitCount == 1, "Disabling an active collider must fire an exit event on next update.");
    }

    /// <summary>
    /// 9. Verifies that removing a collider from the manager cleans up active collisions and triggers Exit.
    /// </summary>
    public static void TestRemovedColliderExit()
    {
        var manager = new CollisionManager();

        var a = new PixelDataCollider("A", 0, 0, PixelData.CreateSolid(4, 4));
        var b = new PixelDataCollider("B", 0, 0, PixelData.CreateSolid(4, 4));

        manager.Add(a);
        manager.Add(b);

        var exitCount = 0;
        manager.OnCollisionExit += _ => exitCount++;

        manager.Update(); // Enter
        Assert(exitCount == 0, "No exit on enter.");

        // Remove collider A from manager
        manager.Remove(a);
        Assert(exitCount == 1, "Removing an active collider must immediately fire an exit event.");

        // Next update should produce no errors and no duplicate exits
        manager.Update();
        Assert(exitCount == 1, "Exit count must not increment after removal.");
    }

    /// <summary>
    /// 10. Verifies that colliders with empty or missing pixel data are handled safely without exceptions.
    /// </summary>
    public static void TestEmptyPixelDataSafety()
    {
        var empty = new PixelDataCollider("Empty");
        var solid = new PixelDataCollider("Solid", 0, 0, PixelData.CreateSolid(4, 4));

        Assert(empty.Width == 0 && empty.Height == 0, "Default dimensions should be 0.");
        Assert(!empty.CheckBounds(solid), "Empty collider must not overlap bounds.");
        Assert(empty.CheckCollision(solid) is null, "Empty collider must not cause collision.");

        // Test clearing a collider
        solid.Clear();
        Assert(solid.Width == 0 && solid.Height == 0, "Cleared collider should have 0 size.");
        Assert(solid.CheckCollision(empty) is null, "Cleared collider should not collide.");
    }

    #endregion

    #region Helper Assertion

    private static void Assert(bool condition, string message)
    {
        if (condition)
        {
            _passCount++;
        }
        else
        {
            _failCount++;
            Logger.LogError($"[TEST FAILURE] {message}");
        }
    }

    #endregion
}
