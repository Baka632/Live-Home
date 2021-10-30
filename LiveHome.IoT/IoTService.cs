using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Common;
using Iot.Device.DHTxx;
using LiveHome.IoT.Devices;
using UnitsNet;

namespace LiveHome.IoT
{
    public static class IoTService
    {
        /// <summary>
        /// 获取当前的环境信息
        /// </summary>
        /// <returns>一个元组,第一项为温度(摄氏度),第二项为湿度(相对湿度,以百分数表示),第三项为炎热指数</returns>
        public static Task<(double, double, double)> GetEnvironmentInfo()
        {
            return Task.Run(() => GetEnvInfo());

            (double, double, double) GetEnvInfo()
            {
                // HACK: 更改Dht11传感器的Gpio pin
                using (Dht11 dht11 = new Dht11(26))
                {
                    Temperature temperature = dht11.Temperature;
                    RelativeHumidity humidity = dht11.Humidity;
                    if (dht11.IsLastReadSuccessful)
                    {
                        //读取成功,返回值
                        double temp = temperature.DegreesCelsius;
                        double rh = humidity.Percent;
                        double heatIndex = WeatherHelper.CalculateHeatIndex(temperature, humidity).DegreesCelsius;
                        return (temp, rh, heatIndex);
                    }
                    else
                    {
                        //第一次读取失败,再尝试9次
                        for (int i = 0; i < 10; i++)
                        {
                            Temperature temperature1 = dht11.Temperature;
                            RelativeHumidity humidity1 = dht11.Humidity;
                            if (dht11.IsLastReadSuccessful)
                            {
                                //读取成功就返回值
                                double temp = temperature1.DegreesCelsius;
                                double rh = humidity1.Value;
                                double heatIndex = WeatherHelper.CalculateHeatIndex(temperature, humidity).DegreesCelsius;
                                return (temp, rh, heatIndex);
                            }
                            else
                            {
                                //读取失败就等待一秒然后再读取
                                Thread.Sleep(1000);
                            }
                        }
                        //9次尝试后仍失败就返回NaN
                        return (double.NaN, double.NaN, double.NaN);
                    }
                }
            }
        }

        /// <summary>
        /// 侦测当前环境是否有可燃气体
        /// </summary>
        /// <returns>如果当前环境发现可燃气体,则返回true,否则返回false</returns>
        public static Task<bool> DetectCombustibleGas()
        {
            return Task.Run(() =>
            {
                // HACK: 更改MQ-2传感器的Gpio pin
                using (MQ2 mq2 = new MQ2(26))
                {
                    bool state = mq2.IsCombustibleGasDetected;
                    return state;
                }
            });
        }
    }
}
