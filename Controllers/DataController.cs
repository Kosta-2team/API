using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading;
using parking.Hubs;
using parking.Models;
using parking.Services;

namespace parking.Controllers
{
    [ApiController] //해당 클래스가 Web API 컨트롤러임을 지정
    [Route("api/[controller]")] //컨트롤러의 기본 라우트를 api/Data로 설정
    public class DataController : ControllerBase
    //ControllerBase: View 렌더링 기능이 없는 컨트롤러를 위한 기본 클래스
    {
        //MongoDB와 상호작용하는 서비스로, DB 작업 수행
        //생성자 주입을 사용하여 의존성 주입(MongoDbService.cs 받아옴)
        private readonly MongoDbService _service;
        private readonly IHubContext<SignalRHub> _hubContext;

        public DataController(MongoDbService service, IHubContext<SignalRHub> hubContext)
        {
            _service = service;
            _hubContext = hubContext;
        }

        // GET: 모든 데이터 가져오기 (Database1 - entries)
        //HTTP GET 요청을 처리(API 경로가 base_url/database1인 요청을 처리)
        [HttpGet("database1")]  
        //비동기로 동작하며, 호출자가 HTTP 응답을 받을 수 있도록 IActionResult를 반환
        //반환 타입이 IActionResult인 이유는
        //다양한 HTTP 응답 상태 코드와 데이터를 유연하게 반환할 수 있기 때문
        public async Task<IActionResult> GetDatabase1Data()
        {
            //MongoDB의 데이터를 읽기 위해 entries라는 컬렌션 반환
            var collection = _service.GetentriesCollection("entries");

            // 데이터를 가져와 변환
            var data = await collection.Find(_ => true) //MongoDB 쿼리로, 필터 조건이 없는 모든 문서를 가져옴
                                                        //_ => true는 람다 표현식으로, "모든 데이터"를 반환
                                        .ToListAsync(); //비동기적으로 데이터를 가져와 리스트로 변환

            // ObjectId를 문자열로 변환
            var response = data.Select(entry => new //Select 메서드를 사용하여 컬렉션 데이터를 익명 객체의 시퀀스로 매핑
            {
                Id = entry.Id.ToString(),        // ObjectId -> 문자열 변환
                Column = entry.column,
                NumPlate = entry.numPlate,
                InTime = entry.inTime,
                OutTime = entry.outTime,
                HoursParked = entry.hoursParked,
                RatePerHour = entry.ratePerHour,
                TotalCost = entry.totalCost,
                Etc = entry.etc
            });

            return Ok(response);
        }

        // GET: ID로 특정 데이터 가져오기 (Database1 - entries)
        [HttpGet("database1/{id:length(24)}")]
        public async Task<IActionResult> GetDatabase1DataById(string id)
        {
            var collection = _service.GetentriesCollection("entries");
            var data = await collection.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
            if (data == null) return NotFound();
            return Ok(data);
        }

        // POST: 데이터 추가 (Database1 - entries)
        [HttpPost("database1")]
        public async Task<IActionResult> AddToDatabase1([FromBody] entriesData data)
        {
            var collection = _service.GetentriesCollection("entries");
            await collection.InsertOneAsync(data);
            return CreatedAtAction(nameof(GetDatabase1DataById), new { id = data.Id.ToString() }, data);
        }

        // PUT: 데이터 업데이트 (Database1 - entries)
        [HttpPut("database1/{id:length(24)}")]
        public async Task<IActionResult> UpdateDatabase1(string id, [FromBody] entriesData updatedData)
        {
            var collection = _service.GetentriesCollection("entries");
            var filter = Builders<entriesData>.Filter.Eq(x => x.Id, ObjectId.Parse(id));
            var update = Builders<entriesData>.Update
                .Set(x => x.column, updatedData.column)
                .Set(x => x.numPlate, updatedData.numPlate)
                .Set(x => x.inTime, updatedData.inTime)
                .Set(x => x.outTime, updatedData.outTime)
                .Set(x => x.hoursParked, updatedData.hoursParked)
                .Set(x => x.ratePerHour, updatedData.ratePerHour)
                .Set(x => x.totalCost, updatedData.totalCost)
                .Set(x => x.etc, updatedData.etc);

            var result = await collection.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0) return NotFound();

            // SignalR를 통해 업데이트된 데이터를 클라이언트에 브로드캐스트
            await _hubContext.Clients.All.SendAsync("ReceiveChange", updatedData);

            return NoContent();
        }

        // DELETE: 데이터 삭제 (Database1 - entries)
        [HttpDelete("database1/{id:length(24)}")]
        public async Task<IActionResult> DeleteFromDatabase1(string id)
        {
            var collection = _service.GetentriesCollection("entries");
            var result = await collection.DeleteOneAsync(x => x.Id == ObjectId.Parse(id));

            if (result.DeletedCount == 0) return NotFound();

            return NoContent();
        }

        // 동일한 방식으로 Database2 엔드포인트 추가

        // GET: 모든 데이터 가져오기 (Database2 - members)
        [HttpGet("database2")]
        public async Task<IActionResult> GetDatabase2Data()
        {
            var collection = _service.GetmembersCollection("members");
            var data = await collection.Find(_ => true).ToListAsync();
            return Ok(data);
        }

        // GET: ID로 특정 데이터 가져오기 (Database2 - members)
        [HttpGet("database2/{id:length(24)}")]
        public async Task<IActionResult> GetDatabase2DataById(string id)
        {
            var collection = _service.GetmembersCollection("members");
            var data = await collection.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
            if (data == null) return NotFound();
            return Ok(data);
        }

        // POST: 데이터 추가 (Database2 - members)
        [HttpPost("database2")]
        public async Task<IActionResult> AddToDatabase2([FromBody] membersData data)
        {
            var collection = _service.GetmembersCollection("members");
            await collection.InsertOneAsync(data);
            return CreatedAtAction(nameof(GetDatabase2DataById), new { id = data.Id.ToString() }, data);
        }

        // PUT: 데이터 업데이트 (Database2 - members)
        [HttpPut("database2/{id:length(24)}")]
        public async Task<IActionResult> UpdateDatabase2(string id, [FromBody] membersData updatedData)
        {
            var collection = _service.GetmembersCollection("members");
            var filter = Builders<membersData>.Filter.Eq(x => x.Id, ObjectId.Parse(id));
            var update = Builders<membersData>.Update
                .Set(x => x.userId, updatedData.userId)
                .Set(x => x.userPw, updatedData.userPw)
                .Set(x => x.userName, updatedData.userName)
                .Set(x => x.userBirth, updatedData.userBirth)
                .Set(x => x.admin, updatedData.admin);

            var result = await collection.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0) return NotFound();
            return NoContent();
        }

        // DELETE: 데이터 삭제 (Database2 - members)
        [HttpDelete("database2/{id:length(24)}")]
        public async Task<IActionResult> DeleteFromDatabase2(string id)
        {
            var collection = _service.GetmembersCollection("members");
            var result = await collection.DeleteOneAsync(x => x.Id == ObjectId.Parse(id));

            if (result.DeletedCount == 0) return NotFound();
            return NoContent();
        }
    }
}
