using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore2;
using ExileCore2.PoEMemory.Elements;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared;
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
        private string _lastMessageAddress;
        private readonly Stopwatch _updateSw = Stopwatch.StartNew();
        public event Action<string> MessageReceived = delegate { };
        private readonly Settings _settings;
        private bool firstScan = true;

        public ChatController(GameController gameController, Settings settings)
        {
            _gameController = gameController;
            _settings = settings;
            //File.Delete(LOG_PATH);
            ScanChat();
        }

        public void Update()
        {
            if (_updateSw.ElapsedMilliseconds > _settings.ChatScanDelay.Value)
            {
                /*if (_settings.Debug)
                {
                    DebugWindow.LogMsg($"[PoeTradesHelper] Scanning chat");
                }*/
                _updateSw.Restart();
                ScanChat();
            }
        }

        private void ScanChat()
        {
            //var messageElements = _gameController?.Game?.IngameState?.IngameUi?.ChatBoxRoot?.MessageBox?.Children?.ToList();

            //var msgs = _gameController?.Game?.IngameState?.IngameUi?.ChatPanel?.Children[1]?.Children[2]?.Children[1]?.Children;
            //if(msgs is null)
            //{
            //    DebugWindow.LogMsg("No chat msg thingy");
            //}

            var messageElements = _gameController?.Game?.IngameState?.IngameUi?.ChatMessages?.ToList();
            //var messageElements = _gameController?.Game?.IngameState?.IngameUi?.ChatPanel?.Children[1]?.Children[2]?.Children[1]?.Children.ToList();

            if (messageElements == null)
            {
                return;
            }

            if (_settings.Debug)
            {
                DebugWindow.LogMsg($"[PoeTradesHelper] Messages: {messageElements.Count}, last msg scanned: {_lastMessageAddress}");
            }

            var msgQueue = new Queue<string>();
            for (var i = messageElements.Count - 1; i >= 0; i--)
            {
                //var messageElement = messageElements[i];
                var messageElement = messageElements[i];//.Text;

                if (messageElement.Equals(_lastMessageAddress))
                    break;

                //var text = NativeStringReader.ReadStringLong(messageElement.Address + 0x378, messageElement.M);
                msgQueue.Enqueue(messageElement);

                //try
                //{
                //    File.AppendAllText(LOG_PATH, $"{text}{Environment.NewLine}");
                //}
                //catch
                //{
                //    //ignored
                //}
            }
            if (_settings.Debug)
            {
                DebugWindow.LogMsg($"[PoeTradesHelper] New Messages: {msgQueue.Count}");
            }

            //_lastMessageAddress = messageElements.LastOrDefault();
            _lastMessageAddress = messageElements.LastOrDefault();//?.Text;

            if (firstScan)
            {
                firstScan = false;
                return;
            }

            while (msgQueue.Count > 0)
            {
                if (_settings.Debug)
                {
                    DebugWindow.LogMsg($"[PoeTradesHelperCore] Msg received: {msgQueue.Peek()}");
                }
                
                try
                {
                    MessageReceived(msgQueue.Dequeue());
                }
                catch (Exception e)
                {
                    DebugWindow.LogError($"[PoeTradesHelperCore] Error processing chat message. Error: {e.Message}", 5);
                }
            }
        }

        public void PrintToChat(string message, bool send = true)
        {
            if (!_gameController.Window.IsForeground())
            {
                WinApi.SetForegroundWindow(_gameController.Window.Process.MainWindowHandle);
            }

            var chatBoxRoot = _gameController.Game.IngameState.IngameUi.ChatTitlePanel;
           var simulator = new InputSimulator();
           if (!chatBoxRoot.IsVisible)
           {
               simulator.Keyboard.KeyDown(VirtualKeyCode.RETURN);
               simulator.Keyboard.KeyUp(VirtualKeyCode.RETURN);
           }

            //var oldClipboardText = Clipboard.GetText();//ImGui.GetClipboardText();
            if (!string.IsNullOrEmpty(message))
            {
                ImGui.SetClipboardText(message);
            }
           
            simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            if (send)
            {
                simulator.Keyboard.KeyDown(VirtualKeyCode.RETURN);
                simulator.Keyboard.KeyUp(VirtualKeyCode.RETURN);
            }

            Thread.Sleep(_settings.MessageCooldownMilliseconds.Value);
            /*if (_settings.RestoreClipboard)
            {
                ImGui.SetClipboardText(oldClipboardText);
            }*/
        }
    }
}