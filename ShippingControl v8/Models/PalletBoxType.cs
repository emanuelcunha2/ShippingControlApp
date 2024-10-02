using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShippingControl_v8.Models
{
    public class PalletBoxType
    {
        private readonly string palletType;
        private readonly string boxType;

        public PalletBoxType(string palletType, string boxType)
        {
            this.palletType = palletType;
            this.boxType = boxType;
        }

        public string PalletType
        {
            get { return palletType; }
        }

        public string BoxType
        {
            get { return boxType; }
        }
    }
}
