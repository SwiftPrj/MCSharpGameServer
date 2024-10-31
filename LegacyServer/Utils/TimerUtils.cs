using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyServer
{
    public class TimerUtils
    {
        public float ms;

        public TimerUtils() 
        {
            ms = 0;
        } 

        public bool Wait(float milliseconds)
        {
            ms++;
            if (ms >= milliseconds)
            {
                Reset();
                return true;
            }
            return false;
        }

        public void Reset() { ms = 0; }
    }
}
