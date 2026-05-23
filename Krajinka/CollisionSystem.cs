using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Jedna instance objektu ve scéně.
/// </summary>
internal struct ObjectInstance
{
    public Vector3 Position;
    public byte Type;
    public float RotationY;
    public float Scale;
    public ObjectHitbox Hitbox;
}

/// <summary>
/// Jedna instance květiny ve scéně.
/// </summary>
internal struct FlowerInstance
{
    public Vector3 Position;
    public float RotationY;
    public float Scale;
    public int ModelIndex;
    public ObjectHitbox Hitbox;
}

/// <summary>
/// Předpočítaný hitbox objektu po aplikaci měřítka.
/// </summary>
internal struct ObjectHitbox
{
    public float Radius;
    public float MinX;
    public float MaxX;
    public float MinZ;
    public float MaxZ;
    public float TopY;
}

/// <summary>
/// Kolizní logika pro objekty ve scéně.
/// </summary>
internal class CollisionSystem
{
    /// <summary>
    /// Tolerance výšky chodidel pro pohyb po vršku kamene.
    /// </summary>
    private const float RockTopStepEpsilon = 0.08f;

    /// <summary>
    /// Kód objektu stromu.
    /// </summary>
    private readonly byte treeObjectCode;

    /// <summary>
    /// Kód objektu kamene.
    /// </summary>
    private readonly byte rockObjectCode;

    /// <summary>
    /// Kód objektu keře.
    /// </summary>
    private readonly byte bushObjectCode;

    /// <summary>
    /// Kód objektu květiny.
    /// </summary>
    private readonly byte flowerObjectCode;

    /// <summary>
    /// Kolizní poloměr hráče v rovině XZ.
    /// </summary>
    private readonly float cameraCollisionRadius;

    /// <summary>
    /// Základní radius hitboxu stromu.
    /// </summary>
    private readonly float treeHitboxRadius;

    /// <summary>
    /// Základní radius hitboxu kamene.
    /// </summary>
    private readonly float rockHitboxRadius;

    /// <summary>
    /// Základní radius hitboxu keře.
    /// </summary>
    private readonly float bushHitboxRadius;

    /// <summary>
    /// Základní radius hitboxu květiny.
    /// </summary>
    private readonly float flowerHitboxRadius;

    /// <summary>
    /// Výška hitboxu stromu.
    /// </summary>
    private readonly float treeHitboxHeight;

    /// <summary>
    /// Výška hitboxu kamene.
    /// </summary>
    private readonly float rockHitboxHeight;

    /// <summary>
    /// Výška hitboxu keře.
    /// </summary>
    private readonly float bushHitboxHeight;

    /// <summary>
    /// Výška hitboxu květiny.
    /// </summary>
    private readonly float flowerHitboxHeight;

    /// <summary>
    /// Vytvoří kolizní systém se známými parametry hitboxů.
    /// </summary>
    public CollisionSystem(
        byte treeObjectCode,
        byte rockObjectCode,
        byte bushObjectCode,
        byte flowerObjectCode,
        float cameraCollisionRadius,
        float treeHitboxRadius,
        float rockHitboxRadius,
        float bushHitboxRadius,
        float flowerHitboxRadius,
        float treeHitboxHeight,
        float rockHitboxHeight,
        float bushHitboxHeight,
        float flowerHitboxHeight)
    {
        this.treeObjectCode = treeObjectCode;
        this.rockObjectCode = rockObjectCode;
        this.bushObjectCode = bushObjectCode;
        this.flowerObjectCode = flowerObjectCode;
        this.cameraCollisionRadius = cameraCollisionRadius;
        this.treeHitboxRadius = treeHitboxRadius;
        this.rockHitboxRadius = rockHitboxRadius;
        this.bushHitboxRadius = bushHitboxRadius;
        this.flowerHitboxRadius = flowerHitboxRadius;
        this.treeHitboxHeight = treeHitboxHeight;
        this.rockHitboxHeight = rockHitboxHeight;
        this.bushHitboxHeight = bushHitboxHeight;
        this.flowerHitboxHeight = flowerHitboxHeight;
    }

    /// <summary>
    /// Vytvoří hitbox objektu a předpočítá jeho rozměry.
    /// </summary>
    public ObjectHitbox CreateHitbox(byte objectType, Vector3 position)
    {
        ObjectHitbox hitbox = new ObjectHitbox();

        if (objectType == treeObjectCode)
        {
            hitbox.Radius = treeHitboxRadius;
            hitbox.TopY = position.Y + treeHitboxHeight;
        }
        else if (objectType == bushObjectCode)
        {
            hitbox.Radius = bushHitboxRadius;
            hitbox.TopY = position.Y + bushHitboxHeight;
        }
        else if (objectType == flowerObjectCode)
        {
            hitbox.Radius = flowerHitboxRadius;
            hitbox.TopY = position.Y + flowerHitboxHeight;
        }
        else
        {
            hitbox.Radius = rockHitboxRadius;
            hitbox.TopY = position.Y + rockHitboxHeight;
        }

        hitbox.MinX = position.X - hitbox.Radius;
        hitbox.MaxX = position.X + hitbox.Radius;
        hitbox.MinZ = position.Z - hitbox.Radius;
        hitbox.MaxZ = position.Z + hitbox.Radius;

        return hitbox;
    }

    /// <summary>
    /// Zjistí, zda je místo volné pro umístění dalšího objektu.
    /// </summary>
    public bool IsPlacementFree(ObjectHitbox candidateHitbox, List<ObjectInstance> objectInstances, List<FlowerInstance> flowerInstances)
    {
        for (int i = 0; i < objectInstances.Count; i++)
        {
            ObjectHitbox existingHitbox = objectInstances[i].Hitbox;
            if (AreHitboxesOverlapping(candidateHitbox, existingHitbox))
            {
                return false;
            }
        }

        for (int i = 0; i < flowerInstances.Count; i++)
        {
            ObjectHitbox existingHitbox = flowerInstances[i].Hitbox;
            if (AreHitboxesOverlapping(candidateHitbox, existingHitbox))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Ověří, zda se dva hitboxy překrývají v rovině XZ.
    /// </summary>
    private static bool AreHitboxesOverlapping(ObjectHitbox first, ObjectHitbox second)
    {
        float deltaX = first.MinX + first.Radius - (second.MinX + second.Radius);
        float deltaZ = first.MinZ + first.Radius - (second.MinZ + second.Radius);
        float radiusSum = first.Radius + second.Radius;
        return (deltaX * deltaX) + (deltaZ * deltaZ) < radiusSum * radiusSum;
    }

    /// <summary>
    /// Vypočítá předpokládanou pozici kamery po horizontálním pohybu.
    /// </summary>
    public Vector3 GetPredictedCameraPosition(Vector3 currentPosition, Vector3 moveDirection, float dt, float movementSpeed)
    {
        if (moveDirection.LengthSquared <= 0.0f)
        {
            return currentPosition;
        }

        Vector3 normalizedMoveDirection = Vector3.Normalize(moveDirection);
        float predictedX = currentPosition.X + (normalizedMoveDirection.X * movementSpeed * dt);
        float predictedZ = currentPosition.Z + (normalizedMoveDirection.Z * movementSpeed * dt);
        return new Vector3(predictedX, currentPosition.Y, predictedZ);
    }

    /// <summary>
    /// Vrátí true, pokud plánovaný pohyb kamery narazí do objektu.
    /// </summary>
    public bool IsColliding(
        Vector3 currentPosition,
        Vector3 moveDirection,
        float dt,
        float movementSpeed,
        float eyeHeight,
        List<ObjectInstance> objectInstances)
    {
        if (moveDirection.LengthSquared <= 0.0f)
        {
            return false;
        }

        Vector3 targetCameraPosition = GetPredictedCameraPosition(currentPosition, moveDirection, dt, movementSpeed);
        Vector2 targetPosition = new Vector2(targetCameraPosition.X, targetCameraPosition.Z);
        float cameraFeetY = currentPosition.Y - eyeHeight;

        for (int i = 0; i < objectInstances.Count; i++)
        {
            ObjectInstance instance = objectInstances[i];
            byte type = instance.Type;
            ObjectHitbox hitbox = instance.Hitbox;

            bool intersectsObjectBoxXZ =
                targetPosition.X >= hitbox.MinX - cameraCollisionRadius &&
                targetPosition.X <= hitbox.MaxX + cameraCollisionRadius &&
                targetPosition.Y >= hitbox.MinZ - cameraCollisionRadius &&
                targetPosition.Y <= hitbox.MaxZ + cameraCollisionRadius;

            if (!intersectsObjectBoxXZ)
            {
                continue;
            }

            bool canStepOnObjectTop = type == rockObjectCode && cameraFeetY + RockTopStepEpsilon >= hitbox.TopY;
            if (canStepOnObjectTop)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Vrátí výšku podlahy kamery pro vršek kamene včetně výšky očí.
    /// </summary>
    public float GetRockTopGroundY(Vector3 cameraPosition, float eyeHeight, List<ObjectInstance> objectInstances)
    {
        float bestGroundY = 0.0f;
        float cameraFeetY = cameraPosition.Y - eyeHeight;

        for (int i = 0; i < objectInstances.Count; i++)
        {
            ObjectInstance instance = objectInstances[i];
            if (instance.Type != rockObjectCode)
            {
                continue;
            }

            ObjectHitbox hitbox = instance.Hitbox;

            bool isInsideRockTopXZ =
                cameraPosition.X >= hitbox.MinX &&
                cameraPosition.X <= hitbox.MaxX &&
                cameraPosition.Z >= hitbox.MinZ &&
                cameraPosition.Z <= hitbox.MaxZ;

            if (!isInsideRockTopXZ)
            {
                continue;
            }

            if (cameraFeetY + RockTopStepEpsilon < hitbox.TopY)
            {
                continue;
            }

            float candidateGroundY = hitbox.TopY + eyeHeight;
            if (candidateGroundY > bestGroundY)
            {
                bestGroundY = candidateGroundY;
            }
        }

        return bestGroundY;
    }

    /// <summary>
    /// Zničí květiny, které se nacházejí pod kamerou (zašlápnutí do země).
    /// </summary>
    /// <returns>True, pokud byla odstraněna alespoň jedna květina.</returns>
    public bool TrampleFlowers(Vector3 position, List<FlowerInstance>[,] flowerGrid, List<FlowerInstance> flowerInstances)
    {
        bool removedAnyFlower = false;
        int gridWidth = flowerGrid.GetLength(0);
        int gridDepth = flowerGrid.GetLength(1);

        float spacing = 0.5f;

        int centerGridX = Math.Clamp((int)MathF.Round(position.X / spacing), 0, gridWidth - 1);
        int centerGridZ = Math.Clamp((int)MathF.Round(position.Z / spacing), 0, gridDepth - 1);

        int radius = 1;
        int minX = Math.Max(0, centerGridX - radius);
        int maxX = Math.Min(gridWidth - 1, centerGridX + radius);
        
        int minZ = Math.Max(0, centerGridZ - radius);
        int maxZ = Math.Min(gridDepth - 1, centerGridZ + radius);

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                List<FlowerInstance>? cell = flowerGrid[x, z];

                if (cell == null)
                {
                    continue;
                }

                for (int i = cell.Count - 1; i >= 0; i--)
                {
                    FlowerInstance flower = cell[i];
                    
                    float deltaX = position.X - flower.Position.X;
                    float deltaZ = position.Z - flower.Position.Z;
                    float distanceSquared = (deltaX * deltaX) + (deltaZ * deltaZ);

                    float trampleRadiusSquared = cameraCollisionRadius * cameraCollisionRadius;

                    if (distanceSquared <= trampleRadiusSquared)
                    {
                        cell.RemoveAt(i);
                        flowerInstances.Remove(flower);
                        removedAnyFlower = true;
                    }
                }
            }
        }

        return removedAnyFlower;
    }
}
