using AirVinyl.API.DbContexts;
using AirVinyl.API.Helpers;
using AirVinyl.Entities;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
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

        //[EnableQuery]
        //public async Task<IActionResult> Get()
        //{
        //    return Ok(await _airVinylDbContext.People.ToListAsync());
        //}
        //"LINQ defered execution that helps optimize database query."
        //The select query would only return specifed fields from database.
        [EnableQuery(PageSize = 4)]
        public IActionResult Get()
        {
            return Ok(_airVinylDbContext.People);
        }

        // Convention based routing which mapped to the URL in the form of: People(1)
        //[EnableQuery]
        //public async Task<IActionResult> Get(int key)
        //{
        //    var person = await _airVinylDbContext.People.FirstOrDefaultAsync(p => p.PersonId == key);

        //    if (person == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(person);
        //}
        [EnableQuery]
        public IActionResult Get(int key)
        {
            var people = _airVinylDbContext.People.Where(p => p.PersonId == key);

            if (!people.Any())
            {
                return NotFound();
            }

            return Ok(SingleResult.Create(people));
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

        //// Get the VinylRecords collection property.
        //[EnableQuery]
        //[HttpGet("odata/People({key})/VinylRecords")]
        //public IActionResult GetPersonCollectionProperty(int key)
        //{
        //    var propertyName = new Uri(HttpContext.Request.GetEncodedUrl())
        //        .Segments.Last();

        //    var person = _airVinylDbContext.People
        //        .Include(propertyName)
        //        .FirstOrDefault(p => p.PersonId == key);

        //    if (person == null)
        //    {
        //        return NotFound();
        //    }

        //    if (!person.HasProperty(propertyName))
        //    {
        //        return NotFound();
        //    }

        //    var propertyValue = person.GetValue(propertyName);

        //    return Ok(propertyValue);
        //}

        // Get the VinylRecords collection property.
        [EnableQuery]
        [HttpGet("odata/People({key})/VinylRecords")]
        public IActionResult GetPersonCollectionProperty(int key)
        {
            var person = _airVinylDbContext.People.FirstOrDefault(p => p.PersonId == key);

            if (person == null)
            {
                return NotFound();
            }

            return Ok(_airVinylDbContext.VinylRecords.Where(v => v.PersonId == key));
        }

        [EnableQuery]
        [HttpGet("odata/People({key})/VinylRecords({vinylRecordKey})")]
        public IActionResult GetVinylRecordForPerson(int key, int vinylRecordKey)
        {
            var person = _airVinylDbContext.People
                .FirstOrDefault(p => p.PersonId == key);

            if (person == null)
            {
                return NotFound();
            }

            var vinylRecords = _airVinylDbContext.VinylRecords
                .Where(v => v.VinylRecordId == vinylRecordKey);

            if (!vinylRecords.Any())
            {
                return NotFound();
            }

            return Ok(SingleResult.Create(vinylRecords));
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

        [HttpPut("odata/People({key})")]
        public async Task<IActionResult> UpdatePerson(int key, [FromBody] Person person)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentPerson = await _airVinylDbContext.People.FirstOrDefaultAsync(p => p.PersonId == key);

            if (currentPerson == null)
            {
                return NotFound();
            }

            // Note: code shall not assume that person.PersonId is the same as the key.
            // Set it explictly in case it is different.
            person.PersonId = currentPerson.PersonId;
            _airVinylDbContext.Entry(currentPerson).CurrentValues.SetValues(person);
            await _airVinylDbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("odata/People({key})")]
        public async Task<IActionResult> PartiallyUpdatePerson(int key, [FromBody] Delta<Person> patch)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentPerson = await _airVinylDbContext.People.FirstOrDefaultAsync(p => p.PersonId == key);

            if (currentPerson == null)
            {
                return NotFound();
            }

            patch.Patch(currentPerson);
            await _airVinylDbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("odata/People({key})")]
        public async Task<IActionResult> DeletePerson(int key)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentPerson = await _airVinylDbContext.People.FirstOrDefaultAsync(p => p.PersonId == key);

            if (currentPerson == null)
            {
                return NotFound();
            }

            _airVinylDbContext.People.Remove(currentPerson);
            await _airVinylDbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}
