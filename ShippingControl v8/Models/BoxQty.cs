using ShippingControl_v8.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShippingControl_v8.Models
{
    public class BoxQty : ViewModelBase
    {
        private string pn = "";
        private string box = "";
        private int qty = 0;

        public BoxQty(string pn, string box, int qty)
        {
            this.pn = pn;
            this.box = box;
            this.qty = qty;
        }

        public string PN
        {
            get { return pn; }
            set { pn = value; OnPropertyChanged(); }
        }

        public string Box
        {
            get { return box; }
            set { box = value; OnPropertyChanged(); }
        }

        public int Qty
        {
            get { return qty; }
            set { qty = value; OnPropertyChanged(); }
        }
        private Color _backgroundColor = Colors.Transparent;
        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set { _backgroundColor = value; OnPropertyChanged();}
        }
    }
}
