using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using DroneWar.Models;
using SkiaSharp;

namespace DroneWar;

/// <summary>
/// Herní pole - hlavní komponenta hry DroneWar.
/// Tato třída řídí celou herní smyčku a vykreslování.
/// </summary>
public partial class BattleField : UserControl
{
    
    /// <summary>Interval aktualizace hry.</summary>
    private const double UpdateInterval = 0.01;
    
    // ============================================================
    // HERNÍ OBJEKTY
    // ============================================================
    
    /// <summary>Dělo (hráčova jednotka).</summary>
    private readonly Cannon? _cannon;
    
    /// <summary>Nepřátelský dron (cíl).</summary>
    private readonly Drone? _drone;
    
    /// <summary>Aktuální střela (null = žádná střela ve vzduchu).</summary>
    private Projectile? _projectile;
    
    /// <summary>Zda byla mapa aktualizována po posledním dopadu střely.</summary>
    private bool _terrainNeedsRefresh;
    
    /// <summary>Vítr ovlivňující střelu.</summary>
    private readonly Wind? _wind;
    
    /// <summary>Data načteného scénáře (mapa, pozice objektů).</summary>
    private readonly ScenarioData? _scenarioData;
    
    /// <summary>Výšková mapa terénu pro vykreslování.</summary>
    private readonly TerrainMap? _terrainMap;
    
    /// <summary> Spodní panel.</summary>
    private readonly TerrainProfile? _terrainProfile;
    
    /// <summary>Časovač pro herní smyčku.</summary>
    private readonly DispatcherTimer _timer;

    // ============================================================
    // KONSTRUKTOR
    // ============================================================
    
    /// <summary>
    /// Vytvoří herní pole a inicializuje všechny herní objekty.
    /// </summary>
    public BattleField()
    {
        InitializeComponent();

        // 1. Načtení dat scénáře ze souboru
        _scenarioData = ScenarioLoader.LoadScenario(Program.SelectedScenario);

        // 2. Vytvoření herních objektů ze scénáře
        _cannon = _scenarioData.CreateCannon();
        _drone = _scenarioData.CreateDrone();
        _wind = _scenarioData.CreateWind();

        // 3. Vytvoření mapy terénu
        _terrainMap = new TerrainMap(_scenarioData);
        
        // 4. Vytvoření spodního panelu
        _terrainProfile = new TerrainProfile();

        // 5. Nastavení cíle dronu (letí k dělu)
        _drone.SetTarget(_cannon.X, _cannon.Y, _cannon.Elevation);
        
        // 6. Spuštění herní smyčky (časovač volá OnTimerTick)
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(UpdateInterval) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    // ============================================================
    // VEŘEJNÉ METODY PRO OVLÁDÁNÍ
    // ============================================================
    
    /// <summary>
    /// Upraví azimut děla.
    /// </summary>
    /// <param name="delta">Změna ve stupních (+ = doprava, - = doleva).</param>
    public void AdjustAzimuth(double delta)
    {
        if (_cannon == null) 
            return;
        
        // Nelze měnit během letu střely
        if (_projectile != null && _projectile.IsActive()) 
            return;
        
        _cannon.AdjustAzimuth(delta);
        InvalidateVisual(); 
    }

    /// <summary>
    /// Upraví zenit hlavně.
    /// </summary>
    /// <param name="delta">Změna ve stupních (+ = nahoru, - = dolů).</param>
    public void AdjustZenith(double delta)
    {
        if (_cannon == null) 
            return;
        
        if (_projectile != null && _projectile.IsActive()) 
            return;
        
        _cannon.AdjustZenith(delta);
        InvalidateVisual();
    }

    /// <summary>
    /// Upraví rychlost střely.
    /// </summary>
    /// <param name="delta">Změna v m/s.</param>
    public void AdjustVelocity(double delta)
    {
        if (_cannon == null) 
            return;
        
        if (_projectile != null && _projectile.IsActive()) 
            return;
        
        _cannon.AdjustVelocity(delta);
        InvalidateVisual();
    }

    /// <summary>
    /// Vystřelí střelu z děla.
    /// </summary>
    public void Fire()
    {
        if (_cannon == null) 
            return;
        
        if (_cannon.IsDestroyed)
            return;
        
        if (_cannon.InitialVelocity <= 0)
        {
            return;
        }
        
        // Střelit pouze pokud není žádná aktivní střela
        if (_projectile == null || !_projectile.IsActive())
        {
            _projectile = Projectile.Create(_cannon);
        }
    }
    
    /// <summary>
    /// Zjistí, zda je daný bod na obrazovce blízko děla.
    /// </summary>
    public bool IsCannonAtPosition(Point screenPoint)
    {
        if (_cannon == null || _scenarioData == null) 
            return false;

        var metrics = CalculateMetrics();
        
        // Ignorujeme kliknutí ve spodním panelu
        if (screenPoint.Y > metrics.MapBounds.Height) return false;

        // Pozice děla na obrazovce
        double cannonScreenX = metrics.OffsetX + (_cannon.X * metrics.PixelPerMeter);
        double cannonScreenY = metrics.OffsetY + (_cannon.Y * metrics.PixelPerMeter);
        
        // Vzdálenost kliknutí od středu děla
        double distance = Math.Sqrt(
            Math.Pow(screenPoint.X - cannonScreenX, 2) + 
            Math.Pow(screenPoint.Y - cannonScreenY, 2));

        // Tolerance kliknutí (minimálně 20 pixelů)
        double clickRadius = Math.Max(20, 5.0 * metrics.PixelPerMeter);

        return distance < clickRadius;
    }
    
    /// <summary>
    /// Zjistí, zda je daný bod ve spodním panelu.
    /// </summary>
    public bool IsInBottomPanel(Point screenPoint)
    {
        float bottomPanelTop = (float)Bounds.Height - TerrainProfile.PanelHeight;
        return screenPoint.Y >= bottomPanelTop;
    }

    // ============================================================
    // HERNÍ SMYČKA
    // ============================================================

    /// <summary>
    /// Aktualizuje stav hry - volá se každých 10ms.
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_scenarioData == null || _cannon == null || _drone == null || _wind == null)
            return;
        
        _wind.Update(UpdateInterval);
        
        _cannon.CurrentExplosion?.Update(UpdateInterval);
        _drone.CurrentExplosion?.Update(UpdateInterval);
        _projectile?.CurrentExplosion?.Update(UpdateInterval);
        
        if (!_drone.IsDestroyed)
        {
            _drone.Update(UpdateInterval, _scenarioData, _cannon);
        }
        
        _projectile?.Update(UpdateInterval, _wind, _scenarioData, _drone);
        
        if (_projectile != null)
        {
            bool wasActive = _projectile.IsActive();
            _projectile.Update(UpdateInterval, _wind!, _scenarioData, _drone);
            
            if (wasActive && !_projectile.IsActive())
            {
                _terrainNeedsRefresh = true;
            }
        }
        
        if (_terrainNeedsRefresh && _terrainMap != null)
        {
            _terrainMap.RefreshBitmap();
            _terrainNeedsRefresh = false;
        }
        
        InvalidateVisual();
    }
    
    // ============================================================
    // VYKRESLOVÁNÍ
    // ============================================================
    
    /// <summary>
    /// Vykreslí celé herní pole.
    /// </summary>
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        
        if (_cannon == null || _drone == null || _scenarioData == null || _terrainMap == null || _terrainProfile == null) 
            return;
        
        var metrics = CalculateMetrics();
        
        var customDrawOp = new SkiaBattleFieldRenderer(
            Bounds,
            metrics,
            _cannon,
            _drone,
            _projectile,
            _wind,
            _scenarioData,
            _terrainMap,
            _terrainProfile
        );
        
        context.Custom(customDrawOp);
    }

    // ============================================================
    // POMOCNÉ METODY
    // ============================================================
    
    /// <summary>
    /// Struktura obsahující vypočítané rozměry pro vykreslování.
    /// </summary>
    private struct ViewMetrics
    {
        /// <summary>Počet pixelů na jeden metr.</summary>
        public float PixelPerMeter;
        
        /// <summary>Horizontální posun mapy (pro centrování).</summary>
        public float OffsetX;
        
        /// <summary>Vertikální posun mapy.</summary>
        public float OffsetY;
        
        /// <summary>Šířka obsahu v pixelech.</summary>
        public float ContentWidth;
        
        /// <summary>Oblast pro mapu (bez spodního panelu).</summary>
        public Rect MapBounds;
    }
    
    /// <summary>
    /// Vypočítá metriky (zoom, posun) na základě velikosti okna.
    /// </summary>
    private ViewMetrics CalculateMetrics()
    {
        if (_scenarioData == null) return new ViewMetrics();

        // Výška spodního panelu
        float bottomPanelHeight = TerrainProfile.PanelHeight;
        var mapRect = new Rect(0, 0, Bounds.Width, Bounds.Height - bottomPanelHeight);

        const float padding = 30;
        
        // Rozměry světa v metrech
        float worldWidth = _scenarioData.Width * _scenarioData.Dx;
        float worldHeight = _scenarioData.Height * _scenarioData.Dy;

        // Dostupné místo v okně (bez legendy)
        float availableWidth = (float)mapRect.Width - 2 * padding - TerrainMap.LegendTotalWidth;
        float availableHeight = (float)mapRect.Height - 2 * padding;

        if (availableWidth <= 0 || availableHeight <= 0) return new ViewMetrics();

        // Výpočet měřítka (zachování poměru stran)
        float scaleX = availableWidth / worldWidth;
        float scaleY = availableHeight / worldHeight;
        float pixelPerMeter = Math.Min(scaleX, scaleY);

        // Centrování obsahu
        float contentWidth = worldWidth * pixelPerMeter;
        float contentHeight = worldHeight * pixelPerMeter;
        float offsetX = padding + (availableWidth - contentWidth) / 2;
        float offsetY = padding + (availableHeight - contentHeight) / 2;

        return new ViewMetrics
        {
            PixelPerMeter = pixelPerMeter,
            OffsetX = offsetX,
            OffsetY = offsetY,
            ContentWidth = contentWidth,
            MapBounds = mapRect
        };
    }
    
    // ============================================================
    // VLASTNÍ SKIA RENDERER
    // ============================================================
    
    /// <summary>
    /// Vlastní renderer využívající SkiaSharp pro vykreslení herního pole.
    ///  </summary>
    private sealed class SkiaBattleFieldRenderer(
        Rect bounds,
        ViewMetrics metrics,
        Cannon cannon,
        Drone drone,
        Projectile? projectile,
        Wind? wind,
        ScenarioData scenarioData,
        TerrainMap terrainMap,
        TerrainProfile terrainProfile)
        : ICustomDrawOperation
    {
        public Rect Bounds => bounds;

        /// <summary>
        /// Hlavní vykreslovací metoda.
        /// </summary>
        public void Render(ImmediateDrawingContext context)
        {
            // Získáme SkiaSharp canvas
            var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
            if (leaseFeature == null) return;
            
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            
            canvas.Clear(SKColors.White);
            
            canvas.Save();
            canvas.Translate(metrics.OffsetX, metrics.OffsetY);
            
            terrainMap.DrawTerrain(canvas, metrics.PixelPerMeter);
            cannon.Draw(canvas, metrics.PixelPerMeter);

            double projectileZ;
            
            if (projectile != null)
            {
                projectileZ = projectile.ProjectileZ();
            }
            else
            {
                projectileZ = 0.0;
            }
                
            if (drone.Z < projectileZ)
            {
                drone.Draw(canvas, cannon, metrics.PixelPerMeter);
                projectile?.Draw(canvas, metrics.PixelPerMeter);
            } 
            else
            {
                projectile?.Draw(canvas, metrics.PixelPerMeter);
                drone.Draw(canvas, cannon, metrics.PixelPerMeter);
            }
            
            
            
            canvas.Restore();
            
            float mapRightEdge = metrics.OffsetX + metrics.ContentWidth;
            
            wind?.Draw(canvas, bounds, mapRightEdge, metrics.OffsetY);
            
            terrainMap.DrawLegend(canvas, bounds, mapRightEdge, metrics.OffsetY);
            
            terrainProfile.Draw(canvas, bounds, cannon, scenarioData, projectile);
        }
        
        // Povinné metody rozhraní ICustomDrawOperation
        public void Dispose() { }
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public bool HitTest(Point p) => true;
    }
}
