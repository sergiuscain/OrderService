using Microsoft.AspNetCore.Mvc;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public WeatherForecastController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public string Get()
        {
            string url = _configuration["InventoryService:BaseUrl"];
            return url;
        }
    }
}
