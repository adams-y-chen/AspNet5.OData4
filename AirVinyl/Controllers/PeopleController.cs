using AirVinyl.API.DbContexts;
using AirVinyl.API.Helpers;
using AirVinyl.Entities;
using Microsoft.AspNetCore.Http.Extensions;
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

        // Convention based routing which mapped to the URL in the form of: People(1)
        public async Task<IActionResult> Get(int key)
        {
            var person = await _airVinylDbContext.People.FirstOrDefaultAsync(p => p.PersonId == key);

            if (person == null)
            {
                return NotFound();
            }

            return Ok(person);
        }

        // Get a property of a person`
        // Attribute based routing is needed to handle this type of URL
        [HttpGet("odata/People({key})/Email")]
        [HttpGet("odata/People({key})/FirstName")]
        [HttpGet("odata/People({key})/LastName")]
        [HttpGet("odata/People({key})/DataOfBirth")]
        [HttpGet("odata/People({key})/Gender")]
        public async Task<IActionResult> GetPersonProperty(int key)
        {
            var person = await _airVinylDbContext.People.FirstOrDefaultAsync(p => p.PersonId == key);

            if (person == null)
            {
                return NotFound();
            }

            var propertyName = new Uri(HttpContext.Request.GetEncodedUrl()).Segments.Last();

            if (!person.HasProperty(propertyName))
            {
                return NotFound();
            }

            var propertyValue = person.GetValue(propertyName);
            if (propertyValue == null)
            {
                return NoContent();
            }

            return Ok(propertyValue);
        }

        // Get a property raw value of a person`
        // Attribute based routing is needed to handle this type of URL
        [HttpGet("odata/People({key})/Email/$value")]
        [HttpGet("odata/People({key})/FirstName/$value")]
        [HttpGet("odata/People({key})/LastName/$value")]
        [HttpGet("odata/People({key})/DataOfBirth/$value")]
        [HttpGet("odata/People({key})/Gender/$value")]
        public async Task<IActionResult> GetPersonPropertyRawValue(int key)
        {
            var person = await _airVinylDbContext.People.FirstOrDefaultAsync(p => p.PersonId == key);

            if (person == null)
            {
                return NotFound();
            }

            var url = HttpContext.Request.GetEncodedUrl();
            var propertyName = new Uri(url).Segments[^2].TrimEnd('/');

            if (!person.HasProperty(propertyName))
            {
                return NotFound();
            }

            var propertyValue = person.GetValue(propertyName);
            if (propertyValue == null)
            {
                return NoContent();
            }

            return Ok(propertyValue.ToString());
        }

        // Get the VinylRecords collection property.
        [HttpGet("odata/People({key})/VinylRecords")]
        public async Task<IActionResult> GetPersonCollectionProperty(int key)
        {
            var propertyName = new Uri(HttpContext.Request.GetEncodedUrl())
                .Segments.Last();

            var person = await _airVinylDbContext.People
                .Include(propertyName)
                .FirstOrDefaultAsync(p => p.PersonId == key);

            if (person == null)
            {
                return NotFound();
            }

            if (!person.HasProperty(propertyName))
            {
                return NotFound();
            }

            var propertyValue = person.GetValue(propertyName);

            return Ok(propertyValue);
        }

        // Support creating People with VinylRecourds in the body.
        [HttpPost("odata/People")]
        public async Task<IActionResult> CreatePerson([FromBody] Person person)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _airVinylDbContext.People.Add(person);
            await _airVinylDbContext.SaveChangesAsync();

            return Created(person); 
        }
    }
}
