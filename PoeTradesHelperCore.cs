﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.AtlasHelper;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using PoeTradesHelper.Chat;
using SharpDX;

namespace PoeTradesHelper
{
    using System.Diagnostics;
    using System.Linq;
    using ExileCore.PoEMemory.MemoryObjects;
    using ExileCore.Shared;

    public class PoeTradesHelperCore : BaseSettingsPlugin<Settings>
    {
        private const float ENTRY_HEIGHT = 65;
        private const float ENTRY_HEIGHT_MINIMIZED = 25;
        private const float EntrySpacing = 1;
        private readonly MouseClickController _mouseClickController = new MouseClickController();
        private readonly ReplyButtonsController _replyButtonsController = new ReplyButtonsController();
        private readonly AreaPlayersController _areaPlayersController = new AreaPlayersController();
        private BannedMessagesFilter _bannedMessagesFilter;

        private ChatController _chatController;
        private AtlasTexture _closeTexture;
        private AtlasTexture _entryBgTexture;
        private AtlasTexture _headerTexture;
        private AtlasTexture _iconTrade;
        private AtlasTexture _iconVisitHideout;
        private AtlasTexture _incomeTradeIcon;
        private AtlasTexture _inviteIcon;
        private AtlasTexture _kickIcon;
        private AtlasTexture _leaveIcon;
        private AtlasTexture _askInterestingIcon;
        private AtlasTexture _repeatIcon;
        private AtlasTexture _whoIsIcon;
        private MessagesController _messagesController;
        private AtlasTexture _outgoingTradeIcon;
        private StashTradeController _stashTradeController;
        private TradeLogic _tradeLogic;
        private CancellationTokenSource _cancellationTokenSource;
        private string _notificationSound;
        private bool _clipboardTradeProcessorProcessPressed;
        private string _readeProcessorPrevText;

        public override bool Initialise()
        {
            Input.RegisterKey(Keys.Control);
            Input.RegisterKey(Keys.Enter);
            Input.RegisterKey(Keys.V);
            _chatController = new ChatController(GameController, Settings);
            _messagesController = new MessagesController();
            _tradeLogic = new TradeLogic(Settings);
            _stashTradeController = new StashTradeController(GameController, Graphics);
            _replyButtonsController.Load(DirectoryFullName);
            _bannedMessagesFilter = new BannedMessagesFilter(Settings);

            _notificationSound = Path.Combine(DirectoryFullName, "Sounds", "notification.wav");
            _iconVisitHideout = GetAtlasTexture("visiteHideout");
            _iconTrade = GetAtlasTexture("trade");
            _headerTexture = GetAtlasTexture("header_bg");
            _entryBgTexture = GetAtlasTexture("entry_bg");
            _closeTexture = GetAtlasTexture("close");
            _incomeTradeIcon = GetAtlasTexture("incoming_arrow");
            _outgoingTradeIcon = GetAtlasTexture("outgoing_arrow");
            _leaveIcon = GetAtlasTexture("leave");
            _kickIcon = GetAtlasTexture("kick");
            _inviteIcon = GetAtlasTexture("invite");
            _askInterestingIcon = GetAtlasTexture("still-interesting");
            _repeatIcon = GetAtlasTexture("reload-history");
            _whoIsIcon = GetAtlasTexture("who-is");

            _chatController.MessageReceived += _messagesController.ReceiveMessage;
            _messagesController.ChatMessageReceived += _bannedMessagesFilter.FilterMessage;
            _bannedMessagesFilter.MessagePassed += _tradeLogic.OnChatMessageReceived;
            _bannedMessagesFilter.MessagePassed += _areaPlayersController.OnChatMessageReceived;

            _tradeLogic.NewTradeReceived += OnNewTradeReceived;

            _cancellationTokenSource = new CancellationTokenSource();

            var factory = new TaskFactory(_cancellationTokenSource.Token,
                                          TaskCreationOptions.LongRunning,
                                          TaskContinuationOptions.None,
                                          TaskScheduler.Default);
            factory.StartNew(UpdateThread, _cancellationTokenSource.Token);

            return base.Initialise();
        }

        #region Overrides of BaseSettingsPlugin<Settings>

        public override void OnUnload()
        {
            base.OnUnload();
            _cancellationTokenSource.Cancel();
        }

        public override void EntityAddedAny(Entity entity)
        {
            if (entity.Type != EntityType.Player)
                return;

            if (entity.Address == GameController.Player.Address)
                return;

            var player = entity.GetComponent<Player>();

            if (string.IsNullOrEmpty(player.PlayerName))
                return;
            _areaPlayersController.RegisterPlayerInArea(player.PlayerName);
        }

        public override void EntityRemoved(Entity entity)
        {
            if (entity.Type != EntityType.Player)
                return;

            if (entity.Address == GameController.Player.Address)
                return;

            var player = entity.GetComponent<Player>();

            if (string.IsNullOrEmpty(player.PlayerName))
                return;
            _areaPlayersController.UnregisterPlayerInArea(player.PlayerName);
        }

        #endregion

        private void OnNewTradeReceived()
        {
            if (Settings.PlaySound.Value)
                GameController.SoundController.PlaySound(_notificationSound);
        }

        private void UpdateThread()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                _chatController.Update();
                Task.Delay(100).Wait();


                //The same functionality as in mercury trade. Press F2, press whisper button on trade site (copy to buffer), unpress F2- it will be printed to chat
                var keyState = Input.GetKeyState(Settings.TradeCopyToChatHotkey.Value);
                if (keyState && !_clipboardTradeProcessorProcessPressed)
                {
                    _clipboardTradeProcessorProcessPressed = true;
                    _readeProcessorPrevText = ImGui.GetClipboardText();
                }
                else if (!keyState && _clipboardTradeProcessorProcessPressed)
                {
                    _clipboardTradeProcessorProcessPressed = false;
                    var tradeText = ImGui.GetClipboardText();

                    if (_readeProcessorPrevText != tradeText)
                    {
                        WinApi.SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                        Thread.Sleep(30);
                        _chatController.PrintToChat(tradeText);
                    }

                    _readeProcessorPrevText = null;
                }
            }
        }

        public override void OnPluginDestroyForHotReload()
        {
            _cancellationTokenSource.Cancel();
            base.OnPluginDestroyForHotReload();
        }

        public override void OnClose()
        {
            _cancellationTokenSource.Cancel();
            base.OnClose();
        }

        public override void Render()
        {
            if (_tradeLogic.TradeEntries.Count == 0 && Settings.HideIfNoTradeEntries)
                return;

            _mouseClickController.Update();

            ImGui.SetNextWindowPos(new System.Numerics.Vector2(Settings.PosX, Settings.PosY), ImGuiCond.Once,
                                   System.Numerics.Vector2.Zero);

            var windowSize = new System.Numerics.Vector2(Settings.EntryWidth,
                                                         _tradeLogic.TradeEntries.Count * (ENTRY_HEIGHT + EntrySpacing) + 20);

            ImGui.SetNextWindowSize(
                windowSize,
                ImGuiCond.Always);

            var rect = new RectangleF(Settings.PosX, Settings.PosY, windowSize.X, 60);

            var flags = ImGuiWindowFlags.NoScrollbar |
                        ImGuiWindowFlags.NoBackground |
                        ImGuiWindowFlags.NoBringToFrontOnFocus |
                        ImGuiWindowFlags.NoFocusOnAppearing |
                        ImGuiWindowFlags.NoSavedSettings;

            flags = Settings.Resizable ? flags : flags | ImGuiWindowFlags.NoResize;
            flags = Settings.Movable ? flags : flags | ImGuiWindowFlags.NoMove;

            if (!rect.Contains(Input.MousePosition))
                flags ^= ImGuiWindowFlags.NoMove;

            var opened = true;

            if (ImGui.Begin($"{Name}", ref opened, flags))
            {
                DrawWindowContent();

                var pos = ImGui.GetWindowPos();
                Settings.PosX = pos.X;
                Settings.PosY = pos.Y;

                var size = ImGui.GetWindowSize();
                Settings.EntryWidth = size.X;
            }

            ImGui.End();

        }

        private void DrawWindowContent()
        {
            var drawPos = new System.Numerics.Vector2(Settings.PosX, Settings.PosY + 20);

            foreach (var tradeEntry in _tradeLogic.TradeEntries)
            {
                var height = tradeEntry.Value.Minimize ? ENTRY_HEIGHT_MINIMIZED : ENTRY_HEIGHT;
                var rect = new RectangleF(drawPos.X, drawPos.Y, Settings.EntryWidth, height);

                var globalRect = rect;
                const float border = 1;
                globalRect.X -= border;
                globalRect.Y -= border;
                globalRect.Width += border * 2;
                globalRect.Height += border * 2;

                Graphics.DrawImage(_entryBgTexture, globalRect);

                var headerRect = rect;
                headerRect.Height = 25;
                DrawHeader(tradeEntry.Value, headerRect);

                if (!tradeEntry.Value.Minimize)
                {
                    var contentRect = rect;
                    contentRect.Top += 25;
                    DrawContent(tradeEntry.Value, contentRect);
                }
               
                
                Graphics.DrawFrame(rect, Settings.TradeEntryBorder.Value, 1);
                 drawPos.Y += height + EntrySpacing;
            }

            _stashTradeController.Draw(_tradeLogic.TradeEntries.Values);
        }

        private void DrawHeader(TradeEntry tradeEntry, RectangleF headerRect)
        {
            Graphics.DrawImage(_headerTexture, headerRect);

            headerRect.Y += 3;
            var minimizePos = headerRect.TopLeft.Translate(3);

            if (DrawTextButton(ref minimizePos, 18, tradeEntry.Minimize ? ">" : "v", 2,
                    tradeEntry.Minimize ? new Color(255, 211, 78) : Color.Green))
            {
                tradeEntry.Minimize = !tradeEntry.Minimize;
            }

            if (DrawImageButton(new RectangleF(headerRect.X + 15, headerRect.Y + 1, 18, 18), _whoIsIcon, 2))
            {
                _chatController.PrintToChat($"/whois {tradeEntry.PlayerNick}");
            }

            var inArea = _areaPlayersController.IsPlayerInArea(tradeEntry.PlayerNick);
            var nickPos = new Vector2(headerRect.X + 15 + 20 + 3, headerRect.Y + 1);

            var nickShort = tradeEntry.PlayerNick;

            if (nickShort.Length > 6)
            {
                nickShort = $"{nickShort.Substring(0, 6)}...";
            }

            if (DrawTextButton(ref nickPos, 18, nickShort, 0, inArea ? Color.Green : new Color(255, 211, 78)))
                _chatController.PrintToChat($"@{tradeEntry.PlayerNick} ", false);

            var currencyTextPos = headerRect.TopLeft.Translate(135+Graphics.MeasureText(nickShort).X);//headerRect.TopLeft.Translate(headerRect.Width / 2 - 5);

            var textSize = Graphics.DrawText($"{tradeEntry.CurrencyAmount} {tradeEntry.CurrencyType}",
                                             currencyTextPos,
                                             Settings.CurrencyColor.Value, FontAlign.Right);

            var rectangleF = new RectangleF(currencyTextPos.X - textSize.X - 5 - 18, currencyTextPos.Y, 18, 18);
            Graphics.DrawImage(tradeEntry.IsIncomingTrade ? _outgoingTradeIcon : _incomeTradeIcon, rectangleF);

            const float button_width = 18;
            const float buttons_spacing = 10;

            var buttonsRect = headerRect;
            buttonsRect.X += headerRect.Width - button_width - 3f;
            buttonsRect.Width = button_width;
            buttonsRect.Height = 18;

            if (DrawImageButton(buttonsRect, _closeTexture, 2))
                _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);

            buttonsRect.X -= button_width + buttons_spacing;

            if (!tradeEntry.IsIncomingTrade)
            {
                if (DrawImageButton(buttonsRect, _leaveIcon, 1))
                {
                    _chatController.PrintToChat($"/kick {GameController.Player.GetComponent<Player>().PlayerName}");
                    _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);
                }
            }
            else
            {
                if (DrawImageButton(buttonsRect, _kickIcon, 2))
                {
                    _chatController.PrintToChat($"/kick {tradeEntry.PlayerNick}");
                    _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);
                }
            }

            buttonsRect.X -= button_width + buttons_spacing;

            if (DrawImageButton(buttonsRect, _iconTrade, 1, inArea ? Color.Yellow : Color.Gray))
                _chatController.PrintToChat($"/tradewith {tradeEntry.PlayerNick}");

            buttonsRect.X -= button_width + buttons_spacing;

            if (DrawImageButton(buttonsRect, _iconVisitHideout))
                _chatController.PrintToChat($"/hideout {tradeEntry.PlayerNick}");

            if (tradeEntry.IsIncomingTrade)
            {
                buttonsRect.X -= button_width + buttons_spacing;

                if (DrawImageButton(buttonsRect, _inviteIcon))
                    _chatController.PrintToChat($"/invite {tradeEntry.PlayerNick}");

                if (!Settings.HideBanButton)
                {
                    buttonsRect.X -= button_width + buttons_spacing + 10;

                    if (DrawImageButton(buttonsRect, _closeTexture, color: Color.Red))
                    {
                        _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);

                        _bannedMessagesFilter.BanMessage(tradeEntry.Message);
                    }
                }
                
            }

            var elapsed = DateTime.Now - tradeEntry.Timestamp;

            Graphics.DrawText(Utils.TimeSpanToString(elapsed),
                buttonsRect.TopLeft.Translate(-40),
                Settings.ElapsedTimeColor.Value);
        }

        private void DrawContent(TradeEntry tradeEntry, RectangleF contentRect)
        {
            string nameText = tradeEntry.ItemName;

            if (tradeEntry.ItemAmount != "")
            {
                nameText = tradeEntry.ItemAmount + " " + tradeEntry.ItemName;
            }

            Graphics.DrawText(nameText, contentRect.TopLeft.Translate(30, 2), Color.Yellow);
            Graphics.DrawText(tradeEntry.OfferText, contentRect.TopLeft.Translate(contentRect.Width - 30, 2), Color.Red, FontAlign.Right);

            var repeatButtonRect = contentRect;
            repeatButtonRect.Y += 2;
            repeatButtonRect.X += repeatButtonRect.Width - 21;
            repeatButtonRect.Width = 18;
            repeatButtonRect.Height = 18;

            if (tradeEntry.IsIncomingTrade)
            {
                if (DrawImageButton(repeatButtonRect, _askInterestingIcon, 2))
                {
                    _chatController.PrintToChat($"@{tradeEntry.PlayerNick} Hi, are you still interested in my {tradeEntry.ItemName} for {tradeEntry.CurrencyAmount} {tradeEntry.CurrencyType}?");
                }
            }
            else
            {
                if (DrawImageButton(repeatButtonRect, _repeatIcon, 2))
                {
                    _chatController.PrintToChat($"@{tradeEntry.PlayerNick} {tradeEntry.Message}");
                }
            }

            var buttonsDrawPos = contentRect.TopLeft;
            buttonsDrawPos.Y += 19;
            buttonsDrawPos.X += 5;

            var buttons = tradeEntry.IsIncomingTrade
                ? _replyButtonsController.IncomingReplies
                : _replyButtonsController.OutgoingReplies;

            foreach (var replyButtonInfo in buttons)
            {
                if (DrawTextButton(ref buttonsDrawPos, 19, replyButtonInfo.ButtonName))
                {
                    _chatController.PrintToChat($"@{tradeEntry.PlayerNick} {replyButtonInfo.Message}");

                    if (replyButtonInfo.GoToOwnHideout)
                    {
                        _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);
                        _chatController.PrintToChat($"/kick {GameController.Player.GetComponent<Player>().PlayerName}");
                        _chatController.PrintToChat("/hideout");
                    }
                    else if (replyButtonInfo.KickLeaveParty)
                    {
                        _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);

                        if (tradeEntry.IsIncomingTrade)
                            _chatController.PrintToChat($"/kick {tradeEntry.PlayerNick}");
                        else
                            _chatController.PrintToChat($"/kick {GameController.Player.GetComponent<Player>().PlayerName}");
                    }
                    else if (replyButtonInfo.Close)
                    {
                        _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);
                    }
                }
            }
        }

        #region DrawUtils

        private bool DrawImageButton(
            RectangleF rect,
            AtlasTexture texture,
            float imageMargin = 0,
            Color? color = null)
        {
            var result = DrawButtonBase(rect);

            if (imageMargin != 0)
            {
                rect.X += imageMargin;
                rect.Y += imageMargin;
                rect.Width -= imageMargin * 2;
                rect.Height -= imageMargin * 2;
            }

            Graphics.DrawImage(texture, rect, color ?? Color.White);

            return result;
        }

        private bool DrawTextButton(
            ref Vector2 pos,
            float height,
            string text,
            float sideMargin = 5,
            Color? color = null)
        {
            var textSize = Graphics.MeasureText(text);

            var rect = new RectangleF(pos.X, pos.Y, textSize.X + sideMargin * 2, height);
            pos.X += textSize.X + sideMargin * 2 + 5;

            Graphics.DrawText(text, rect.Center, color ?? Color.White, FontAlign.Center | FontAlign.VerticalCenter);

            return DrawButtonBase(rect);
        }

        private bool DrawButtonBase(RectangleF rect)
        {
            var bgColor = Settings.ButtonBorder.Value;
            var contains = rect.Contains(Input.MousePosition);
            var wasIntended = rect.Contains(_mouseClickController.InitialMousePosition);

            if (contains)
                bgColor = new Color(198, 193, 154);

            Graphics.DrawFrame(rect, bgColor, 1);

            if (contains && _mouseClickController.MouseClick && wasIntended)
                return true;

            return false;
        }

        #endregion
    }
}
