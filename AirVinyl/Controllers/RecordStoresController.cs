using AirVinyl.API.DbContexts;
using AirVinyl.API.Helpers;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("RecordStore({key})/AirVinyl.Functions.IsHighRated(minmumRating={minimumRating})")]
        public async Task<bool> IsHighRated(int key, int minimumRating)
        {
            var recordStore = await _airVinylDbContext.RecordStores
                .FirstOrDefaultAsync(p => p.RecordStoreId == key
                && p.Ratings.Any()
                && (p.Ratings.Sum(r=>r.Value)/p.Ratings.Count() > minimumRating));

            return (recordStore != null);

        }
    }
}
