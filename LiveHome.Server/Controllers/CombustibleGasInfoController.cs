using System.Threading.Tasks;
using LiveHome.IoT;
using Microsoft.AspNetCore.Mvc;

namespace LiveHome.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CombustibleGasInfoController : ControllerBase
    {
        public CombustibleGasInfoController()
        {

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
#if DEBUG
                return true;
#endif
                return StatusCode(503);
            }
        }
    }
}
