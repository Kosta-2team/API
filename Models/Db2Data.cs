// Models/membersData.cs
using MongoDB.Bson;

namespace parking.Models
{
    public class membersData
    {
        public ObjectId Id { get; set; }  // MongoDB에서 자동 생성되는 _id
        public string userId { get; set; }
        public string userPw { get; set; }
        public string userName { get; set; }
        public string userBirth { get; set; }
        public string admin { get; set; }
    }
}
