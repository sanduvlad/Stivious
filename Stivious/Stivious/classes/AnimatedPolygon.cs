using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MultipleAnimations
{
    class AnimatedPolygon : IDisposable
    {
        private ThicknessAnimation ta = new ThicknessAnimation();

        private Polygon p = new Polygon();

        private Grid gridRef;


        private double DegreesToRadians(double deg)
        {
            return deg * Math.PI / 180;
        }

        public AnimatedPolygon(int heigth, int width, Color color, Grid grid, int seed, Point startPos, int direction)
        {
            gridRef = grid;
            Random r = new Random(seed);
            var xfrom = r.Next(0, width);
            var yto = r.Next(0, heigth);
            ta.From = new Thickness(startPos.X, startPos.Y, 0, 0);
            ta.To = new Thickness(startPos.X + startPos.X * Math.Cos(DegreesToRadians(direction)) * 5, startPos.Y + startPos.Y * Math.Sin(DegreesToRadians(direction)) * 5, 0, 0);
            ta.Duration = TimeSpan.FromSeconds(r.Next(10, 15));

            
            p.Fill = new SolidColorBrush(color);
            p.Points.Add(new Point(0, 0));
            p.Points.Add(new Point(15, 0));
            p.Points.Add(new Point(15, 15));

            ta.Completed += Ta_Completed;

            gridRef.Children.Add(p);
            gridRef.SetValue(Panel.ZIndexProperty, (int)99);
            p.BeginAnimation(Polygon.MarginProperty, ta);
        }

        private void Ta_Completed(object sender, EventArgs e)
        {
            gridRef.Children.Remove(p);
            this.Dispose();

        }

        public void Dispose()
        {
            p = null;
            ta = null;
            gridRef = null;
        }
    }
}
