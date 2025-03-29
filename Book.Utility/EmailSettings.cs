using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Book.Utility
{
    public class EmailSettings
    {
        public string? SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string? FromName { get; set; }
        public string? FromEmail { get; set; }
        public string? Password { get; set; }
    }
}
