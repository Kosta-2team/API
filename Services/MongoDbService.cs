using Microsoft.Extensions.Options;
using MongoDB.Driver;
using parking.Models;
using Microsoft.AspNetCore.SignalR;
using parking.Hubs;

//MongoDB와 상호작용하기 위한 서비스를 제공
//데이터베이스 및 컬렉션 관리, 데이터 변경 모니터링 등을 처리
namespace parking.Services
{
    public class MongoDbService
    {
        // 변수 IMongoDatabase 객체 초기화
        //두 개의 데이터베이스(Database1과 Database2)를 설정
        //각각 entries와 members 관련 작업을 처리
        private readonly IMongoDatabase _car;
        private readonly IMongoDatabase _member;

        // 변수 _hubContext : SignalR 허브와 상호작용하기 위한 객체
        //변경된 데이터를 클라이언트로 브로드캐스트
        private readonly IHubContext<SignalRHub> _hubContext;

        // 생성자
        // 1) MongoSettings.cs에서 ConnectionString, Database1Name, Database2Name을 읽어옴
        // MongoClient를 생성하여 MongoDB 서버와 연결
        public MongoDbService(IOptions<MongoSettings> settings, IHubContext<SignalRHub> hubContext)
        {
            var connectionString = settings.Value.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "MongoDB connection string is null.");

            // 2) 데이터베이스 초기화
            // DatabaseName에 해당하는 데이터베이스를 각 변수에 저장
            var client = new MongoClient(connectionString);
            _car = client.GetDatabase(settings.Value.Database1Name);
            _member = client.GetDatabase(settings.Value.Database2Name);

            // 3) SignalR 허브 초기화
            // _hubContext를 통해 SignalR과 통신 설정
            _hubContext = hubContext;
        }

        // 변수 _watchedCollections
        // 변경 감시가 이미 설정된 컬렉션을 관리하는 딕셔너리, 중복 감시를 방지
        private readonly Dictionary<string, Task> _watchedCollections = new();

        // 각 컬렉션에 접근하거나, 필요시 변경 감시를 시작
        // 1. 접근 : MongoDB.Driver의 GetCollection<T> 메서드 사용
        // 2. 변경 감시 시작
        // 2-1 _watchedCollections에 컬렉션 이름이 없으면 WatchChanges를 호출해 변경 사항을 감시
        // 2-2 감시를 시작한 컬렉션 이름은 _watchedCollections에 추가
        public IMongoCollection<entriesData> GetentriesCollection(string collectionName)
        {
            if (!_watchedCollections.ContainsKey(collectionName))
            {
                var collection = _car.GetCollection<entriesData>(collectionName);
                WatchChanges(collection);
                _watchedCollections[collectionName] = Task.CompletedTask; // 이미 구독 처리된 컬렉션
            }
            return _car.GetCollection<entriesData>(collectionName);
        }

        public IMongoCollection<membersData> GetmembersCollection(string collectionName)
        {
            if (!_watchedCollections.ContainsKey(collectionName))
            {
                var collection = _member.GetCollection<membersData>(collectionName);
                WatchChanges(collection);
                _watchedCollections[collectionName] = Task.CompletedTask; // 이미 구독 처리된 컬렉션
            }
            return _member.GetCollection<membersData>(collectionName);
        }

        // 변수 _processedIds : 이미 처리된 MongoDB Document ID를 저장
        //동일한 변경 사항이 여러 번 처리되지 않도록 함
        private readonly HashSet<string> _processedIds = new();

        // 함수 WatchChanges : 데이터 변경 감시
        //MongoDB Change Stream을 사용하여 데이터 변경 감시 및 SignalR로 클라이언트에 알림
        //데이터 변경 사항 발생 시 SignalR Hub를 통해 클라이언트에게 실시간 업데이트를 보냄
        //클라이언트는 SignalR을 통해 변경된 데이터를 실시간으로 수신
        private void WatchChanges<T>(IMongoCollection<T> collection)
        {
            // Change Stream 파이프라인 정의
            // MongoDB의 ChangeStream을 사용하여 컬렉션의 생성, 수정, 삭제 이벤트를 감시
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<T>>()
                .Match(x => x.OperationType == ChangeStreamOperationType.Insert ||
                            x.OperationType == ChangeStreamOperationType.Update ||
                            x.OperationType == ChangeStreamOperationType.Delete);

            var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
            // FullDocumentOption.UpdateLookup: 변경된 전체 문서를 포함
            var cursor = collection.Watch(pipeline, options);

            // 변경 사항 처리
            Task.Run(() =>
            {
                // 변경 사항을 cursor.ToEnumerable()로 반복 탐색
                foreach (var change in cursor.ToEnumerable())                
                {
                    // 각 변경 사항의 Document ID를 확인하여 중복 처리를 방지
                    var documentId = change.DocumentKey?.GetElement("_id").Value.ToString();
                    if (documentId == null || _processedIds.Contains(documentId)) continue;

                    //SignalR를 통해 알림
                    //변경된 데이터를 ReceiveChange 이벤트로 클라이언트에 전송
                    //_processedIds에 처리된 ID를 추가하여 중복 전송을 방지
                    _processedIds.Add(documentId);
                    _hubContext.Clients.All.SendAsync("ReceiveChange", change.FullDocument).Wait();
                }
            });
        }

    }
}
