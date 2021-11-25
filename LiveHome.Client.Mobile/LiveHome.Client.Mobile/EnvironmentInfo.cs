using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveHome.Client.Mobile
{
    public class EnvironmentInfo
    {
        /// <summary>
        /// 以摄氏度所表示的温度
        /// </summary>
        public double Temperature { get; set; }
        /// <summary>
        /// 相对湿度
        /// </summary>
        public double RelativeHumidity { get; set; }
    }
}
