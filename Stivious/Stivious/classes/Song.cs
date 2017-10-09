using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stivious.classes
{
    class Song : IComparable
    {
        public string name { get; set; }

        public TimeSpan duration { get; set; }

        public string path { get; set; }

        public override string ToString()
        {
            return name;
        }

        public int CompareTo(object obj)
        {
            return this.path.CompareTo(((Song)obj).path);
        }
    }
}
