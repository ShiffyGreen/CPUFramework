using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPUFramework
{
    public class CPUDevExeption : Exception
    {
        public CPUDevExeption(string? message, Exception? innerexception):base(message, innerexception)
        {

        }
    }
}
