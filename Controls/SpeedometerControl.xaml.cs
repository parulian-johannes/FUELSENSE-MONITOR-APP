using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FuelsenseMonitorApp.Controls
{
    public partial class SpeedometerControl : UserControl
    {
        private static readonly string[] ColorTags = { "Blue", "Purple", "Cyan", "Orange", "Green" };
        private static int ColorIndex = 0;

        public SpeedometerControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            
            // Auto-assign color tag if not set
            if (string.IsNullOrEmpty(Tag?.ToString()))
            {
                Tag = ColorTags[ColorIndex % ColorTags.Length];
                ColorIndex++;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CreateSpeedometer();
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(SpeedometerControl),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(double), typeof(SpeedometerControl),
                new PropertyMetadata(0.0));

        public double MinValue
        {
            get { return (double)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double), typeof(SpeedometerControl),
                new PropertyMetadata(100.0));

        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(SpeedometerControl),
                new PropertyMetadata("Speed"));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register("Unit", typeof(string), typeof(SpeedometerControl),
                new PropertyMetadata("km/h"));

        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SpeedometerControl)d;
            control.UpdateNeedle();
        }

        private void CreateSpeedometer()
        {
            if (ScaleMarks != null)
            {
                CreateScaleMarks();
            }
            UpdateNeedle();
        }

        private void CreateScaleMarks()
        {
            ScaleMarks.Children.Clear();
            
            double radius = 220;  // Increased for 600px size
            double centerX = 300; // Center for 600px
            double centerY = 300; // Center for 600px

            for (int i = 0; i <= 10; i++)
            {
                double angle = -225 + (i * 27);
                double radians = angle * Math.PI / 180;

                double x1 = centerX + (radius - 35) * Math.Cos(radians); // Larger tick marks
                double y1 = centerY + (radius - 35) * Math.Sin(radians);
                double x2 = centerX + (radius - 18) * Math.Cos(radians); 
                double y2 = centerY + (radius - 18) * Math.Sin(radians);

                var tick = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = new SolidColorBrush(Color.FromRgb(204, 204, 204)), // #CCCCCC
                    StrokeThickness = 5  // Thicker for 600px speedometer
                };

                ScaleMarks.Children.Add(tick);
            }
        }

        private void UpdateNeedle()
        {
            var needleTransform = this.FindName("NeedleTransform") as RotateTransform;
            if (needleTransform == null) return;

            double percentage = (Value - MinValue) / (MaxValue - MinValue);
            if (percentage < 0) percentage = 0;
            if (percentage > 1) percentage = 1;

            // Calculate angle: -135° to +135° (270° total range)
            double angle = -135 + (percentage * 270);
            
            // Smooth animation for needle movement
            var animation = new System.Windows.Media.Animation.DoubleAnimation()
            {
                From = needleTransform.Angle,
                To = angle,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase()
            };
            
            needleTransform.BeginAnimation(RotateTransform.AngleProperty, animation);

            if (ValueText != null)
            {
                ValueText.Text = Value.ToString("F1");
            }
        }
    }
}
