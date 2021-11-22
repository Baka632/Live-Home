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
        /// <returns>一个元组,第一项为温度(摄氏度),第二项为湿度(相对湿度,以百分数表示)</returns>
        public static Task<(double, double)> GetEnvironmentInfo()
        {
            Console.WriteLine($"{DateTime.Now} > [IoTService:环境信息]初始化...");
            return Task.Run(() => GetEnvInfo());

            (double, double) GetEnvInfo()
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
                        Console.WriteLine($"{DateTime.Now} > [IoTService:环境信息]读取成功\n温度为:{temp:0.#}℃\n湿度为:{rh:0.#}%");
                        return (temp, rh);
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now} > [IoTService:环境信息]第一次读取失败");
                        //第一次读取失败,再尝试3次
                        for (int i = 0, b = 2; i < 4; i++, b++)
                        {
                            Temperature temperature1 = dht11.Temperature;
                            RelativeHumidity humidity1 = dht11.Humidity;
                            if (dht11.IsLastReadSuccessful)
                            {
                                //读取成功就返回值
                                double temp = temperature1.DegreesCelsius;
                                double rh = humidity1.Percent;
                                Console.WriteLine($"{DateTime.Now} > [IoTService:环境信息]读取成功\n温度为:{temp:0.#}℃\n湿度为:{rh:0.#}%");
                                return (temp, rh);
                            }
                            else
                            {
                                Console.WriteLine($"{DateTime.Now} > [IoTService:环境信息]第{b}次读取失败,等待2.5秒...");
                                //读取失败就等待2.5秒然后再读取
                                Thread.Sleep(2500);
                            }
                        }
                        Console.WriteLine($"{DateTime.Now} > [IoTService:环境信息]读取失败次数过多,返回NaN,过程结束。");
                        //3次尝试后仍失败就返回NaN
                        return (double.NaN, double.NaN);
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
                Console.WriteLine($"{DateTime.Now} > [IoTService:可燃气体检测]初始化...");
                // HACK: 更改MQ-2传感器的Gpio pin
                using (MQ2 mq2 = new MQ2(17))
                {
                    bool state = mq2.IsCombustibleGasDetected;
                    Console.WriteLine($"{DateTime.Now} > [IoTService:可燃气体检测]读取完成,结果为{state}。");
                    return state;
                }
            });
        }

        public static Task WriteTextToLCD(string text)
        {
            return new Task(() =>
            {
                //TODO: LCD!!!
            });
        }

        public static Task LightOnOrOffLED(bool isLedLight)
        {
            return new Task(() =>
            {
                //HACK: 更改LED的Gpio pin
                int pin = 27;
                using (GpioController controller = new GpioController())
                {
                    controller.OpenPin(pin, PinMode.Output);
                    if (isLedLight)
                    {
                        controller.Write(pin, PinValue.High);
                    }
                    else
                    {
                        controller.Write(pin, PinValue.Low);
                    }
                }
            });
        }
    }
}
