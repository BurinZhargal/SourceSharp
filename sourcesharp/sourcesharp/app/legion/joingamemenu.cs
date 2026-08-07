using System;
using System.Collections.Generic;

namespace sourcesharp.app.legion;

// Одно объявлением файла вместо связки .h + .cpp. C# избавил нас от лишних макетов.
internal class JoinGameMenu : BaseMenu, INetworkMessageListener
{
    // Компоненты UI (в C# инициализируются напрямую или через фабрику)
    private readonly ListPanel _playerList;
    private readonly TextEntry _chatLog;
    private readonly TextEntry _serverName;
    private readonly TextEntry _serverPort;
    private readonly TextEntry _chatEntry;
    private readonly TextEntry _playerName;
    private readonly Button _joinGame;
    
    private bool _isJoiningGame;

    public JoinGameMenu(Panel parent, string panelName) : base(parent, panelName)
    {
        // Инициализация списков и колонок
        _playerList = new ListPanel(this, "PlayerList");
        _playerList.AddColumnHeader(0, "color", "Color", 52);
        _playerList.AddColumnHeader(1, "player", "Player Name", 128);
        _playerList.SetEmptyListText("No Players");
        
        // Лямбда-выражение C# заменяет старую статическую функцию сортировки __cdecl
        _playerList.SetSortFunc(1, (item1, item2) => 
            string.Compare(item1.Kv.GetString("player"), item2.Kv.GetString("player"), StringComparison.OrdinalIgnoreCase));
        _playerList.SetSortColumn(1);

        _serverName = new TextEntry(this, "ServerName");
        _serverPort = new TextEntry(this, "ServerPort");
        _serverPort.SetText(NetworkSystem.DefaultServerPort.ToString());

        _playerName = new TextEntry(this, "PlayerName") { Multiline = false };
        _playerName.SetText("Unnamed");

        _chatLog = new TextEntry(this, "ChatLog") { Multiline = true, VerticalScrollbar = true };

        _chatEntry = new TextEntry(this, "ChatEntry") { Multiline = false };
        _chatEntry.OnNewLine += OnTextNewLine; // Удобное событие вместо макросов MESSAGE_FUNC
        _chatEntry.SetEnabled(false);

        _joinGame = new Button(this, "JoinGame", "Join Game", OnCommand);

        LoadControlSettings("resource/joingamemenu.res", "GAME");

        // Сетевая инициализация
        if (!NetworkManager.StartClient())
        {
            _joinGame.SetEnabled(false);
            return;
        }

        NetworkManager.AddListener(NetworkMessageRoute.ServerToClient, "CHAT_MESSAGE", this);
    }

    // Деструктор заменяется на интерфейс IDisposable или стандартный Dispose метод
    public void Dispose()
    {
        NetworkManager.RemoveListener(NetworkMessageRoute.ServerToClient, "CHAT_MESSAGE", this);
    }

    // Обработка входящего сетевого сообщения чата
    public void OnNetworkMessage(NetworkMessageRoute route, INetworkMessage networkMessage)
    {
        if (networkMessage is NetworkMessageChat chatMsg)
        {
            // Прямая вставка строки без ручной конкатенации символов
            _chatLog.InsertString(chatMsg.Message + "\n");
        }
    }

    // Отправка сообщения в чат по нажатию Enter
    private void OnTextNewLine()
    {
        string text = _chatEntry.GetText().Trim();
        if (string.IsNullOrEmpty(text)) return;

        _chatEntry.SetText(string.Empty);

        string name = _playerName.GetText().Trim();
        if (string.IsNullOrEmpty(name))
        {
            name = "unnamed";
        }

        // Интерполяция строк .NET 10 собирает строку атомарно и эффективно
        var msg = new NetworkMessageChat { Message = $"[{name}] {text}" };
        NetworkManager.PostClientToServerMessage(msg);
    }

    // Логика кнопок и команд интерфейса
    public void OnCommand(string command)
    {
        if (string.Equals(command, "Cancel", StringComparison.OrdinalIgnoreCase))
        {
            NetworkManager.ShutdownClient();
            MenuManager.PopMenu();
            return;
        }

        if (string.Equals(command, "JoinGame", StringComparison.OrdinalIgnoreCase))
        {
            if (!_isJoiningGame)
            {
                NetworkManager.DisconnectClientFromServer();
                _chatEntry.SetEnabled(false);
                _chatEntry.SetText(string.Empty);
                _isJoiningGame = true;
                _joinGame.SetText("Join Game");
            }
            else
            {
                string server = _serverName.GetText();
                string portStr = _serverPort.GetText();
                int.TryParse(portStr, out int port);

                if (NetworkManager.ConnectClientToServer(server, port))
                {
                    _chatEntry.SetEnabled(true);
                    _isJoiningGame = false;
                    _joinGame.SetText("Leave Game");
                }
            }
            return;
        }

        base.OnCommand(command);
    }
}

// --- Минимальные заглушки структуры UI/Network для компиляции ---
internal class BaseMenu : Panel { public BaseMenu(Panel p, string n) : base(p, n) {} public virtual void OnCommand(string c) {} }
internal class Panel { public Panel(Panel p, string n) {} }
internal class ListPanel : Panel { public ListPanel(Panel p, string n) : base(p, n) {} public void AddColumnHeader(int id, string k, string name, int w) {} public void SetEmptyListText(string t) {} public void SetSortFunc(int col, Func<ListItem, ListItem, int> f) {} public void SetSortColumn(int col) {} }
internal class ListItem { public KeyValues Kv { get; } = new(); }
internal class KeyValues { public string GetString(string key) => "Player"; }
internal class TextEntry : Panel { public TextEntry(Panel p, string n) : base(p, n) {} public bool Multiline { get; set; } public bool VerticalScrollbar { get; set; } public event Action? OnNewLine; public void SetEnabled(bool e) {} public void SetText(string t) {} public string GetText() => ""; public void InsertString(string t) {} }
internal class Button : Panel { public Button(Panel p, string n, string label, Action<string> cmd) : base(p, n) {} public void SetEnabled(bool e) {} public void SetText(string t) {} }
internal interface INetworkMessageListener { void OnNetworkMessage(NetworkMessageRoute r, INetworkMessage m); }
internal interface INetworkMessage { string Message { get; } }
internal class NetworkMessageChat : INetworkMessage { public string Message { get; init; } = ""; }
internal enum NetworkMessageRoute { ServerToClient }
internal static class NetworkSystem { public const int DefaultServerPort = 27015; }
internal static class NetworkManager { public static bool StartClient() => true; public static void ShutdownClient() {} public static void DisconnectClientFromServer() {} public static bool ConnectClientToServer(string s, int p) => true; public static void AddListener(NetworkMessageRoute r, string msg, INetworkMessageListener l) {} public static void RemoveListener(NetworkMessageRoute r, string msg, INetworkMessageListener l) {} public static void PostClientToServerMessage(INetworkMessage m) {} }
internal static class MenuManager { public static void PopMenu() {} }
