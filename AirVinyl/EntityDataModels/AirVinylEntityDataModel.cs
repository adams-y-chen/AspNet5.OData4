using AirVinyl.Entities;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirVinyl.EntityDataModels
{
    public class AirVinylEntityDataModel
    {
        public IEdmModel GetEntityDataModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.Namespace = "AirVinyl";
            builder.ContainerName = "AirVinylContainer";

            builder.EntitySet<Person>("People");
            //builder.EntitySet<VinylRecord>("VinylRecords");
            builder.EntitySet<RecordStore>("RecordStores");

            // customize function on single RecordStore entity
            var isHighRatedFunction = builder.EntityType<RecordStore>()
                .Function("IsHighRated");
            isHighRatedFunction.Returns<bool>();
            isHighRatedFunction.Parameter<int>("minimumRating");
            isHighRatedFunction.Namespace = "AirVinyl.Functions";

            // customized function on a collection of RecordStore entities
            var areRatedByFunction = builder.EntityType<RecordStore>().Collection
                .Function("AreRatedBy");
            areRatedByFunction.ReturnsCollectionFromEntitySet<RecordStore>("RecordStores");
            areRatedByFunction.CollectionParameter<int>("personIds");
            areRatedByFunction.Namespace = "AirVinyl.Functions";

            return builder.GetEdmModel();
        }
    }
}
