using Newtonsoft.Json;

namespace T3_LeandroBras_22100770
{
    public class MandelbrotStats
    {
        [JsonProperty("ETime")]
        public double ETime { get; private set; }
        [JsonProperty("MRes")]
        public string MRes { get; private set; }
        [JsonProperty("IsTaskBased")]
        public CalcType Type { get; private set; }

        public MandelbrotStats(double eTime, string mRes, CalcType calcType)
        {
            ETime = eTime;
            MRes = mRes;
            Type = calcType;
        }

        public override string ToString()
        {
            return $"Size: {MRes}, Time: {ETime.ToString("0.00")} ms, Type: {Type}";
        }
    }
}
