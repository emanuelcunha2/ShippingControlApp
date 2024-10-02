using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShippingControl_v8.Models
{
    public class WHException : Exception
    {
        public WHException() { }
        public WHException(string message) : base(message) { }
        public WHException(string message, Exception inner) : base(message, inner) { }
    }
}
