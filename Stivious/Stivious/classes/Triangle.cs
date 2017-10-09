using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace Stivious.classes
{
    class Triangle
    {
        public Point a { get { return a; } set { a = value; } }
        public Point b { get { return b; } set { b = value; } }
        public Point c { get { return c; } set { c = value; } }

        public bool HasAnimatedProperties
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Triangle()
        {
            a = new Point();
            b = new Point();
            c = new Point();
        }

        public Triangle(Point a, Point b, Point c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }
}