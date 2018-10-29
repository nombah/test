using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.ML.Runtime.TextAnalytics;

namespace WebApplication1.Models
{
    public class MailObject
    {
        [Column("0")]
        public string Subject { get; set; }
        [Column("1")]
        public string Body { get; set; }
        [Column("2")]
        public string Team { get; set; }

    }
}