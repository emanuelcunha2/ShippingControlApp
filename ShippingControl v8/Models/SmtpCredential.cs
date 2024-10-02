using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ShippingControl_v8.Models
{
    public class SmtpCredential : NetworkCredential
    { 
        public SmtpCredential(string userName, string password, string domain) 
        {
            this.UserName = userName;
            this.Password = password;
            this.Domain = domain;
        }
    }
}
