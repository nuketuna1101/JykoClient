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
        // ������ Ŭ���̾�Ʈ ���
        _httpClient = httpClient;
        // Ŭ���̾�Ʈ �ּ� ���
        _httpClient.BaseAddress = new Uri(url);
        // ���� guid ����
        _guid = Guid.NewGuid();
    }

    public async Task<byte[]> SendPing()
    {
        // ���� Ȯ�ο� �׽�Ʈ
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
            httpResponse.EnsureSuccessStatusCode();         // http response�� ���� ���� �� ���� throw
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
            httpResponse.EnsureSuccessStatusCode();         // http response�� ���� ���� �� ���� throw
            return await httpResponse.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            Debug.Log("Exception throwed : " + ex.Message);
            var httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            return Array.Empty<byte>();
        }
    }

    // WebSocket ���� �޼���
    // WebSocket ���� �޼���
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

    // WebSocket �޽��� ���� �޼���
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
