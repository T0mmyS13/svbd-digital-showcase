using OpenTK.Mathematics;

namespace Krajinka
{ 
    /// <summary>
    /// Pomocná třída pro výpočet viewportu v pixelech.
    /// </summary>
    internal class Viewport
    {
        /// <summary>
        /// Levý spodní roh viewportu v normalizovaných souřadnicích 0..1.
        /// </summary>
        public Vector2 BottomLeft = Vector2.Zero;

        /// <summary>
        /// Pravý horní roh viewportu v normalizovaných souřadnicích 0..1.
        /// </summary>
        public Vector2 TopRight = Vector2.One;

        /// <summary>
        /// Velikost klientské oblasti okna v pixelech.
        /// </summary>
        public Vector2i ClientSize;

        /// <summary>
        /// Přepočítá normalizovaný viewport na pixely.
        /// </summary>
        /// <returns>Pozice a velikost viewportu v pixelech.</returns>
        public (Vector2i position, Vector2i size) GetPixelViewport()
        {
            int left = (int)(BottomLeft.X * ClientSize.X);
            int right = (int)(TopRight.X * ClientSize.X);
            int bottom = (int)(BottomLeft.Y * ClientSize.Y);
            int top = (int)(TopRight.Y * ClientSize.Y);
            int width = right - left;
            int height = top - bottom;
            return (new Vector2i(left, bottom), new Vector2i(width, height));
        }

        /// <summary>
        /// Vrátí poměr stran aktuálního viewportu.
        /// </summary>
        /// <returns>Poměr šířky ku výšce.</returns>
        public float GetAspectRatio()
        {
            (Vector2i position, Vector2i size) = GetPixelViewport();
            if (size.Y <= 0)
            {
                return 1.0f;
            }

            return (float)size.X / size.Y;
        }
    }
}