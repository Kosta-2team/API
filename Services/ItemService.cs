using MongoDB.Driver;
using Microsoft.Extensions.Options;
using parking.Models;

namespace parking.Services
{
    public class ItemService
    {
        private readonly IMongoDatabase _car;
        private readonly IMongoDatabase _member;

        //public ItemService(IOptions<MongoSettings> mongoSettings)
        //{
        //    // MongoDB Atlas 연결
        //    var client1 = new MongoClient(mongoSettings.Value.car);
        //    _car = client1.GetDatabase(new MongoUrl(mongoSettings.Value.CAR).DatabaseName);

        //    var client2 = new MongoClient(mongoSettings.Value.MEMBER);
        //    _member = client2.GetDatabase(new MongoUrl(mongoSettings.Value.MEMBER).DatabaseName);
        //}

        public IMongoDatabase GetDatabase1() => _car;
        public IMongoDatabase GetDatabase2() => _member;

    }
}
