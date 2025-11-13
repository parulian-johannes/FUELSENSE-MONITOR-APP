using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace EngineMonitoring.Controls
{
    public partial class SpeedometerControl : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(SpeedometerControl),
                new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(double), typeof(SpeedometerControl),
                new PropertyMetadata(0.0, OnRangeChanged));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double), typeof(SpeedometerControl),
                new PropertyMetadata(100.0, OnRangeChanged));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SpeedometerControl),
                new PropertyMetadata("GAUGE", OnTitleChanged));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register("Unit", typeof(string), typeof(SpeedometerControl),
                new PropertyMetadata("UNIT", OnUnitChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double MinValue
        {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        private readonly CultureInfo indonesiaCulture = new CultureInfo("id-ID");

        public SpeedometerControl()
        {
            InitializeComponent();
            Loaded += (s, e) => Initialize();
        }

        private void Initialize()
        {
            DrawScale();
            UpdateNeedle(Value, false);
            UpdateDisplay();
        }

        private void DrawScale()
        {
            ScaleCanvas.Children.Clear();
            
            // Draw tick marks with better positioning for new design
            for (int i = 0; i <= 10; i++)
            {
                double angle = -135 + (i * 27); // 270 degrees / 10 = 27 degrees per tick
                double angleRad = angle * Math.PI / 180;
                
                double outerRadius = 100;
                double innerRadius = i % 2 == 0 ? 88 : 92; // Longer ticks for even numbers
                
                double x1 = 120 + Math.Cos(angleRad) * innerRadius;
                double y1 = 120 + Math.Sin(angleRad) * innerRadius;
                double x2 = 120 + Math.Cos(angleRad) * outerRadius;
                double y2 = 120 + Math.Sin(angleRad) * outerRadius;
                
                var tick = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                    StrokeThickness = i % 2 == 0 ? 2.5 : 1.5
                };
                
                ScaleCanvas.Children.Add(tick);
            }
        }

        private void UpdateNeedle(double value, bool animate)
        {
            double percentage = (value - MinValue) / (MaxValue - MinValue);
            percentage = Math.Max(0, Math.Min(1, percentage));
            double targetAngle = -135 + (percentage * 270);
            
            if (animate)
            {
                var animation = new DoubleAnimation
                {
                    To = targetAngle,
                    Duration = TimeSpan.FromMilliseconds(800),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                NeedleRotate.BeginAnimation(RotateTransform.AngleProperty, animation);
                
                // Sync shadow with needle
                var shadowTransform = NeedleShadow.RenderTransform as TransformGroup;
                if (shadowTransform != null && shadowTransform.Children.Count > 0)
                {
                    var shadowRotate = shadowTransform.Children[0] as RotateTransform;
                    if (shadowRotate != null)
                    {
                        shadowRotate.BeginAnimation(RotateTransform.AngleProperty, animation);
                    }
                }
            }
            else
            {
                NeedleRotate.Angle = targetAngle;
                
                // Sync shadow with needle
                var shadowTransform = NeedleShadow.RenderTransform as TransformGroup;
                if (shadowTransform != null && shadowTransform.Children.Count > 0)
                {
                    var shadowRotate = shadowTransform.Children[0] as RotateTransform;
                    if (shadowRotate != null)
                    {
                        shadowRotate.Angle = targetAngle;
                    }
                }
            }
        }

        private void UpdateDisplay()
        {
            ValueText.Text = Value.ToString("N2", indonesiaCulture);
            UnitText.Text = Unit;
            TitleText.Text = Title;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpeedometerControl control && control.IsLoaded)
            {
                control.UpdateNeedle((double)e.NewValue, true);
                control.UpdateDisplay();
            }
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpeedometerControl control && control.IsLoaded)
            {
                control.DrawScale();
                control.UpdateNeedle(control.Value, false);
            }
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpeedometerControl control && control.IsLoaded)
            {
                control.UpdateDisplay();
            }
        }

        private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SpeedometerControl control && control.IsLoaded)
            {
                control.UpdateDisplay();
            }
        }
    }
}
