using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ExileCore;
using ExileCore.Shared;
using ImGuiNET;
using WindowsInput;
using WindowsInput.Native;

namespace PoeTradesHelper.Chat
{
    public class ChatController
    {
        //private const string LOG_PATH =
        //    @"C:\HomeProjects\Games\_PoE\HUD\PoEHelper\Plugins\Compiled\PoeTradesHelper\chatLog.txt";

        private readonly GameController _gameController;
        private long _lastMessageAddress;
        private readonly Stopwatch _updateSw = Stopwatch.StartNew();
        public event Action<string> MessageReceived = delegate { };
        private readonly Settings _settings;

        public ChatController(GameController gameController, Settings settings)
        {
            _gameController = gameController;
            _settings = settings;
            //File.Delete(LOG_PATH);
            ScanChat(true);
        }

        public void Update()
        {
            if (_updateSw.ElapsedMilliseconds > _settings.ChatScanDelay.Value)
            {
                _updateSw.Restart();
                ScanChat(false);
            }
        }

        private void ScanChat(bool firstScan)
        {
            var messageElements = _gameController.Game.IngameState.IngameUi.ChatBoxRoot.MessageBox.Children.ToList();

            var msgQueue = new Queue<string>();
            for (var i = messageElements.Count - 1; i >= 0; i--)
            {
                var messageElement = messageElements[i];

                if (messageElement.Address == _lastMessageAddress)
                    break;

                if (!messageElement.IsVisibleLocal)
                    continue;

                var text = messageElement.LongText;
                msgQueue.Enqueue(text);

                //try
                //{
                //    File.AppendAllText(LOG_PATH, $"{text}{Environment.NewLine}");
                //}
                //catch
                //{
                //    //ignored
                //}
            }


            _lastMessageAddress = messageElements.LastOrDefault()?.Address ?? 0;

            if (firstScan)
                return;

            while (msgQueue.Count > 0)
            {
                try
                {
                    MessageReceived(msgQueue.Dequeue());
                }
                catch (Exception e)
                {
                    DebugWindow.LogError($"Error processing chat message. Error: {e.Message}", 5);
                }
            }
        }

        public void PrintToChat(string message, bool send = true)
        {
            if (!_gameController.Window.IsForeground())
            {
                WinApi.SetForegroundWindow(_gameController.Window.Process.MainWindowHandle);
            }

            var chatBoxRoot = _gameController.Game.IngameState.IngameUi.ChatBoxRoot;
            var simulator = new InputSimulator();
            if (!chatBoxRoot.IsVisible)
            {
                simulator.Keyboard.KeyDown(VirtualKeyCode.RETURN);
                simulator.Keyboard.KeyUp(VirtualKeyCode.RETURN);
            }

            var oldClipboardText = ImGui.GetClipboardText();
            if (string.IsNullOrEmpty(message))
            {
                ImGui.SetClipboardText(message);
            }
            simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            if (send)
            {
                simulator.Keyboard.KeyDown(VirtualKeyCode.RETURN);
                simulator.Keyboard.KeyUp(VirtualKeyCode.RETURN);
            }

            Thread.Sleep(_settings.MessageCooldownMilliseconds);
            if (_settings.RestoreClipboard)
            {
                ImGui.SetClipboardText(oldClipboardText);
            }

            //WinApi.SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
            //WinApi.SetForegroundWindow(_gameController.Window.Process.MainWindowHandle);
        }
    }
}