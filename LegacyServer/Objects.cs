using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyServer
{
    public static class Objects
    {
        public static string[] objects = { "par_solid", "obj_barrel", "obj_projectile" };

        public static string[] GetObjects()
        {
            return objects;
        }
    }
}
