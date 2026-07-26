using Microsoft.AspNetCore.Mvc;
using PartnerService.Core.Abstractions.Repositories;
using PartnerService.Core.Domain;
using PartnerService.WebHost.Models;
using System.Security.Cryptography.Xml;

namespace PartnerService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PartnersController : ControllerBase
    {
        private readonly IRepository<Partner> _partnersRepository;

        public PartnersController(IRepository<Partner> partnersRepository)
        {
            _partnersRepository = partnersRepository;
        }

        /// <summary>
        /// Получить список партнеров
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<PartnerResponse>>> GetPartnersAsync()
        {
            var partners = await _partnersRepository.GetAllAsync();

            var response = partners.Select(x => new PartnerResponse()
            {
                Id = x.Id,
                Name = x.Name
            });

            return Ok(response);
        }

    }
}
