using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveHome.IoT;
using LiveHome.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LiveHome.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EnvironmentInfoController : ControllerBase
    {
        private readonly ILogger<EnvironmentInfoController> _logger;

        public EnvironmentInfoController(ILogger<EnvironmentInfoController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取当前环境信息
        /// </summary>
        /// <returns>当前环境信息</returns>
        [HttpGet]
        public async Task<ActionResult<EnvironmentInfo>> GetEnvironmentInfo()
        {
            EnvironmentInfo environmentInfo = new();
            try
            {
                (double, double, double) values = await IoTService.GetEnvironmentInfo();
                if (double.IsNaN(values.Item1) && double.IsNaN(values.Item2) && double.IsNaN(values.Item3))
                {
                    return StatusCode(503);
                }
                environmentInfo.Temperature = values.Item1;
                environmentInfo.RelativeHumidity = values.Item2;
                environmentInfo.HeatIndex = values.Item3;
            }
            catch
            {
                return StatusCode(503);
            }
            return environmentInfo;
        }

        /// <summary>
        /// 侦测设备附近是否有可燃气体
        /// </summary>
        /// <returns>如果当前环境发现可燃气体,则返回true,否则返回false</returns>
        [HttpGet]
        public async Task<ActionResult<bool>> DetectCombustibleGas()
        {
            try
            {
                bool value = await IoTService.DetectCombustibleGas();
                return value;
            }
            catch
            {
                return StatusCode(503);
            }
        }
    }
}
