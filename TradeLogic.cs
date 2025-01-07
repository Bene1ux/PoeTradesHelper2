using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using PoeTradesHelper.Chat;

namespace PoeTradesHelper
{
    public class TradeLogic
    {
        private static int EntryUniqueIdCounter;
        private readonly Regex _buyRegexEN;
        private readonly Regex _buyRegexCompass;
        private readonly List<Regex> _buyRegexOther;
        private readonly Regex _itemPosRegex;
        private readonly Settings _settings;
        public event Action NewTradeReceived = delegate { };

        public TradeLogic(Settings settings)
        {
            _settings = settings;
            _buyRegexEN = new Regex(
                @"(I('d like| would like) to buy your|wtb) (?'ItemAmount'[\d.]+\s)?(?'ItemName'.*?) ((listed for|for my) (?'CurrencyAmount'[\d.]+) (?'CurrencyType'.*) )?in (?'LeagueName'\w+)?(?'ExtraText'.*)", RegexOptions.Compiled);
            //_buyRegexCompass = new Regex(@"WTB (?'ItemAmount'[\d.]*)\s(?'ItemName'.*?) (?>[\d.]+\.?[\d.]*)\w+ each\. Total (?'CurrencyAmount'\S+(?> \(.*\))?)?(?'ExtraText'.*)", RegexOptions.Compiled);
            _buyRegexOther = new List<Regex>()
            {
                new Regex(@"안녕하세요,", RegexOptions.Compiled),
                new Regex(@"Здравствуйте", RegexOptions.Compiled),
                new Regex(@"Olá, eu gostaria", RegexOptions.Compiled),
                new Regex(@"สวัสดี", RegexOptions.Compiled),
                new Regex(@"Hi, ich möchte"),
                new Regex(@"Bonjour, je souhaiterais", RegexOptions.Compiled),
                new Regex(@"Hola, quisiera", RegexOptions.Compiled),
                new Regex(@"やあ、", RegexOptions.Compiled),
                new Regex(@"你好",RegexOptions.Compiled),
                new Regex(@"こんにちは",RegexOptions.Compiled),
                
            };


            //\((stash tab|stash) \"(?'TabName'.*)\"\;(\sposition\:|) left (?'TabX'\d+)\, top (?'TabY'\d+)\)(?'Offer'.+|)
            _itemPosRegex =
                new Regex(
                    @"\((stash tab|stash) \""(?'TabName'.*)\""\;(\sposition\:|) left (?'TabX'\d+)\, top (?'TabY'\d+)\)(?'Offer'.+|)");
        }

        public ConcurrentDictionary<int, TradeEntry> TradeEntries { get; } =
            new ConcurrentDictionary<int, TradeEntry>();

        public void OnChatMessageReceived(ChatMessage message)
        {
            if (message.MessageType == MessageType.From || message.MessageType == MessageType.To)
            {
                var matchEN = _buyRegexEN.Match(message.Message);

                if (matchEN.Success)
                {
                    TradeMessageReceivedEN(message, matchEN);
                    return;
                }

                /*var matchCompass = _buyRegexCompass.Match(message.Message);
                if (matchCompass.Success)
                {
                    TradeMessageReceivedCompass(message, matchCompass);
                    return;
                }*/

                foreach (var buyRegex in _buyRegexOther)
                {
                    var match = buyRegex.Match(message.Message);
                    if (!match.Success)
                    {
                        continue;
                    }
                    TradeMessageReceivedOther(message, match);
                    return;
                }

            }
            else if (message.MessageType == MessageType.NotOnline)
            {
                var entryToRemove = TradeEntries
                    .LastOrDefault(x => !x.Value.IsIncomingTrade && x.Value.PlayerNick == message.Nick).Value;
                if (entryToRemove != null)
                {
                    TradeEntries.TryRemove(entryToRemove.UniqueId, out _);
                }
            }
        }

        private void TradeMessageReceivedCompass(ChatMessage message, Match match)
        {
            if (_settings.RemoveDuplicatedTrades.Value && TradeEntries.Any(x =>
               x.Value.PlayerNick == message.Nick && x.Value.Message == message.Message))
                return;

            var itemAmount = match.Groups["ItemAmount"].Value;
            var itemName = match.Groups["ItemName"].Value;
            var currencyType = string.Empty;
            var currencyAmount = match.Groups["CurrencyAmount"].Value;
            EntryUniqueIdCounter++;

            var tradeEntry = new TradeEntry(
                itemAmount,
                itemName,
                message.Nick,
                currencyType,
                currencyAmount,
                message.MessageType == MessageType.From,
                EntryUniqueIdCounter,
                message.Message);

            //var leagueName = match.Groups["LeagueName"].Value;//TODO: Check and warn if wrong league
            var extraText = match.Groups["ExtraText"];

            if (extraText.Success)
            {
                tradeEntry.OfferText = extraText.Value;
            }

            TradeEntries.TryAdd(EntryUniqueIdCounter, tradeEntry);
            NewTradeReceived();
        }

        private void TradeMessageReceivedEN(ChatMessage message, Match match)
        {
            if (_settings.RemoveDuplicatedTrades.Value && TradeEntries.Any(x =>
                x.Value.PlayerNick == message.Nick && x.Value.Message == message.Message))
                return;

            var itemAmount = match.Groups["ItemAmount"].Value;
            var itemName = match.Groups["ItemName"].Value;
            var currencyType = match.Groups["CurrencyType"].Value;
            var currencyAmount = match.Groups["CurrencyAmount"].Value;
            EntryUniqueIdCounter++;

            var tradeEntry = new TradeEntry(
                itemAmount,
                itemName,
                message.Nick,
                currencyType,
                currencyAmount,
                message.MessageType == MessageType.From,
                EntryUniqueIdCounter,
                message.Message);

            //var leagueName = match.Groups["LeagueName"].Value;//TODO: Check and warn if wrong league
            var extraText = match.Groups["ExtraText"];

            if (extraText.Success)
            {
                var itemPos = _itemPosRegex.Match(extraText.Value);

                if (itemPos.Success)
                {
                    var tab = itemPos.Groups["TabName"].Value;
                    var posX = itemPos.Groups["TabX"].Value;
                    var posY = itemPos.Groups["TabY"].Value;
                    var offer = itemPos.Groups["Offer"].Value;
                    tradeEntry.ItemPosInfo = new ItemPosInfo(tab, new Vector2(int.Parse(posX), int.Parse(posY)));
                    tradeEntry.OfferText = offer;
                }
                else
                {
                    tradeEntry.OfferText = extraText.Value;
                }
            }

            TradeEntries.TryAdd(EntryUniqueIdCounter, tradeEntry);
            NewTradeReceived();
        }

        private void TradeMessageReceivedOther(ChatMessage message, Match match)
        {
            if (_settings.RemoveDuplicatedTrades.Value && TradeEntries.Any(x =>
                    x.Value.PlayerNick == message.Nick && x.Value.Message == message.Message))
                return;

            EntryUniqueIdCounter++;

            var tradeEntry = new TradeEntry(
                "-",
                "-",
                message.Nick,
                "-",
                "-",
                message.MessageType == MessageType.From,
                EntryUniqueIdCounter,
                message.Message);

            //var leagueName = match.Groups["LeagueName"].Value;//TODO: Check and warn if wrong league
            var extraText = match.Groups["ExtraText"];

            TradeEntries.TryAdd(EntryUniqueIdCounter, tradeEntry);
            NewTradeReceived();
        }
    }

    public class TradeEntry
    {
        public TradeEntry(string itemAmount, string itemName, string playerNick, string currencyType, string currencyAmount,
            bool incomingTrade, int uniqueId, string message)
        {
            var iconText = "{{icon}}";
            ItemAmount = itemAmount;
            ItemName = itemName.Replace(iconText, string.Empty);
            PlayerNick = playerNick;
            CurrencyType = currencyType;
            CurrencyAmount = currencyAmount;
            IsIncomingTrade = incomingTrade;
            UniqueId = uniqueId;
            Message = message;
            Timestamp = DateTime.Now;
        }

        public string PlayerNick { get; }
        public string ItemAmount { get; }
        public string ItemName { get; }
        public string CurrencyType { get; }
        public string CurrencyAmount { get; }
        public bool IsIncomingTrade { get; }
        public bool GoToHideout { get; }
        public DateTime Timestamp { get; }
        public ItemPosInfo ItemPosInfo { get; set; }
        public int UniqueId { get; }
        public string Message { get; }
        public string OfferText { get; set; } = string.Empty;
        public bool Minimize { get; set; } = false;
    }

    public class ItemPosInfo
    {
        public ItemPosInfo(string tabName, Vector2 pos)
        {
            TabName = tabName;
            Pos = pos;
        }

        public string TabName { get; }
        public Vector2 Pos { get; }
    }
}