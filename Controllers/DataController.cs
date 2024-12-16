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
        public async Task<IActionResult> GetDatabase1Data([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            // 페이지와 제한값 유효성 검사
            if (page <= 0 || limit <= 0)
                return BadRequest("Page and limit must be greater than 0.");

            // entries 컬렉션 가져오기
            var collection = _service.GetentriesCollection("entries");

            // 전체 데이터 개수 계산
            var totalCount = await collection.CountDocumentsAsync(_ => true);

            // 페이징 데이터 가져오기
            var data = await collection.Find(_ => true)
                                        .Skip((page - 1) * limit)  // 페이지 시작점
                                        .Limit(limit)             // 페이지에 표시할 데이터 개수
                                        .ToListAsync();

            // ObjectId를 문자열로 변환하여 반환 데이터 구성
            var response = new
            {
                TotalCount = totalCount,                     // 전체 데이터 개수
                TotalPages = (int)Math.Ceiling((double)totalCount / limit), // 총 페이지 수
                CurrentPage = page,                         // 현재 페이지 번호
                PageSize = limit,                           // 페이지당 데이터 개수
                Data = data.Select(entry => new
                {
                    Id = entry.Id.ToString(),              // ObjectId -> 문자열 변환
                    Column = entry.column,
                    NumPlate = entry.numPlate,
                    InTime = entry.inTime,
                    OutTime = entry.outTime,
                    MinsParked = entry.minsParked,
                    Rate = entry.rate,
                    TotalCost = entry.totalCost,
                    Etc = entry.etc
                })
            };

            return Ok(response); // 200 OK와 함께 결과 반환
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

        // 최근 생성된 데이터 5개 가져오기 (Database1 - entries)
        [HttpGet("database1/recent")]
        public async Task<IActionResult> GetRecentEntries()
        {
            var collection = _service.GetentriesCollection("entries");

            // 최근 생성된 데이터 5개 가져오기
            var recentData = await collection.Find(_ => true)
                                             .SortByDescending(x => x.Id) // ObjectId는 생성 시간 순으로 정렬 가능
                                             .Limit(5)
                                             .ToListAsync();

            // 반환 데이터 형식 설정
            var response = recentData.Select(entry => new
            {
                Id = entry.Id.ToString(),
                NumPlate = entry.numPlate,
                InTime = entry.inTime,
                OutTime = entry.outTime,
                MinsParked = entry.minsParked,
                Rate = entry.rate,
                TotalCost = entry.totalCost
            });

            return Ok(response);
        }

        // POST 요청 시 SignalR을 통해 새 데이터 전달
        [HttpPost("database1")]
        public async Task<IActionResult> AddToDatabase1([FromBody] entriesData data)
        {
            var collection = _service.GetentriesCollection("entries");
            await collection.InsertOneAsync(data); // 데이터 삽입

            // 삽입된 데이터의 반환 형식 설정 (Id를 문자열로 변환)
            var responseData = new
            {
                Id = data.Id.ToString(),
                NumPlate = data.numPlate,
                InTime = data.inTime,
                OutTime = data.outTime,
                MinsParked = data.minsParked,
                Rate = data.rate,
                TotalCost = data.totalCost
            };

            // SignalR 브로드캐스트 (문자열로 변환된 Id 사용)
            await _hubContext.Clients.All.SendAsync("ReceiveChange", responseData);

            // CreatedAtAction에 변환된 데이터 사용
            return CreatedAtAction(nameof(GetDatabase1DataById), new { id = data.Id.ToString() }, responseData);
        }


        // PUT: 데이터 업데이트 (Database1 - entries)
        [HttpPut("database1/{id:length(24)}")]
        public async Task<IActionResult> UpdateDatabase1(string id, [FromBody] entriesData updatedData)
        {
            var collection = _service.GetentriesCollection("entries");
            var filter = Builders<entriesData>.Filter.Eq(x => x.Id, ObjectId.Parse(id));

            try
            {
                if (!string.IsNullOrEmpty(updatedData.inTime) && !string.IsNullOrEmpty(updatedData.outTime))
                {
                    int inMinutes = ConvertTimeStringToMinutes(updatedData.inTime);
                    int outMinutes = ConvertTimeStringToMinutes(updatedData.outTime);

                    if (outMinutes < inMinutes)
                    {
                        return BadRequest("Invalid time range: outTime must be later than inTime.");
                    }

                    int duration = outMinutes - inMinutes;
                    updatedData.minsParked = duration; // 주차 시간(분 단위)
                    updatedData.totalCost = CalculateTotalCost(duration, updatedData.rate);
                }
            }
            catch (FormatException ex)
            {
                return BadRequest(ex.Message);
            }

            var update = Builders<entriesData>.Update
                .Set(x => x.column, updatedData.column)
                .Set(x => x.numPlate, updatedData.numPlate)
                .Set(x => x.inTime, updatedData.inTime)
                .Set(x => x.outTime, updatedData.outTime)
                .Set(x => x.minsParked, updatedData.minsParked)
                .Set(x => x.rate, updatedData.rate)
                .Set(x => x.totalCost, updatedData.totalCost)
                .Set(x => x.etc, updatedData.etc);

            var result = await collection.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0) return NotFound();

            // SignalR을 통해 업데이트된 데이터를 브로드캐스트
            await _hubContext.Clients.All.SendAsync("ReceiveChange", new
            {
                Id = id,
                Column = updatedData.column,
                NumPlate = updatedData.numPlate,
                InTime = updatedData.inTime,
                OutTime = updatedData.outTime,
                MinsParked = updatedData.minsParked,
                Rate = updatedData.rate,
                TotalCost = updatedData.totalCost,
                Etc = updatedData.etc
            });

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

        // 시간 문자열을 분 단위로 변환하는 함수 (기본 함수)
        private int ConvertTimeStringToMinutes(string timeString)
        {
            // 시간 문자열 형식 예: "20241212-144830"
            if (timeString.Length != 15 || timeString[8] != '-')
            {
                throw new FormatException("Invalid time format. Expected format: YYYYMMDD-HHMMSS.");
            }

            // 시분 데이터 추출
            string hourMinute = timeString.Substring(9, 4); // "1448"
            int hour = int.Parse(hourMinute.Substring(0, 2)); // "14"
            int minute = int.Parse(hourMinute.Substring(2, 2)); // "48"

            return hour * 60 + minute; // 분 단위 시간 반환
        }


        // 요금 계산 함수
        private int CalculateTotalCost(int duration, int rate)
        {
            const int baseRate = 10000; // 기본요금
            int additionalCost = 0;

            if (duration > 60)
            {
                int additionalMinutes = duration - 60;
                int additionalUnits = (int)Math.Ceiling(additionalMinutes / 10.0);
                additionalCost = additionalUnits * rate;
            }

            return baseRate + additionalCost;
        }

    }
}
