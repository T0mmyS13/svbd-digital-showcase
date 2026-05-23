using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Základní objekt scény s transformací a životním cyklem.
/// </summary>
internal class SceneObject : IDisposable
{
    /// <summary>
    /// Pozice objektu ve světě.
    /// </summary>
    protected Vector3 _position;

    /// <summary>
    /// Měřítko objektu.
    /// </summary>
    protected Vector3 _scale = Vector3.One;

    /// <summary>
    /// Rotace objektu v radiánech.
    /// </summary>
    protected Vector3 _rotation;

    /// <summary>
    /// Indikuje, zda je potřeba přepočítat modelovou matici.
    /// </summary>
    protected bool _isModelMatrixDirty = true;

    /// <summary>
    /// Cacheovaná modelová matice objektu.
    /// </summary>
    protected Matrix4 _modelMatrix;

    /// <summary>
    /// Nastaví pozici objektu ve světě.
    /// </summary>
    /// <param name="position">Nová pozice.</param>
    public void SetPosition(Vector3 position)
    {
        _position = position;
        _isModelMatrixDirty = true;
    }

    /// <summary>
    /// Vrátí pozici objektu ve světě.
    /// </summary>
    /// <returns>Pozice objektu.</returns>
    public Vector3 GetPosition()
    {
        return _position;
    }

    /// <summary>
    /// Nastaví měřítko objektu.
    /// </summary>
    /// <param name="scale">Nové měřítko.</param>
    public void SetScale(Vector3 scale)
    {
        _scale = scale;
        _isModelMatrixDirty = true;
    }

    /// <summary>
    /// Vrátí měřítko objektu.
    /// </summary>
    /// <returns>Měřítko objektu.</returns>
    public Vector3 GetScale()
    {
        return _scale;
    }

    /// <summary>
    /// Nastaví rotaci objektu v radiánech.
    /// </summary>
    /// <param name="rotation">Nová rotace.</param>
    public void SetRotation(Vector3 rotation)
    {
        _rotation = rotation;
        _isModelMatrixDirty = true;
    }

    /// <summary>
    /// Vrátí rotaci objektu.
    /// </summary>
    /// <returns>Rotace objektu.</returns>
    public Vector3 GetRotation()
    {
        return _rotation;
    }

    /// <summary>
    /// Vrátí modelovou matici objektu s cache.
    /// </summary>
    /// <returns>Výsledná modelová matice.</returns>
    public Matrix4 GetModelMatrix()
    {
        if (_isModelMatrixDirty)
        {
            _modelMatrix = ComputeModelMatrix();
            _isModelMatrixDirty = false;
        }

        return _modelMatrix;
    }

    /// <summary>
    /// Vytvoří modelovou matici z pozice, rotace a měřítka.
    /// </summary>
    /// <returns>Výsledná modelová matice.</returns>
    protected virtual Matrix4 ComputeModelMatrix()
    {
        return Matrix4.CreateScale(_scale)
            * Matrix4.CreateRotationX(_rotation.X)
            * Matrix4.CreateRotationY(_rotation.Y)
            * Matrix4.CreateRotationZ(_rotation.Z)
            * Matrix4.CreateTranslation(_position);
    }

    /// <summary>
    /// Aktualizuje objekt.
    /// </summary>
    /// <param name="dt">Doba od posledního snímku v sekundách.</param>
    public virtual void Update(float dt)
    {
    }

    /// <summary>
    /// Vykreslí objekt.
    /// </summary>
    public virtual void Draw()
    {
    }

    /// <summary>
    /// Uvolní prostředky objektu.
    /// </summary>
    public virtual void Dispose()
    {
    }
}
