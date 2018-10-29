using Microsoft.ML.Runtime.Api;

namespace WebApplication1.Models
{
    public class TeamPrediction
    {
        [ColumnName("Score")]
        public string Team { get; set; }
    }
}