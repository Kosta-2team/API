// Models/entriesData.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace parking.Models
{
    public class entriesData
    {
        public ObjectId Id { get; set; }  // MongoDB에서 자동 생성되는 _id
        public int column { get; set; }
        public string numPlate { get; set; }
        public string inTime { get; set; }
        public string? outTime { get; set; }
        public int? hoursParked { get; set; }
        public int ratePerHour { get; set; }
        public int? totalCost { get; set; }
        public string? etc { get; set; }
    }
}
