using AirVinyl.API.DbContexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirVinyl.Controllers
{
    public class PeopleController : ODataController
    {
        private readonly AirVinylDbContext _airVinylDbContext;

        public PeopleController(AirVinylDbContext airVinylDbContext)
        {
            _airVinylDbContext = airVinylDbContext ??
                throw new ArgumentNullException(nameof(airVinylDbContext));
        }

        public async Task<IActionResult> Get()
        {
            return Ok(await _airVinylDbContext.People.ToListAsync());
        }
    }
}
