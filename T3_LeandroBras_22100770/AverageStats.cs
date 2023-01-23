using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3_LeandroBras_22100770
{
    internal class AverageStats
    {
        public int Num { get; set; }
        public double Avg { get; set; }
        public string Size { get; set; }
        public CalcType Type { get; }

        public AverageStats(CalcType type)
        {
            Num = 0;
            Avg = 0;
            Size = "";
            Type = type;
        }
        
        public override string ToString()
        {
            string space = "";
            for (int i = 0; i < ((13 - (Avg.ToString("0.00") + " ms").Length) / 2); i++)
                space += " ";

            return String.Format($"| {Size} |" +
                $"{space}{Avg.ToString("0.00")} ms{space}");
        }
    }
}
