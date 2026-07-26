using Microsoft.AspNetCore.Mvc;

namespace PartnerService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PartnersController : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<DependencyInjection> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new DependencyInjection
            {
            })
            .ToArray();
        }
    }
}
