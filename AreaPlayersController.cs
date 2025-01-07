﻿using System.Collections.Generic;
using ExileCore2;
using PoeTradesHelper.Chat;

namespace PoeTradesHelper
{
    public class AreaPlayersController
    {
        private readonly HashSet<string> _playersInArea = new HashSet<string>();

        public void OnChatMessageReceived(ChatMessage message)
        {
            if (message.MessageType == MessageType.LeftArea)
            {
                _playersInArea.Remove(message.Nick);
            }
            else if (message.MessageType == MessageType.JoinArea)
            {
                if (!_playersInArea.Contains(message.Nick))
                {
                     _playersInArea.Add(message.Nick);
                     //DebugWindow.LogMsg($"Player {message.Nick} in area.");
                }
                   
            }
        }

        public bool IsPlayerInArea(string nick)
        {
            return _playersInArea.Contains(nick);
        }

        public void RegisterPlayerInArea(string nick)
        {
            if (!_playersInArea.Contains(nick))
            {
                _playersInArea.Add(nick);
                //DebugWindow.LogMsg($"Player {nick} in area");
            }
                
        }

        public void UnregisterPlayerInArea(string nick)
        {
            _playersInArea.Remove(nick);
        }

        public void Clear()
        {
            _playersInArea.Clear();
        }
    }
}
