using System;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// FPS kamera s orientací yaw/pitch a pohybem.
/// </summary>
internal class Camera : SceneObject
{
    /// <summary>
    /// Tolerance pro přichycení kamery k zemi při dopadu.
    /// </summary>
    private const float GroundSnapEpsilon = 0.08f;

    /// <summary>
    /// Maximální povolená strmost při pohybu do kopce v radiánech.
    /// </summary>
    private const float UphillSlopeThreshold = 0.5f;

    /// <summary>
    /// Násobek rychlosti při pohybu ve vodě.
    /// </summary>
    private const float WaterSpeedMultiplier = 0.6f;

    /// <summary>
    /// Směr, kterým kamera míří.
    /// </summary>
    public Vector3 Front;

    /// <summary>
    /// Směr nahoru kamery.
    /// </summary>
    public Vector3 Up;

    /// <summary>
    /// Pravý směrový vektor kamery.
    /// </summary>
    public Vector3 Right;

    /// <summary>
    /// Horizontální úhel natočení ve stupních.
    /// </summary>
    public float Yaw;

    /// <summary>
    /// Vertikální úhel natočení ve stupních.
    /// </summary>
    public float Pitch;

    /// <summary>
    /// Rychlost pohybu kamery.
    /// </summary>
    public float MovementSpeed = 9.11f;

    /// <summary>
    /// Výška očí nad terénem.
    /// </summary>
    public float EyeHeight;

    /// <summary>
    /// Požadovaný směr pohybu v rovině XZ.
    /// </summary>
    public Vector3 MoveDirection;

    /// <summary>
    /// Omezení pohybu na ose X.
    /// </summary>
    public float MinX;

    /// <summary>
    /// Omezení pohybu na ose X.
    /// </summary>
    public float MaxX;

    /// <summary>
    /// Omezení pohybu na ose Z.
    /// </summary>
    public float MinZ;

    /// <summary>
    /// Omezení pohybu na ose Z.
    /// </summary>
    public float MaxZ;

    /// <summary>
    /// Terén použitý pro výpočet výšky kamery.
    /// </summary>
    public Terrain? Terrain;

    /// <summary>
    /// Volitelná výška podlahy pro objekty nad terénem (např. vrchol kamene) včetně EyeHeight.
    /// Pokud je menší nebo rovna nule, používá se pouze výška terénu.
    /// </summary>
    public float ExtraGroundY;

    /// <summary>
    /// Počáteční rychlost výskoku.
    /// </summary>
    public float JumpSpeed = 3.7f;

    /// <summary>
    /// Gravitační zrychlení.
    /// </summary>
    public float Gravity = 9.81f;

    /// <summary>
    /// Indikuje, zda je kamera na zemi.
    /// </summary>
    public bool IsGrounded = true;

    /// <summary>
    /// Aktuální rychlost kamery ve světových osách.
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    /// Aktuální vertikální rychlost kamery.
    /// </summary>
    private float verticalVelocity;

    /// <summary>
    /// Indikuje, že byl vyžádán výskok.
    /// </summary>
    private bool jumpRequested;

    /// <summary>
    /// Vytvoří kameru na zadané pozici.
    /// </summary>
    /// <param name="startPosition">Počáteční pozice kamery.</param>
    public Camera(Vector3 startPosition)
    {
        SetPosition(startPosition);
        Yaw = -90.0f;
        Pitch = 0.0f;
        Up = new Vector3(0.0f, 1.0f, 0.0f);

        UpdateVectors();
    }

    /// <summary>
    /// Vrátí perspektivní projekční matici.
    /// </summary>
    /// <param name="aspectRatio">Poměr stran viewportu.</param>
    /// <returns>Projekční matice kamery.</returns>
    public virtual Matrix4 GetProjectionMatrix(float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), aspectRatio, 0.1f, 1000.0f);
    }

    /// <summary>
    /// Aktualizuje pozici, rychlost a stav skoku kamery.
    /// </summary>
    /// <param name="dt">Doba od posledního snímku v sekundách.</param>
    public override void Update(float dt)
    {
        base.Update(dt);

        Vector3 position = GetPosition();
        Vector3 horizontalVelocity = Vector3.Zero;

        if (MoveDirection.LengthSquared > 0.0f)
        {
            Vector3 normalizedDirection = Vector3.Normalize(MoveDirection);
            float currentHorizontalSpeed = MovementSpeed;

            if (Terrain != null && IsGrounded)
            {
                SurfaceType currentSurfaceType = Terrain.GetSurfaceTypeAt(position.X, position.Z);
                if (currentSurfaceType == SurfaceType.Water)
                {
                    currentHorizontalSpeed = currentHorizontalSpeed * WaterSpeedMultiplier;
                }
            }

            horizontalVelocity = normalizedDirection * currentHorizontalSpeed;

            float targetX = position.X + (horizontalVelocity.X * dt);
            float targetZ = position.Z + (horizontalVelocity.Z * dt);

            bool canMoveHorizontally = true;
            if (Terrain != null && IsGrounded)
            {
                canMoveHorizontally = Terrain.CanMoveUphill(position.X, position.Z, targetX, targetZ, UphillSlopeThreshold);
            }

            if (canMoveHorizontally)
            {
                position = new Vector3(targetX, position.Y, targetZ);
            }
            else
            {
                horizontalVelocity = Vector3.Zero;
            }
        }

        float clampedX = MathHelper.Clamp(position.X, MinX, MaxX);
        float clampedZ = MathHelper.Clamp(position.Z, MinZ, MaxZ);

        float groundY = position.Y;
        if (Terrain != null)
        {
            groundY = Terrain.GetHeightAt(clampedX, clampedZ) + EyeHeight;
        }

        if (ExtraGroundY > groundY)
        {
            groundY = ExtraGroundY;
        }

        bool nearGround = position.Y <= groundY + GroundSnapEpsilon;
        if (jumpRequested && nearGround)
        {
            verticalVelocity = JumpSpeed;
            IsGrounded = false;
        }

        jumpRequested = false;

        verticalVelocity -= Gravity * dt;
        float nextY = position.Y + verticalVelocity * dt;

        bool isFalling = verticalVelocity <= 0.0f;
        if (isFalling && nextY <= groundY + GroundSnapEpsilon)
        {
            nextY = groundY;

            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = 0.0f;
            }

            IsGrounded = true;
        }
        else
        {
            IsGrounded = false;
        }

        position = new Vector3(clampedX, nextY, clampedZ);
        SetPosition(position);
        Velocity = new Vector3(horizontalVelocity.X, verticalVelocity, horizontalVelocity.Z);
    }

    /// <summary>
    /// Vyžádá výskok kamery.
    /// </summary>
    public void RequestJump()
    {
        jumpRequested = true;
    }

    /// <summary>
    /// Vrátí pohledovou matici kamery.
    /// </summary>
    /// <returns>Pohledová matice.</returns>
    public Matrix4 GetViewMatrix()
    {
        Vector3 position = GetPosition();
        return Matrix4.LookAt(position, position + Front, Up);
    }

    /// <summary>
    /// Přepočítá směrové vektory z yaw a pitch.
    /// </summary>
    public void UpdateVectors()
    {
        if (Pitch > 89.0f)
        {
            Pitch = 89.0f;
        }

        if (Pitch < -89.0f)
        {
            Pitch = -89.0f;
        }

        float radYaw = MathHelper.DegreesToRadians(Yaw);
        float radPitch = MathHelper.DegreesToRadians(Pitch);

        Front.X = (float)(Math.Cos(radPitch) * Math.Cos(radYaw));
        Front.Y = (float)Math.Sin(radPitch);
        Front.Z = (float)(Math.Cos(radPitch) * Math.Sin(radYaw));
        Front = Vector3.Normalize(Front);

        Right = Vector3.Normalize(Vector3.Cross(Front, Up));
    }
}