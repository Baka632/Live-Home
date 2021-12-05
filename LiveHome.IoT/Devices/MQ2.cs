using System;
using System.Device.Gpio;

namespace LiveHome.IoT.Devices
{
    /// <summary>
    /// MQ-2烟雾气敏传感器
    /// </summary>
    public class MQ2 : IDisposable
    {
        private readonly GpioController _controller;
        private readonly int _outPin;
        public event Action CombustibleGasDetected;

        /// <summary>
        /// 构造<see cref="MQ2"/>类的新实例
        /// </summary>
        /// <param name="outPin">引脚口</param>
        /// <param name="pinNumberingScheme">引脚编号方案</param>
        public MQ2(int outPin, PinNumberingScheme pinNumberingScheme = PinNumberingScheme.Logical)
        {
            _outPin = outPin;

            _controller = new GpioController(pinNumberingScheme);
            _controller.OpenPin(outPin, PinMode.InputPullDown);
            _controller.RegisterCallbackForPinValueChangedEvent(outPin, PinEventTypes.Falling, (obj, sender) => RaiseEvent());
        }

        ~MQ2()
        {
            _controller.UnregisterCallbackForPinValueChangedEvent(_outPin, (obj, sender) => RaiseEvent());
        }

        private void RaiseEvent()
        {
            CombustibleGasDetected?.Invoke();
        }

        /// <summary>
        /// 指示是否侦测到可燃气体的属性
        /// </summary>
        public bool IsCombustibleGasDetected
        {
            get
            {
                PinValue value = _controller.Read(_outPin);
                bool result = value != PinValue.High;
                return result;
            }
        }

        public void Dispose()
        {
            ((IDisposable)_controller).Dispose();
        }
    }
}
