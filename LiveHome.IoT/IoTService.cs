﻿using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.CharacterLcd;
using Iot.Device.DHTxx;
using Iot.Device.Pcx857x;
using LiveHome.IoT.Devices;
using UnitsNet;

namespace LiveHome.IoT
{
    public static class IoTService
    {
        public static event Action<(double, double)> EnvironmentInfoChanged;
        public static event Action<bool> CombustibleGasInfoChanged;
        private static (double, double) lastEnvInfo;
        private static bool lastGasInfo;

        /// <summary>
        /// 获取当前的环境信息
        /// </summary>
        /// <returns>一个元组,第一项为温度(摄氏度),第二项为湿度(相对湿度,以百分数表示)</returns>
        public static Task<(double, double)> GetEnvironmentInfo(int gpioPin = 26)
        {
            Console.WriteLine($"{DateTime.Now} > [IoTService:环境信息]初始化...");
            return Task.Run(() =>
            {
                (double, double) val = GetEnvInfo();
                if (lastEnvInfo != val)
                {
                    EnvironmentInfoChanged?.Invoke(val);
                }
                lastEnvInfo = val;
                return val;
            });

            (double, double) GetEnvInfo()
            {
                using (Dht11 dht11 = new Dht11(gpioPin))
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
        public static Task<bool> DetectCombustibleGas(int gpioPin = 17)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"{DateTime.Now} > [IoTService:可燃气体检测]初始化...");
                using (MQ2 mq2 = new MQ2(gpioPin))
                {
                    bool state = mq2.IsCombustibleGasDetected;
                    if (lastGasInfo != state)
                    {
                        CombustibleGasInfoChanged?.Invoke(state);
                    }
                    lastGasInfo = state;
                    Console.WriteLine($"{DateTime.Now} > [IoTService:可燃气体检测]读取完成,结果为{state}。");
                    return state;
                }
            });
        }

        public static Task WriteTextToLCD(string text, int gpioPin = 114514, int line = 0)
        {
            return new Task(() =>
            {
                //TODO: LCD!!!
                I2cDevice i2c = I2cDevice.Create(new I2cConnectionSettings(1, 0x27));
                Pcf8574 driver = new Pcf8574(i2c);
                Lcd2004 lcd = new Lcd2004(registerSelectPin: 0,
                                        enablePin: 2,
                                        dataPins: new int[] { 4, 5, 6, 7 },
                                        backlightPin: 3,
                                        backlightBrightness: 0.1f,
                                        readWritePin: 1,
                                        controller: new GpioController(PinNumberingScheme.Logical, driver));
                int currentLine = 0;

                while (true)
                {
                    lcd.Clear();
                    lcd.SetCursorPosition(0, currentLine);
                    lcd.Write(text);
                    currentLine = (currentLine == 3) ? 0 : currentLine + 1;
                    Thread.Sleep(1000);
                }
            });
        }

        public static Task LightOnOrOffLED(bool isLedLight , int gpioPin = 27)
        {
            return new Task(() =>
            {
                using (GpioController controller = new GpioController())
                {
                    controller.OpenPin(gpioPin, PinMode.Output);
                    if (isLedLight)
                    {
                        controller.Write(gpioPin, PinValue.High);
                    }
                    else
                    {
                        controller.Write(gpioPin, PinValue.Low);
                    }
                }
            });
        }
    }
}
