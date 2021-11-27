using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.CharacterLcd;
using Iot.Device.DHTxx;
using Iot.Device.Pcx857x;
using Iot.Device.Ssd13xx;
using Iot.Device.Ssd13xx.Commands;
using Iot.Device.Ssd13xx.Commands.Ssd1306Commands;
using LiveHome.IoT.Devices;
using QRCoder;
using UnitsNet;

namespace LiveHome.IoT
{
    public static class IoTService
    {
        public static event Action<(double, double)> EnvironmentInfoChanged;
        public static event Action<bool> CombustibleGasDetected;
        private static (double, double) lastEnvInfo;

        public static (double, double) LastSuccessEnvInfo => lastEnvInfo;
        public static bool IsBuzzerOn { get; private set; }

        static IoTService()
        {
            Log("IoTService:类型对象构造器", "\n时代。时代在卡西米尔浓缩为了一个词，我们叫它卡瓦莱利亚基，我们也叫它大骑士领。\n新一届卡西米尔骑士特别锦标赛即将打响，我们用荣耀的模具，浇灌出了一场又一场闹剧。\n城市流光溢彩，霓虹灯征服了天空。金币叮叮作响，闪闪发亮，让人看不清那些小人物的面目。\n黎明还未升起。\n但长夜将尽。");
        }

        public static void RecognizeQRCode()
        {
            Log("IoTService:QR Code Recognizer", "初始化...");
        }

        /// <summary>
        /// 获取当前的环境信息
        /// </summary>
        /// <returns>一个元组,第一项为温度(摄氏度),第二项为湿度(相对湿度,以百分数表示)</returns>
        public static Task<(double, double)> GetEnvironmentInfo(int gpioPin = 4)
        {
            Log("IoTService:环境信息", "初始化...");
            return Task.Run(() =>
            {
                (double, double) val = GetEnvInfo();
                if (lastEnvInfo != val && !double.IsNaN(val.Item1) && !double.IsNaN(val.Item2))
                {
                    EnvironmentInfoChanged?.Invoke(val);
                    lastEnvInfo = val;
                }
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
                        Log("IoTService:环境信息", $"读取成功\n温度为:{temp}℃\n湿度为:{rh}%");
                        return (temp, rh);
                    }
                    else
                    {
                        Log("IoTService:环境信息", $"第1次读取失败");
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
                                Log("IoTService:环境信息", $"读取成功\n温度为:{temp:0.#}℃\n湿度为:{rh:0.#}%");
                                return (temp, rh);
                            }
                            else
                            {
                                Log("IoTService:环境信息", $"第{b}次读取失败,等待2.5秒...");
                                //读取失败就等待2.5秒然后再读取
                                Thread.Sleep(2500);
                            }
                        }
                        Log("IoTService:环境信息", $"读取失败次数过多,返回NaN,过程结束。");
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
        public static Task<bool> DetectCombustibleGas(int gpioPin = 17, bool makeNoise = true)
        {
            return Task.Run(async () =>
            {
                Log("IoTService:可燃气体检测", "初始化...");
                using (MQ2 mq2 = new MQ2(gpioPin))
                {
                    bool state = mq2.IsCombustibleGasDetected;
                    if (state)
                    {
                        CombustibleGasDetected?.Invoke(state);
                        if (makeNoise)
                        {
                            await OnOrOffBuzzer(true);
                        }
                    }
                    else
                    {
                        if (IsBuzzerOn)
                        {
                            await OnOrOffBuzzer(false);
                        }
                    }
                    Log("IoTService:可燃气体检测", $"读取完成,结果为{state}。");
                    return state;
                }
            });
        }

        public static Task WriteEnviromentInfoToLCD(int pin = 1, int deviceAdress = 0x78)
        {
            return Task.Run(() =>
            {
                Log("IoTService:LCD", "初始化...");
                //TODO: LCD!!!
                List<byte[]> testMessage = new List<byte[]>()
                {
                    new byte[]
                    {
                        0x10,0x22,0x84,0x00,0x00,0x7F,0x49,0x49,0x49,0x49,0x7F,0x00,0x00,0x00,0x18,0x06,0x01,0x20,0x3F,0x21,0x3F,0x21,0x21,0x3F,0x21,0x3F,0x20,0x00
                    },//温
                    new byte[]
                    {
                        0x00,0x00,0xFC,0x14,0x14,0x7C,0x55,0x56,0x54,0x54,0x7C,0x14,0x14,0x00,0x20,0x18,0x07,0x20,0x21,0x13,0x15,0x09,0x09,0x15,0x13,0x21,0x20,0x00
                    },//度
                    new byte[]{0x00,0x00,0x00,0x60,0x60,0x00,0x00,0x00,0x00,0x00,0x0C,0x0C,0x00,0x00},//:
                    new byte[]{0x00,0x08,0x08,0xFC,0x00,0x00,0x00,0x00,0x08,0x08,0x0F,0x08,0x08,0x00},//1
                    new byte[]{0x00,0x08,0x08,0xFC,0x00,0x00,0x00,0x00,0x08,0x08,0x0F,0x08,0x08,0x00},//1
                    new byte[]{0x00,0x80,0x60,0x10,0xFC,0x00,0x00,0x00,0x01,0x01,0x09,0x0F,0x09,0x00},//4
                    new byte[]{0x00,0x7C,0x24,0x24,0x24,0xC4,0x00,0x00,0x06,0x08,0x08,0x08,0x07,0x00},//5
                    new byte[]{0x00,0x08,0x08,0xFC,0x00,0x00,0x00,0x00,0x08,0x08,0x0F,0x08,0x08,0x00},//1
                    new byte[]{0x00,0x80,0x60,0x10,0xFC,0x00,0x00,0x00,0x01,0x01,0x09,0x0F,0x09,0x00},//4
                    new byte[]
                    {
                        0x06,0x09,0x09,0xF6,0x08,0x04,0x04,0x04,0x04,0x04,0x04,0x08,0x3C,0x00,0x00,0x00,0x00,0x07,0x08,0x10,0x10,0x10,0x10,0x10,0x10,0x08,0x04,0x00
                    },//℃
                };
                using (I2cDevice i2CDevice = I2cDevice.Create(new I2cConnectionSettings(pin, deviceAdress)))
                {
                    using (Ssd1306 device = new Ssd1306(i2CDevice))
                    {
                        device.SendCommand(new SetDisplayOffset(0x00));
                        device.SendCommand(new SetDisplayStartLine(0x00));
                        device.SendCommand(
                            new SetMemoryAddressingMode(SetMemoryAddressingMode.AddressingMode
                                .Horizontal));
                        device.SendCommand(new SetNormalDisplay());
                        device.SendCommand(new SetDisplayOn());
                        device.SendCommand(new SetColumnAddress());
                        device.SendCommand(new SetPageAddress(PageAddress.Page0,
                            PageAddress.Page3));
                        foreach (byte[] character in testMessage)
                        {
                            device.SendData(character);
                        }
                        Log("IoTService:LCD", "Debug complete :)");
                        Console.ReadKey();
                    }
                }
            });
        }

        public static Task OnOrOffBuzzer(bool isBuzzerOn , int gpioPin = 14)
        {
            return Task.Run(() =>
            {
                Log("IoTService:蜂鸣器", "初始化...");
                Log("IoTService:蜂鸣器", "Message from Justice Knight:滴滴,滴滴滴,滴滴......滴!");
                using (GpioController gpioController = new GpioController(PinNumberingScheme.Logical))
                {
                    gpioController.OpenPin(14, PinMode.Output);
                    if (isBuzzerOn)
                    {
                        Log("IoTService:蜂鸣器", "启动");
                        Log("IoTService:蜂鸣器", "Message from Justice Knight:滴滴、滴滴滴——");
                        gpioController.Write(gpioPin, PinValue.High);
                        IsBuzzerOn = true;
                    }
                    else
                    {
                        Log("IoTService:蜂鸣器", "关闭");
                        Log("IoTService:蜂鸣器", "Message from Justice Knight:滴滴,滴滴");
                        gpioController.Write(gpioPin, PinValue.Low);
                        IsBuzzerOn = false;
                    }
                }
            });
        }

        public static Task LightOnOrOffLED(bool isLedLight, int gpioPin = 27)
        {
            return Task.Run(() =>
            {
                Log("IoTService:LED", "初始化...");
                using (GpioController controller = new GpioController())
                {
                    controller.OpenPin(gpioPin, PinMode.Output);
                    if (isLedLight)
                    {
                        Log("IoTService:LED", "LED开");
                        controller.Write(gpioPin, PinValue.High);
                    }
                    else
                    {
                        Log("IoTService:LED", "LED关");
                        controller.Write(gpioPin, PinValue.Low);
                    }
                }
            });
        }

        private static void Log(string sender, string message)
        {
            Console.WriteLine($"\n{DateTime.Now} > [{sender}]{message}");
        }
    }
}
