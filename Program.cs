using Microsoft.Extensions.Options;
using MongoDB.Driver;
using parking.Hubs;
using parking.Models;
using parking.Services;

// ���ø����̼� ������ ����
var builder = WebApplication.CreateBuilder(args);

// SignalR ��� ���
// SignalRHub�� Ŭ���̾�Ʈ�� �ǽð� ����� ������ �� �ֵ��� ����
builder.Services.AddSignalR();

// MongoDB ���� ���
// appsettings.json �Ǵ� ��Ÿ ȯ�� �������� MongoSettings ������ �о��
builder.Services.Configure<MongoSettings>(
    builder.Configuration.GetSection("MongoSettings"));

// MongoClient ���
// MongoDB Ŭ���̾�Ʈ�� �̱������� ����Ͽ� �� �������� �ϳ��� Ŭ���̾�Ʈ�� ����
builder.Services.AddSingleton<IMongoClient, MongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

// MongoDbService ���
// �����ͺ��̽� ���ٰ� ���õ� ����Ͻ� ������ ����
// �̱������� ����Ͽ� SignalR ���� ��Ʈ�ѷ����� ������ �ν��Ͻ��� ���
builder.Services.AddSingleton<MongoDbService>();

// ��Ʈ�ѷ� ���
// MVC ��� API ��Ʈ�ѷ��� ���ø����̼ǿ� �߰�
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ������ ������ ������� ���ø����̼� ��ü�� ����
var app = builder.Build();

// Static Files �� SignalR ����� ����
app.UseDefaultFiles();
app.UseStaticFiles();

// SignalR ��� �����, �ǽð� ��� ����
app.MapHub<SignalRHub>("/hubs/notification");

// Swagger UI Ȱ��ȭ
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Authorization �̵���� : ������ ����ڸ� ���� API ������ ������ �� ����
app.UseAuthorization();
//��Ʈ�ѷ� ����� : API ��û�� ��ϵ� ��Ʈ�ѷ��� ����
app.MapControllers();

app.Run();

/*
    1) MongoDB ���� ����
        MongoDbService�� MongoDB �����ͺ��̽��� ������ ����
    2) SignalR ����
        Ŭ���̾�Ʈ�� /hubs/notification ��η� SignalR ��꿡 ������ �� ����
    3) REST API ����
        DataController���� ������ API ��������Ʈ�� Ȱ��ȭ
    4) Swagger UI Ȱ��ȭ
        ���� ȯ�濡�� Swagger�� ���� API �׽�Ʈ UI�� ����
    5) ���� ���� ����(�ش� �κ��� �׽�Ʈ�� ���� �κ��̹Ƿ� �� ������Ʈ�� ����)
        ���� ���ҽ�(HTML, CSS, JS)�� Ŭ���̾�Ʈ�� ����
 */