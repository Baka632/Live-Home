using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.CharacterLcd;
using Iot.Device.DHTxx;
using Iot.Device.Media;
using Iot.Device.Pcx857x;
using Iot.Device.Ssd13xx;
using Iot.Device.Ssd13xx.Commands;
using Iot.Device.Ssd13xx.Commands.Ssd1306Commands;
using LiveHome.IoT.Devices;
using QRCoder;
using UnitsNet;
using Unosquare.RaspberryIO;
using Unosquare.RaspberryIO.Camera;

namespace LiveHome.IoT
{
    public static class IoTService
    {
        public static event Action<(double, double)> EnvironmentInfoChanged;
        public static event Action<bool> CombustibleGasDetected;
        private static (double, double) lastEnvInfo;
        private static bool lastGasInfo;

        public static (double, double) LastSuccessEnvInfo => lastEnvInfo;
        public static bool IsBuzzerOn { get; private set; }

        static IoTService()
        {
            Log("IoTService:类型对象构造器", "\n时代。时代在卡西米尔浓缩为了一个词，我们叫它卡瓦莱利亚基，我们也叫它大骑士领。\n新一届卡西米尔骑士特别锦标赛即将打响，我们用荣耀的模具，浇灌出了一场又一场闹剧。\n城市流光溢彩，霓虹灯征服了天空。金币叮叮作响，闪闪发亮，让人看不清那些小人物的面目。\n黎明还未升起。\n但长夜将尽。");
        }

        public static async Task RecognizeQRCodeAsync()
        {
            Log("IoTService:QR Code Recognizer", "初始化...");
            byte[] image = await CameraCapture();
        }

        /// <summary>
        /// 获取当前的环境信息
        /// </summary>
        /// <returns>一个元组,第一项为温度(摄氏度),第二项为湿度(相对湿度,以百分数表示)</returns>
        public static Task<(double, double)> GetEnvironmentInfo(int gpioPin = 4)
        {
            Log("IoTService:环境信息", "初始化...");
            return Task.Run(async () =>
            {
                (double, double) val = await GetEnvInfoAsync();
                if (lastEnvInfo != val && !double.IsNaN(val.Item1) && !double.IsNaN(val.Item2))
                {
                    EnvironmentInfoChanged?.Invoke(val);
                    lastEnvInfo = val;
                }
                return val;
            });

            async Task<(double, double)> GetEnvInfoAsync()
            {
                using (Dht11 dht11 = new Dht11(gpioPin))
                {
                    Temperature temperature = dht11.Temperature;
                    RelativeHumidity humidity = dht11.Humidity;
                    if (dht11.IsLastReadSuccessful && temperature.DegreesCelsius != 0d && humidity.Percent != 0)
                    {
                        //读取成功,返回值
                        double temp = temperature.DegreesCelsius;
                        double rh = humidity.Percent;
                        Log("IoTService:环境信息", $"读取成功\n温度为:{temp}℃\n湿度为:{rh}%");
                        await WriteEnviromentInfoToLCD();
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
                            if (dht11.IsLastReadSuccessful && temperature.DegreesCelsius != 0d && humidity.Percent != 0)
                            {
                                //读取成功就返回值
                                double temp = temperature1.DegreesCelsius;
                                double rh = humidity1.Percent;
                                Log("IoTService:环境信息", $"读取成功\n温度为:{temp:0.#}℃\n湿度为:{rh:0.#}%");
                                await WriteEnviromentInfoToLCD();
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
                        if (makeNoise)
                        {
                            await OnOrOffBuzzer(true);
                        }
                        await LightOnOrOffLED(true, 16);
                    }
                    else
                    {
                        await OnOrOffBuzzer(false);
                        await LightOnOrOffLED(false, 16);
                    }
                    lastGasInfo = state;
                    await WriteEnviromentInfoToLCD();
                    Log("IoTService:可燃气体检测", $"读取完成,结果为{state}。");
                    return state;
                }
            });
        }

        public static Task WriteEnviromentInfoToLCD(int pin = 1, int deviceAdress = 0x3C)
        {
            return Task.Run(() =>
            {
                Log("IoTService:LCD", "初始化...");

                string message;
                if (lastGasInfo)
                {
                    message = $"Temp:{LastSuccessEnvInfo.Item1}oC\nHumidity:{LastSuccessEnvInfo.Item2}%\nGas Detected!";
                }
                else
                {
                    message = $"Temp:{LastSuccessEnvInfo.Item1}oC\nHumidity:{LastSuccessEnvInfo.Item2}%\nNo gas";
                }
                using (I2cDevice i2CDevice = I2cDevice.Create(new I2cConnectionSettings(pin, deviceAdress)))
                {
                    using (Ssd1306 device = new Ssd1306(i2CDevice))
                    {
                        InitializeSsd1306(device);
                        ClearScreenSsd1306(device);
                        foreach (char character in message)
                        {
                            if (character.Equals('\n'))
                            {
                                device.SendCommand(new ContinuousVerticalAndHorizontalScrollSetup(ContinuousVerticalAndHorizontalScrollSetup.VerticalHorizontalScrollType.Left, PageAddress.Page0, FrameFrequencyType.Frames128, PageAddress.Page7, 1));
                                continue;
                            }
                            device.SendData(BasicFont.GetCharacterBytes(character));
                        }
                    }
                }
            });

            // Display size 128x32.
            void InitializeSsd1306(Ssd1306 device)
            {
                device.SendCommand(new SetDisplayOff());
                device.SendCommand(new SetDisplayClockDivideRatioOscillatorFrequency(0x00, 0x08));
                device.SendCommand(new SetMultiplexRatio(0x1F));
                device.SendCommand(new SetDisplayOffset(0x00));
                device.SendCommand(new SetDisplayStartLine(0x00));
                device.SendCommand(new SetChargePump(true));
                device.SendCommand(
                    new SetMemoryAddressingMode(SetMemoryAddressingMode.AddressingMode
                        .Horizontal));
                device.SendCommand(new SetSegmentReMap(true));
                device.SendCommand(new SetComOutputScanDirection(false));
                device.SendCommand(new SetComPinsHardwareConfiguration(false, false));
                device.SendCommand(new SetContrastControlForBank0(0x8F));
                device.SendCommand(new SetPreChargePeriod(0x01, 0x0F));
                device.SendCommand(
                    new SetVcomhDeselectLevel(SetVcomhDeselectLevel.DeselectLevel.Vcc1_00));
                device.SendCommand(new EntireDisplayOn(false));
                device.SendCommand(new SetNormalDisplay());
                device.SendCommand(new SetDisplayOn());
                device.SendCommand(new SetColumnAddress());
                device.SendCommand(new SetPageAddress(PageAddress.Page1,
                    PageAddress.Page3));
            }

            void ClearScreenSsd1306(Ssd1306 device)
            {
                device.SendCommand(new SetColumnAddress());
                device.SendCommand(new SetPageAddress(PageAddress.Page0,
                    PageAddress.Page3));

                for (int cnt = 0; cnt < 32; cnt++)
                {
                    byte[] data = new byte[16];
                    device.SendData(data);
                }
            }
        }

        public static Task<byte[]> CameraCapture()
        {
            return Task.Run(async () =>
            {
                CameraStillSettings settings = new CameraStillSettings();
                byte[] image = await Pi.Camera.CaptureImageAsync(settings);
                return image;
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

        public static Task LightOnOrOffLED(bool isLedLight, int gpioPin)
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
