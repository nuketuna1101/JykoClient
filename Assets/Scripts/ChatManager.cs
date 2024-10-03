using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using MemoryPack;
using System.Diagnostics;
using System.Net.WebSockets;

public class ChatManager : MonoBehaviour
{
    public InputField messageInput; //
    public Text chatDisplay; // 
    private NetworkManager _NetworkManager = new NetworkManager(new HttpClient());


    /*
     TODO :: 채팅 금칙어 설정해서 금칙어 필터링시키기  
     */

    private void Awake()
    {
        UnityEngine.Debug.Log("guid : " + _NetworkManager._guid);
        StartCoroutine(ConnectWebSocket());

    }


    //---------------------------------------------------------
    public void PingTest()
    {
        UnityEngine.Debug.Log("[CM] -- [PingTest occurred]");
        StartCoroutine(PingTestCoroutine());
    }
    //debug coroutine
    private IEnumerator PingTestCoroutine()
    {
        UnityEngine.Debug.Log("[CM] -- [PingTestCoroutine occurred]");
        Stopwatch sw = Stopwatch.StartNew();
        Task<byte[]> ping = _NetworkManager.SendPing();
        yield return new WaitUntil(() => ping.IsCompleted);
        sw.Stop();
        DateTime serverTime;
        try
        {
            serverTime = MemoryPackSerializer.Deserialize<DateTime>(ping.Result);
        }
        catch
        {
            UnityEngine.Debug.Log("Connection Error");
            yield break;
        }
        // 성공하면 서버 시간 호출
        UnityEngine.Debug.Log(string.Join(", ", sw.ElapsedMilliseconds, serverTime));
    }

    //---------------------------------------------------------


    public void MakeMessage()
    {
        StartCoroutine(MakeMessageCoroutine());
    }

    IEnumerator MakeMessageCoroutine()
    {
        // async로 save 통신 처리
        string message = messageInput.text;
        Request request = new()
        {
            UserGUID = _NetworkManager._guid,
            Msg = MemoryPackSerializer.Serialize(message),
        };

        Task<byte[]> MakeMessageTask = _NetworkManager.PostAsync("/Send", request);
        yield return new WaitUntil(() => MakeMessageTask.IsCompleted);
        // if success, visualize sth to user.. ex) toast msg, otherwise error msg
        if (MakeMessageTask.IsCompletedSuccessfully)
        {
            UnityEngine.Debug.Log("+----- :: Send Completed!");
        }
        else
        {
            UnityEngine.Debug.Log("+----- :: Send failed!");
        }
    }

    //---------------------------------------------------------

    public void Chat()
    {
        StartCoroutine(ChatCoroutine());
    }

    IEnumerator ChatCoroutine()
    {
        // async로 save 통신 처리
        string message = messageInput.text;
        Request request = new()
        {
            UserGUID = _NetworkManager._guid,
            Msg = MemoryPackSerializer.Serialize(message),
        };

        Task<byte[]> MakeMessageTask = _NetworkManager.PostAsync("/Chat", request);
        yield return new WaitUntil(() => MakeMessageTask.IsCompleted);
        // if success, visualize sth to user.. ex) toast msg, otherwise error msg
        if (MakeMessageTask.IsCompletedSuccessfully)
        {
            UnityEngine.Debug.Log("+----- :: Chat Completed!");
        }
        else
        {
            UnityEngine.Debug.Log("+----- :: Chat failed!");
        }
    }

    //-----------------------------------------------------

    // websocket
    private ClientWebSocket _webSocket;
    private IEnumerator ConnectWebSocket()
    {
        UnityEngine.Debug.Log("[CM] -- Connecting WebSocket...");
        //var connectTask = _NetworkManager.ConnectWebSocketAsync("wss://localhost:7233/Websockethandler");
        var connectTask = _NetworkManager.ConnectWebSocketAsync("wss://localhost:7233/ws");
        yield return new WaitUntil(() => connectTask.IsCompleted);

        if (connectTask.IsCompletedSuccessfully)
        {
            UnityEngine.Debug.Log("WebSocket connected successfully.");
        }
        else
        {
            UnityEngine.Debug.LogError("WebSocket connection failed.");
            if (connectTask.Exception != null)
            {
                UnityEngine.Debug.LogError(connectTask.Exception.Message);
            }
        }
    }

    public void SendByWebSocket()
    {
        StartCoroutine(SendWebSocketMessage());
    }

    // WebSocket 메시지 전송 코루틴
    private IEnumerator SendWebSocketMessage()
    {
        string message = messageInput.text;
        if (string.IsNullOrEmpty(message))
        {
            UnityEngine.Debug.Log("Message is empty");
            yield break;
        }

        // WebSocket 메시지 전송
        Task sendTask = _NetworkManager.SendWebSocketMessageAsync(message);
        yield return new WaitUntil(() => sendTask.IsCompleted);

        if (sendTask.IsCompletedSuccessfully)
        {
            UnityEngine.Debug.Log("WebSocket message sent successfully.");
        }
        else
        {
            UnityEngine.Debug.LogError("Failed to send WebSocket message.");
        }
    }

}

public enum RequestType
{
    PING = 20,
    SEND = 30,
}

[MemoryPackable]
public partial class Request
{
    public Guid UserGUID { get; set; }
    public byte[] Msg { get; set; }
    [MemoryPackConstructor]
    public Request()
    {
        UserGUID = Guid.Empty;
        Msg = Array.Empty<byte>();
    }
}

[MemoryPackable]
public partial class Response
{
    public short TypeNo;
    public byte[] Msg;
    public Response()
    {
        TypeNo = -1;
        Msg = Array.Empty<byte>();
    }
    [MemoryPackConstructor]
    public Response(short TypeNo)
    {
        this.TypeNo = TypeNo;
        Msg = Array.Empty<byte>();
    }
}