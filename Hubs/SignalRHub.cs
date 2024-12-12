// SignalRHub.cs
using Microsoft.AspNetCore.SignalR;

namespace parking.Hubs
{
    //클라이언트와 서버 간의 실시간 데이터를 송수신하는 역할을 수행하는 SignalRHub 클래스 정의
    public class SignalRHub : Hub   
        //SignalRHub은 Hub 클래스를 상속받음
        // Hub는 SignalR의 기본 클래스이며, 서버와 클라이언트 간의 실시간 연결을 관리하는 역할
    {
        // 클라이언트로 메시지를 전송
        public async Task SendMessage(string user, string message)
        //SendMessage는 비동기로 실행
        //이는 다수의 클라이언트가 연결된 상황에서도 효율적인 메시지 처리가 가능하도록 보장
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
            //Clients.All.SendAsync
            //연결된 모든 클라이언트에게 메시지를 브로드캐스트
            //클라이언트는 "ReceiveMessage"라는 이벤트를 수신하며,
            //이때 사용자 이름과 메시지 내용이 함께 전달
        }
    }
}
/*
 SignalR Hub 역할
    1. 실시간 브로드캐스트
        Clients.All는 연결된 모든 클라이언트에게 메시지를 전송
    2. 클라이언트와 서버 간의 양방향 통신
        클라이언트가 메시지를 허브로 전송 →  
        허브에서 이를 처리하고 필요한 클라이언트로 데이터를 전달
    3. 실시간 데이터 공유
        SendMessage 같은 메서드를 통해 여러 사용자가 동일한 데이터를 동시에 볼 수 있음
 */