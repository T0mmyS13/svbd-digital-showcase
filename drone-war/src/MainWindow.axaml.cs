using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;


namespace DroneWar
{
    /// <summary>
    /// Hlavní okno aplikace DroneWar.
    /// Zpracovává vstupy od uživatele (klávesnice, myš) a předává je hernímu poli.
    /// 
    /// OVLÁDÁNÍ:
    /// - A/D nebo tažení myší: otáčení děla (azimut)
    /// - W/S nebo kolečko myši: náklon hlavně (zenit)
    /// - +/- nebo pravé tlačítko + kolečko: síla výstřelu
    /// - Mezerník nebo dvojklik na dělo: střelba
    /// </summary>
    public partial class MainWindow : Window
    {
        // ============================================================
        // KONSTANTY - CITLIVOST OVLÁDÁNÍ
        // ============================================================
        
        /// <summary>Jak rychle se otáčí dělo při tažení myší.</summary>
        private const double AzimuthSensitivity = 0.5;
        
        /// <summary>Citlivost kolečka myši pro zenit.</summary>
        private const double ScrollSensitivity = 1.0;
        
        /// <summary>Citlivost kolečka myši pro rychlost střely.</summary>
        private const double VelocitySensitivity = 5.0;
        
        /// <summary>Citlivost tažení hlavně ve spodním panelu.</summary>
        private const double GraphZenithSensitivity = 0.5;

        /// <summary>Krok azimutu při stisku klávesy A/D.</summary>
        private const double KeyStepAzimuth = 1.0;
        
        /// <summary>Krok zenitu při stisku klávesy W/S.</summary>
        private const double KeyStepZenith = 1.0;
        
        /// <summary>Krok rychlosti při stisku klávesy +/-.</summary>
        private const double KeyStepVelocity = 5.0;

        // ============================================================
        // ČLENSKÉ PROMĚNNÉ
        // ============================================================
        
        /// <summary>Reference na herní pole.</summary>
        private readonly BattleField? _battleFieldView;
        
        /// <summary>Zda právě táhneme myší.</summary>
        private bool _isDragging;
        
        /// <summary>Zda táhneme ve spodním panelu.</summary>
        private bool _isDraggingGraph;
        
        /// <summary>Poslední pozice myši.</summary>
        private Point _lastMousePosition;

        // ============================================================
        // KONSTRUKTOR
        // ============================================================
        
        /// <summary>
        /// Vytvoří hlavní okno a nastaví ovládání.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // Najdeme herní pole v XAML podle jména
            _battleFieldView = this.FindControl<BattleField>("BattleFieldView");
            
            SetupWindow();
            
            SetupEventHandlers();
        }

        // ============================================================
        // INICIALIZACE
        // ============================================================
        
        /// <summary>
        /// Nastaví velikost a minimální rozměry okna.
        /// </summary>
        private void SetupWindow()
        {
            Width = 800;
            Height = 600;
            MinWidth = 800;
            MinHeight = 600;
        }

        /// <summary>
        /// Připojí všechny události pro klávesnici a myš.
        /// </summary>
        private void SetupEventHandlers()
        {   
            // Klávesnice
            KeyDown += OnKeyDown;
            
            // Po otevření okna zaměříme herní pole
            Opened += (_, _) => _battleFieldView?.Focus();

            // Myš
            if (_battleFieldView != null)
            {
                _battleFieldView.PointerPressed += OnPointerPressed;
                _battleFieldView.PointerMoved += OnPointerMoved;
                _battleFieldView.PointerReleased += OnPointerReleased;
                _battleFieldView.PointerWheelChanged += OnPointerWheelChanged;
            }
        }

        // ============================================================
        // OBSLUHA MYŠI
        // ============================================================

        /// <summary>
        /// Uvolnění tlačítka myši - konec tažení.
        /// </summary>
        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isDragging = false;
            _isDraggingGraph = false;
        }

        /// <summary>
        /// Stisknutí tlačítka myši - začátek tažení nebo střelba.
        /// </summary>
        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_battleFieldView == null) 
                return;
            
            var point = e.GetCurrentPoint(_battleFieldView);
            bool isLeftButton = point.Properties.IsLeftButtonPressed;

            // Dvojklik na dělo = střelba
            if (isLeftButton && e.ClickCount == 2)
            {
                if (_battleFieldView.IsCannonAtPosition(point.Position))
                {
                    _battleFieldView.Fire();
                    return;
                }
            }

            // Kliknutí ve spodním panelu
            if (isLeftButton && _battleFieldView.IsInBottomPanel(point.Position))
            {
                _isDraggingGraph = true;
                _lastMousePosition = point.Position;
                return;
            }

            // Kliknutí v mapě
            if (isLeftButton)
            {
                _isDragging = true;
                _lastMousePosition = point.Position;
            }
        }

        /// <summary>
        /// Pohyb myši - otáčení děla nebo změna náklonu.
        /// </summary>
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_battleFieldView == null) return;
            
            var point = e.GetCurrentPoint(_battleFieldView);

            // Tažení ve spodním panelu = změna zenitu
            if (_isDraggingGraph)
            {
                double deltaY = point.Position.Y - _lastMousePosition.Y;
                _battleFieldView.AdjustZenith(-deltaY * GraphZenithSensitivity);
                _lastMousePosition = point.Position;
                return;
            }

            // Tažení v mapě = změna azimutu
            if (_isDragging)
            {
                double deltaX = point.Position.X - _lastMousePosition.X;
                _battleFieldView.AdjustAzimuth(deltaX * AzimuthSensitivity);
                _lastMousePosition = point.Position;
            }
        }

        /// <summary>
        /// Kolečko myši - změna zenitu nebo rychlosti.
        /// </summary>
        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (_battleFieldView == null) return;

            var point = e.GetCurrentPoint(_battleFieldView);

            // Pravé tlačítko + kolečko = změna rychlosti
            if (point.Properties.IsRightButtonPressed)
            {
                _battleFieldView.AdjustVelocity(e.Delta.Y * VelocitySensitivity);
            }
            // Pouze kolečko = změna zenitu
            else
            {
                _battleFieldView.AdjustZenith(e.Delta.Y * ScrollSensitivity);
            }

            e.Handled = true;
        }

        // ============================================================
        // OBSLUHA KLÁVESNICE
        // ============================================================

        /// <summary>
        /// Stisknutí klávesy - ovládání děla.
        /// </summary>
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (_battleFieldView == null) return;

            switch (e.Key)
            {
                // Otáčení děla (azimut)
                case Key.A: 
                    _battleFieldView.AdjustAzimuth(-KeyStepAzimuth); 
                    break;
                case Key.D: 
                    _battleFieldView.AdjustAzimuth(KeyStepAzimuth); 
                    break;
                
                // Náklon hlavně (zenit)
                case Key.W: 
                    _battleFieldView.AdjustZenith(KeyStepZenith); 
                    break;
                case Key.S: 
                    _battleFieldView.AdjustZenith(-KeyStepZenith); 
                    break;
                
                // Rychlost střely
                case Key.Add: 
                case Key.OemPlus: 
                    _battleFieldView.AdjustVelocity(KeyStepVelocity); 
                    break;
                case Key.Subtract: 
                case Key.OemMinus: 
                    _battleFieldView.AdjustVelocity(-KeyStepVelocity); 
                    break;
                
                // Střelba
                case Key.Space: 
                    _battleFieldView.Fire(); 
                    break;
            }
            
            e.Handled = true;
        }
    }
}