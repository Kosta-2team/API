using Microsoft.Extensions.Options;
using MongoDB.Driver;
using parking.Hubs;
using parking.Models;
using parking.Services;

// 애플리케이션 빌더를 생성
var builder = WebApplication.CreateBuilder(args);

// SignalR 허브 등록
// SignalRHub가 클라이언트와 실시간 통신을 지원할 수 있도록 설정
builder.Services.AddSignalR();

// MongoDB 설정 등록
// appsettings.json 또는 기타 환경 변수에서 MongoSettings 섹션을 읽어옴
builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("MongoSettings"));

// MongoClient 등록
// MongoDB 클라이언트를 싱글톤으로 등록하여 앱 전역에서 하나의 클라이언트를 재사용
builder.Services.AddSingleton<IMongoClient, MongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

// MongoDbService 등록
// 데이터베이스 접근과 관련된 비즈니스 로직을 포함
// 싱글톤으로 등록하여 SignalR 허브와 컨트롤러에서 동일한 인스턴스를 사용
builder.Services.AddSingleton<MongoDbService>();

// 컨트롤러 등록
// MVC 기반 API 컨트롤러를 애플리케이션에 추가
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 설정된 빌더를 기반으로 애플리케이션 객체를 생성
var app = builder.Build();

// Static Files 및 SignalR 라우팅 설정
app.UseDefaultFiles();
app.UseStaticFiles();

// SignalR 허브 라우팅, 실시간 통신 설정
app.MapHub<SignalRHub>("/hubs/notification");

// Swagger UI 활성화
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Authorization 미들웨어 : 인증된 사용자를 위한 API 접근을 제한할 수 있음
app.UseAuthorization();
//컨트롤러 라우팅 : API 요청을 등록된 컨트롤러와 연결
app.MapControllers();

app.Run();

/*
    1) MongoDB 연결 설정
        MongoDbService가 MongoDB 데이터베이스와 연결을 설정
    2) SignalR 설정
        클라이언트가 /hubs/notification 경로로 SignalR 허브에 연결할 수 있음
    3) REST API 구성
        DataController에서 정의한 API 엔드포인트가 활성화
    4) Swagger UI 활성화
        개발 환경에서 Swagger를 통해 API 테스트 UI를 제공
    5) 정적 파일 제공(해당 부분은 테스트를 위한 부분이므로 본 프로젝트와 무관)
        정적 리소스(HTML, CSS, JS)를 클라이언트에 제공
 */