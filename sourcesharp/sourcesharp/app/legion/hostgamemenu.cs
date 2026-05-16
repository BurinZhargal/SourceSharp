using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sourcesharp.app.legion
{
    public class CHostGameMenu : CBaseMenu, INetworkMessageListener
    {
        public CHostGameMenu(GameObject hostGameMenuGO) : base(hostGameMenuGO)
        {
            m_pPlayerList = hostGameMenuGO.transform.Find("PlayerList").GetComponent<ListPanel>();
            m_pServerIP = hostGameMenuGO.transform.Find("ServerIP").GetComponent<TextEntry>();
            m_pServerName = hostGameMenuGO.transform.Find("ServerName").GetComponent<TextEntry>();
            m_pChatLog = hostGameMenuGO.transform.Find("ChatLog").GetComponent<TextEntry>();
            m_pChatEntry = hostGameMenuGO.transform.Find("ChatEntry").GetComponent<TextEntry>();
            m_pPlayerName = hostGameMenuGO.transform.Find("PlayerName").GetComponent<TextEntry>();
            m_pStartGame = hostGameMenuGO.transform.Find("StartGame").GetComponent<Button>();

            m_pStartGame.onClick.AddListener(OnStartGameClick);
        }

        public void OnNetworkMessage(NetworkMessageRoute_t route, INetworkMessage pNetworkMessage)
        {
            // Handle network messages
        }

        public void OnCommand(string pCommand)
        {
            // Handle commands
        }

        public void OnStartGameClick()
        {
            // Handle start game button click
        }

        public void OnTextNewLine()
        {
            // Handle new line of text
        }

        private ListPanel m_pPlayerList;
        private TextEntry m_pServerIP;
        private TextEntry m_pServerName;
        private TextEntry m_pChatLog;
        private TextEntry m_pChatEntry;
        private TextEntry m_pPlayerName;
        private Button m_pStartGame;
        public class HostGameMenu : Panel
        {
            private ListPanel m_playerList;
            private TextEntry m_serverIP;
            private TextEntry m_serverName;
            private TextEntry m_playerName;
            private TextEntry m_chatLog;
            private TextEntry m_chatEntry;
            private Button m_startGame;

            public HostGameMenu(Panel pParent, string pPanelName) : base(pParent, pPanelName)
            {
                m_playerList = new ListPanel(this, "PlayerList");
                m_playerList.AddColumnHeader(0, "color", "Color", 52, 0);
                m_playerList.AddColumnHeader(1, "player", "Player Name", 128, 0);
                m_playerList.SetSelectIndividualCells(false);
                m_playerList.SetEmptyListText("No Players");
                m_playerList.SetDragEnabled(false);
                m_playerList.AddActionSignalTarget(this);
                m_playerList.SetSortFunc(0, PlayerNameSortFunc);
                m_playerList.SetSortFunc(1, PlayerNameSortFunc);
                m_playerList.SetSortColumn(1);

                m_serverIP = new TextEntry(this, "ServerIP");
                m_serverName = new TextEntry(this, "ServerName");

                m_playerName = new TextEntry(this, "PlayerName");
                m_playerName.SetMultiline(false);

                m_chatLog = new TextEntry(this, "ChatLog");
                m_chatLog.SetMultiline(true);
                m_chatLog.SetVerticalScrollbar(true);

                m_chatEntry = new TextEntry(this, "ChatEntry");
                m_chatEntry.AddActionSignalTarget(this);
                m_chatEntry.SetMultiline(false);
                m_chatEntry.SendNewLine(true);

                m_startGame = new Button(this, "StartGame", "Start Game", this);

                // Load control settings from a file
                // The equivalent of this will depend on your specific project layout
                LoadControlSettingsFromFile("resource/hostgamemenu.res");

                m_playerName.SetText("Unnamed");

                if (!NetworkManager.Instance.HostGame())
                {
                    m_startGame.SetEnabled(false);
                    return;
                }

                m_serverIP.SetText(NetworkSystem.Instance.GetLocalAddress());
                m_serverName.SetText(NetworkSystem.Instance.GetLocalHostName());

                g_pNetworkManager.RemoveListener(NETWORK_MESSAGE_SERVER_TO_CLIENT, LEGION_NETMESSAGE_GROUP, CHAT_MESSAGE, this);
                g_pNetworkManager.RemoveListener(NETWORK_MESSAGE_CLIENT_TO_SERVER, LEGION_NETMESSAGE_GROUP, CHAT_MESSAGE, this);
            }
            ~CHostGameMenu()
            {
                g_pNetworkManager.RemoveListener(NETWORK_MESSAGE_SERVER_TO_CLIENT, LEGION_NETMESSAGE_GROUP, CHAT_MESSAGE, this);
                g_pNetworkManager.RemoveListener(NETWORK_MESSAGE_CLIENT_TO_SERVER, LEGION_NETMESSAGE_GROUP, CHAT_MESSAGE, this);
            }
            void OnNetworkMessage(NetworkMessageRoute_t route, INetworkMessage pNetworkMessage)
            {
                if (route == NETWORK_MESSAGE_SERVER_TO_CLIENT)
                {
                    CNetworkMessage_Chat pChatMsg = (CNetworkMessage_Chat)pNetworkMessage;
                    m_pChatLog.InsertString(pChatMsg.m_Message.Get());
                    m_pChatLog.InsertChar('\n');
                }
                else
                {
                    // If this message was received from an client, broadcast it to all other clients
                    g_pNetworkManager.BroadcastServerToClientMessage(pNetworkMessage);
                }
            }
            void OnTextNewLine()
            {
                CNetworkMessage_Chat msg = new CNetworkMessage_Chat();

                int nLen = m_pChatEntry.GetTextLength();
                if (nLen > 0)
                {
                    char[] pText = new char[nLen + 1];
                    m_pChatEntry.GetText(pText, nLen + 1);
                    m_pChatEntry.SetText("");

                    int nLenName = m_pPlayerName.GetTextLength();
                    char[] pName = new char[nLenName + 8];
                    if (nLenName == 0)
                    {
                        nLenName = 7;
                        Q_strcpy(pName, "unnamed");
                    }
                    else
                    {
                        m_pPlayerName.GetText(pName, nLenName + 1);
                    }

                    int nTotalLen = nLen + nLenName;

                    char[] message = new char[nTotalLen + 3];
                    Q_snprintf(message, nTotalLen + 3, $"[{pName}] {pText}");
                    msg.m_Message = new string(message);

                    g_pNetworkManager.PostClientToServerMessage(msg);
                }
            }
            void OnCommand(string pCommand)
            {
                if (string.Equals(pCommand, "CancelHostGame", StringComparison.OrdinalIgnoreCase))
                {
                    g_pNetworkManager.StopHostingGame();
                    g_pMenuManager.PopMenu();
                    return;
                }

                if (string.Equals(pCommand, "StartGame", StringComparison.OrdinalIgnoreCase))
                {
                    IGameManager.StartNewLevel();
                    g_pMenuManager.PopAllMenus();
                    return;
                }

                BaseClass.OnCommand(pCommand);
            }


        }
    }
}
