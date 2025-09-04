using CMS.Contracts.Info;
using CMS.Controllers.Base;
using CMS.Data;
using CMS.Models.Info;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.Controllers.Info
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : TenantControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApiContext _db;

        public AddressController(IWebHostEnvironment env, ApiContext db)
        {
            _env = env;
            _db = db;
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<AddressDto>> Get()
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            // Global filter already applies TenantId
            var address = await _db.Address.AsNoTracking().SingleOrDefaultAsync();

            if (address is null)
            {
                var seed = new AddressEntity
                {
                    TenantId = TenantId!,           // <-- set TenantId
                    Street = string.Empty,
                    City = string.Empty,
                    State = string.Empty,
                    Zipcode = string.Empty,
                };
                _db.Address.Add(seed);
                await _db.SaveChangesAsync();
                return new AddressDto { Street = string.Empty, City = string.Empty, State = string.Empty, Zipcode = string.Empty };
            }

            return new AddressDto
            {
                Street = address.Street,
                City = address.City,
                State = address.State,
                Zipcode = address.Zipcode,
            };
        }

        [Authorize(Policy = "TenantMatch")]
        [HttpPatch]
        public async Task<ActionResult<AddressDto>> Patch([FromBody] AddressPatchDto patch)
        {
            if (TenantId is null) return BadRequest("Tenant not resolved.");

            var address = await _db.Address.SingleOrDefaultAsync();
            if (address is null)
            {
                address = new AddressEntity { TenantId = TenantId! };   // <-- set TenantId
                _db.Address.Add(address);
            }

            if (patch.Street is not null) address.Street = patch.Street.Trim();
            if (patch.City is not null) address.City = patch.City.Trim();
            if (patch.State is not null) address.State = patch.State.Trim();
            if (patch.Zipcode is not null) address.Zipcode = patch.Zipcode.Trim();

            await _db.SaveChangesAsync();

            return new AddressDto
            {
                Street = address.Street,
                City = address.City,
                State = address.State,
                Zipcode = address.Zipcode,
            };
        }
    }
}
