namespace parking.Models
{
    public class MongoSettings
    {
        public string ConnectionString { get; set; } // MongoDB 연결 문자열
        public string Database1Name { get; set; } // Database1 이름
        public string Database2Name { get; set; } // Database2 이름
    }
}