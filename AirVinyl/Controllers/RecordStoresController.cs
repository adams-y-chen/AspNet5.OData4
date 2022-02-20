using AirVinyl.API.DbContexts;
using AirVinyl.API.Helpers;
using AirVinyl.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
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
    [Route("odata")]
    public class RecordStoresController : ODataController
    {
        private readonly AirVinylDbContext _airVinylDbContext;

        public RecordStoresController(AirVinylDbContext airVinylDbContext)
        {
            _airVinylDbContext = airVinylDbContext ??
                throw new ArgumentNullException(nameof(airVinylDbContext));
        }

        [EnableQuery]
        [HttpGet("RecordStores")]
        public IActionResult GetRecordStores()
        {
            return Ok(_airVinylDbContext.RecordStores);
        }

        [EnableQuery]
        [Route("RecordStores({key})")]
        public IActionResult GetRecordStore(int key)
        {
            var recordStores = _airVinylDbContext.RecordStores
                .Where(r => r.RecordStoreId == key);

            if (!recordStores.Any())
            {
                return NotFound();
            }

            return Ok(SingleResult.Create(recordStores));
        }

        [EnableQuery]
        [Route("RecordStores({key})/Tags")]
        public IActionResult GetRecordStoreTagsProperty(int key)
        {
            var recordStore = _airVinylDbContext.RecordStores
                .FirstOrDefault(r => r.RecordStoreId == key);

            if (recordStore == null)
            {
                return NotFound();
            }

            var propertyName = new Uri(HttpContext.Request.GetEncodedUrl())
                .Segments.Last();
            var propertyValue = recordStore.GetValue(propertyName);

            return Ok(propertyValue);
        }

        [HttpGet("RecordStores({key})/AirVinyl.Functions.IsHighRated(minmumRating={minimumRating})")]
        public async Task<bool> IsHighRated(int key, int minimumRating)
        {
            var recordStore = await _airVinylDbContext.RecordStores
                .FirstOrDefaultAsync(p => p.RecordStoreId == key
                    && p.Ratings.Any()
                    && (p.Ratings.Sum(r=>r.Value)/p.Ratings.Count() > minimumRating));

            return (recordStore != null);

        }

        [HttpGet("RecordStores/AirVinyl.Functions.AreRatedBy(personIds={people})")]
        public async Task<IActionResult> AreRatedBy([FromODataUri] IEnumerable<int> people)
        {
            var recordStores = await _airVinylDbContext.RecordStores
                .Where(p => p.Ratings.Any(r => people.Contains(r.RatedBy.PersonId)))
                .ToListAsync();

            return Ok(recordStores);
        }

        [HttpGet("GetHighRatedRecordStores(minimumRating={minimumRating})")]
        public async Task<IActionResult> GetHighRatedRecordStores(int minimumRating)
        {
            var recordStores = await _airVinylDbContext.RecordStores
                .Where(p => p.Ratings.Any()
                    && (p.Ratings.Sum(r => r.Value) / p.Ratings.Count() > minimumRating))
                .Include("Ratings")
                .ToListAsync();

            return Ok(recordStores);
        }

        [HttpPost("RecordStores({id})/AirVinyl.Actions.Rate")]
        public async Task<IActionResult> Rate(int id, ODataActionParameters parameters)
        {
            // get the RecordStore
            var recordStore = await _airVinylDbContext.RecordStores
              .FirstOrDefaultAsync(p => p.RecordStoreId == id);

            if (recordStore == null)
            {
                return NotFound();
            }

            if (!parameters.TryGetValue("rating", out object outputFromDictionary))
            {
                return BadRequest();
            }

            if (!int.TryParse(outputFromDictionary.ToString(), out int rating))
            {
                return BadRequest();
            }

            if (!parameters.TryGetValue("personId", out outputFromDictionary))
            {
                return BadRequest();
            }

            if (!int.TryParse(outputFromDictionary.ToString(), out int personId))
            {
                return BadRequest();
            }

            // the person must exist
            var person = await _airVinylDbContext.People
                .FirstOrDefaultAsync(p => p.PersonId == personId);

            if (person == null)
            {
                return BadRequest();
            }

            // everything checks out, add the rating
            recordStore.Ratings.Add(new Rating() { RatedBy = person, Value = rating });

            // save changes 
            if (await _airVinylDbContext.SaveChangesAsync() > 0)
            {
                // return true
                return Ok(true);
            }
            else
            {

                // Something went wrong - we expect our 
                // action to return false in that case.  
                // The request is still successful, false
                // is a valid response
                return Ok(false);
            }
        }

        [HttpPost("RecordStores/AirVinyl.Actions.RemoveRatings")]
        public async Task<IActionResult> RemoveRatings(ODataActionParameters parameters)
        {
            // from the param dictionary, get the personid 
            if (!parameters.TryGetValue("personId", out object outputFromDictionary))
            {
                return BadRequest();
            }

            if (!int.TryParse(outputFromDictionary.ToString(), out int personId))
            {
                return BadRequest();
            }

            // get the RecordStores that were rated by the person with personId
            var recordStoresRatedByCurrentPerson = await _airVinylDbContext.RecordStores
                .Include("Ratings").Include("Ratings.RatedBy")
                .Where(p => p.Ratings.Any(r => r.RatedBy.PersonId == personId)).ToListAsync();

            // remove those ratings
            foreach (var store in recordStoresRatedByCurrentPerson)
            {
                // get the ratings by the current person
                var ratingsByCurrentPerson = store.Ratings
                    .Where(r => r.RatedBy.PersonId == personId).ToList();

                for (int i = 0; i < ratingsByCurrentPerson.Count(); i++)
                {
                    store.Ratings.Remove(ratingsByCurrentPerson[i]);
                }
            }

            // save changes 
            if (await _airVinylDbContext.SaveChangesAsync() > 0)
            {
                // return true
                return Ok(true);
            }
            else
            {
                // Something went wrong - we expect our 
                // action to return false in that case.  
                // The request is still successful, false
                // is a valid response
                return Ok(false);
            }

        }

        [HttpPost("RemoveRecordStoreRatings")]
        public async Task<IActionResult> RemoveRecordStoreRatings(ODataActionParameters parameters)
        {
            // from the param dictionary, get the personid 
            if (!parameters.TryGetValue("personId", out object outputFromDictionary))
            {
                return BadRequest();
            }

            if (!int.TryParse(outputFromDictionary.ToString(), out int personId))
            {
                return BadRequest();
            }

            // get the RecordStores that were rated by the person with personId
            var recordStoresRatedByCurrentPerson = await _airVinylDbContext.RecordStores
                .Include("Ratings").Include("Ratings.RatedBy")
                .Where(p => p.Ratings.Any(r => r.RatedBy.PersonId == personId)).ToListAsync();

            // remove those ratings
            foreach (var store in recordStoresRatedByCurrentPerson)
            {
                // get the ratings by the current person
                var ratingsByCurrentPerson = store.Ratings
                    .Where(r => r.RatedBy.PersonId == personId).ToList();

                for (int i = 0; i < ratingsByCurrentPerson.Count(); i++)
                {
                    store.Ratings.Remove(ratingsByCurrentPerson[i]);
                }
            }

            // save changes 
            if (await _airVinylDbContext.SaveChangesAsync() > 0)
            {
                return NoContent();
            }
            else
            {
                // something went wrong
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [EnableQuery]
        [HttpGet("RecordStores/AirVinyl.SpecializedRecordStore")]
        public IActionResult GetSpecializedRecordStores()
        {
            var specializedStores = _airVinylDbContext.RecordStores
                .Where(r => r is SpecializedRecordStore).ToList();
            return Ok(specializedStores.Select(s => s as SpecializedRecordStore));
        }

        [EnableQuery]
        [HttpGet("RecordStores({id})/AirVinyl.SpecializedRecordStore")]
        public IActionResult GetSpecializedRecordStore(int id)
        {
            var specializedStore = _airVinylDbContext.RecordStores
                .Where(r => r.RecordStoreId == id && r is SpecializedRecordStore);

            if (!specializedStore.Any())
            {
                return NotFound();
            }

            return Ok(SingleResult.Create(specializedStore
                .Select(s => s as SpecializedRecordStore)));
        }

    }
}
