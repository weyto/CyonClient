using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CyonClient
{
    public class DNSRecord
    {   
        public string Name { get; set; }
        public DNSTTL TTL { get; set; }
        public DNSType Type { get; set; }
        public string Value { get; set; }
        internal string RecordHash { get; set; }        
    }

    public enum DNSType
    {
        A, AAAA, CNAME, MX, TXT, SRV
    }

    public enum DNSTTL
    {
        QuarterHour = 900, OneHour = 3600, FourHours = 14400, OneDay = 86400
    }    
}
