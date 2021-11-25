using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LiveHome.IoT;
using LiveHome.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LiveHome.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EnvironmentInfoController : ControllerBase
    {
        public EnvironmentInfoController()
        {

        }

        /// <summary>
        /// 获取当前环境信息
        /// </summary>
        /// <returns>当前环境信息</returns>
        [HttpGet]
        public ActionResult<EnvironmentInfo> GetEnvironmentInfo()
        {
            return StatusCode(410);
            //            EnvironmentInfo environmentInfo = new();
            //            try
            //            {
            //                (double, double) values = await IoTService.GetEnvironmentInfo();
            //                if (double.IsNaN(values.Item1) && double.IsNaN(values.Item2))
            //                {
            //                    return StatusCode(503);
            //                }
            //                environmentInfo.Temperature = values.Item1;
            //                environmentInfo.RelativeHumidity = values.Item2;
            //            }
            //            catch
            //            {
            //#if DEBUG
            //                return new EnvironmentInfo() { Temperature = 11451.4, RelativeHumidity = 81.0 };
            //#endif
            //                return StatusCode(503);
            //            }
            //            return environmentInfo;
        }
    }
}
