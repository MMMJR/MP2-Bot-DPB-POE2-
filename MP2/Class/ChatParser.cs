﻿using DreamPoeBot.Loki.Game;
using MP2.EXtensions;
using chatPanel = DreamPoeBot.Loki.Game.LokiPoe.InGameState.ChatPanel;

namespace MP2.Class
{
    public class ChatParser
    {
        public bool shouldCleanMessages;
        private volatile string _lastMd5;
        private volatile Dictionary<string, bool> _treatedMd5List = new Dictionary<string, bool>();

        public ChatParser()
        {
            _treatedMd5List.Clear();
            shouldCleanMessages = true;
            _lastMd5 = "";
        }

        public static LokiPoe.InGameState.ChatResult SendChatMsg(string msg, bool closeChatUi = true)
        {
            if (string.IsNullOrEmpty(msg))
            {
                return LokiPoe.InGameState.ChatResult.None;
            }
            if (!LokiPoe.InGameState.ChatPanel.IsOpened)
            {
                LokiPoe.InGameState.ChatPanel.ToggleChat();
            }

            if (!LokiPoe.InGameState.ChatPanel.IsOpened)
            {
                return LokiPoe.InGameState.ChatResult.UiNotOpen;
            }

            LokiPoe.InGameState.ChatResult result = LokiPoe.InGameState.ChatPanel.Chat(msg);

            if (closeChatUi)
            {
                if (LokiPoe.InGameState.ChatPanel.IsOpened)
                {
                    LokiPoe.InGameState.ChatPanel.ToggleChat();
                }
            }

            return result;
        }

        private void CleanMessage()
        {
            if (!LokiPoe.IsInGame) return;
            SendChatMsg("/cls");
            List<chatPanel.ChatEntry> msgs = LokiPoe.InGameState.ChatPanel.Messages.ToList();
            if (msgs.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < msgs.Count; i++)
            {
                var chatEntry = msgs[i];
                _lastMd5 = chatEntry.MD5;
                if (_treatedMd5List.TryGetValue(chatEntry.MD5, out bool alreadyTreated))
                {
                    if (alreadyTreated)
                    {
                        continue;
                    }

                    _treatedMd5List[chatEntry.MD5] = true;
                }
                else
                {
                    _treatedMd5List.Add(chatEntry.MD5, true);
                }
            }
            
        }
        public void Update()
        {
            if (!LokiPoe.IsInGame) return;
            if (shouldCleanMessages)
            {
                CleanMessage();
                shouldCleanMessages = false;
            }
            List<chatPanel.ChatEntry> msgs = LokiPoe.InGameState.ChatPanel.Messages;

            if (msgs.Count <= 0)
            {
                return;
            }

            string lastMd5 = msgs.Last().MD5;

            if (lastMd5 == _lastMd5)
            {
                return;
            }

            _lastMd5 = lastMd5;

            for (int i = 0; i < msgs.Count; i++)
            {
                var chatEntry = msgs[i];
                if (_treatedMd5List.TryGetValue(chatEntry.MD5, out bool alreadyTreated))
                {
                    if (alreadyTreated)
                    {
                        continue;
                    }

                    _treatedMd5List[chatEntry.MD5] = true;
                }
                else
                {
                    _treatedMd5List.Add(chatEntry.MD5, true);
                }

                try
                {
                    ProcessNewMessage(chatEntry);
                }
                catch (Exception e)
                {
                    // Suppressing all Exception without warming. This is a bad practice, under mormal circustance you want to know what went wrong.
                    //GlobalLog.Error($"{e}");
                }
            }
        }

        private void ProcessNewMessage(chatPanel.ChatEntry newmessage)
        {
            if (newmessage == null)
            {
                return;
            }

            switch (newmessage.MessageType)
            {
                case chatPanel.MessageType.Local:
                    break;
                case chatPanel.MessageType.Global:
                    break;
                case chatPanel.MessageType.Party:
                    ProcessPartyMessage(newmessage);
                    break;
                case chatPanel.MessageType.Whisper:
                    ProcessPartyMessage(newmessage);
                    break;
                case chatPanel.MessageType.Trade:
                    break;
                case chatPanel.MessageType.Guild:
                    break;
            }
        }

        private static async Task ProcessPartyMessage(LokiPoe.InGameState.ChatPanel.ChatEntry newmessage)
        {
            var start = newmessage.Message.IndexOf($"{newmessage.RemoteName}:", StringComparison.InvariantCulture) + $"{newmessage.RemoteName}:".Length + 1;
            var end = newmessage.Message.Length - start;
            var command = newmessage.Message.Substring(start, end);

            GlobalLog.Warn($"Recieved Message: {newmessage.Message}, Command: {command}");

            /*if (command == FollowBotSettings.Instance.OpenTownPortalChatCommand)
                DefenseAndFlaskTask.ShouldOpenPortal = true;*/
        }
    }
}
