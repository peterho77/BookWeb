﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Book.Models.Vnpay
{
    public class PaymentInformationModel
    {
        public int OrderHeaderId { get; set; }
		public string OrderType { get; set; }
		public double Amount { get; set; }
        public string OrderDescription { get; set; }
        public string Name { get; set; }
    }
}
