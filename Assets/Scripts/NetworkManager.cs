using MemoryPack;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class NetworkManager
{
    private string url = "https://localhost:7233";
    private readonly HttpClient _httpClient;
    public readonly Guid _guid;

    private ClientWebSocket webSocket;

    public NetworkManager(HttpClient httpClient)
    {
        // 생성자 클라이언트 등록
        _httpClient = httpClient;
        // 클라이언트 주소 등록
        _httpClient.BaseAddress = new Uri(url);
        // 유저 guid 생성
        _guid = Guid.NewGuid();
    }

    public async Task<byte[]> SendPing()
    {
        // 연결 확인용 테스트
        string httpResponseBody = "";
        UnityEngine.Debug.Log("[NM] PING test ..");

        Request request = new()
        {
            UserGUID = _guid,
            Msg = MemoryPackSerializer.Serialize("Ping"),
        };

        try
        {
            using var httpResponse = await _httpClient.PostAsync("/Ping", new ByteArrayContent(MemoryPackSerializer.Serialize(request)));
            httpResponse.EnsureSuccessStatusCode();         // http response에 대해 실패 시 예외 throw
            return await httpResponse.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log("Exception throwed : " + ex.Message);
            httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            return Array.Empty<byte>();
        }
    }
    public async Task<byte[]> PostAsync(string path, Request request)
    {
        var reqSerial = MemoryPackSerializer.Serialize(request);
        try
        {
            using var httpResponse = await _httpClient.PostAsync(path, new ByteArrayContent(reqSerial));
            httpResponse.EnsureSuccessStatusCode();         // http response에 대해 실패 시 예외 throw
            return await httpResponse.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            Debug.Log("Exception throwed : " + ex.Message);
            var httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            return Array.Empty<byte>();
        }
    }

    // WebSocket 연결 메서드
    // WebSocket 연결 메서드
    public async Task ConnectWebSocketAsync(string url)
    {
        webSocket = new ClientWebSocket();
        try
        {
            await webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
        }
        catch (Exception ex)
        {
            Debug.LogError("WebSocket connection failed: " + ex.Message);
        }
    }

    // WebSocket 메시지 전송 메서드
    public async Task SendWebSocketMessageAsync(string message)
    {
        if (webSocket == null || webSocket.State != WebSocketState.Open)
        {
            Debug.LogError("WebSocket is not connected.");
            return;
        }

        var buffer = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);
        await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
