using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
namespace PoeTradesHelper
{

    public class Settings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        public float PosX { get; set; } = 100;
        public float PosY { get; set; } = 100;
        public float EntryWidth { get; set; } = 300;

        //public ColorNode TradeEntryBg { get; set; } = new ColorNode(new Color(42, 44, 43));
        //public ColorNode TradeEntryFg { get; set; } = new ColorNode(new Color(52, 62, 61));
        public ColorNode TradeEntryBorder { get; set; } = new ColorNode(System.Drawing.Color.Gray);
        //public ColorNode NickColor { get; set; } = new ColorNode(new Color(255, 211, 78));
        public ColorNode ElapsedTimeColor { get; set; } = new ColorNode(System.Drawing.Color.Cyan);
        public ColorNode CurrencyColor { get; set; } = new ColorNode(System.Drawing.Color.Yellow);
        //public ColorNode ButtonBg { get; set; } = new ColorNode(new Color(70, 80, 79));
        public ColorNode ButtonBorder { get; set; } = new ColorNode(System.Drawing.Color.White);
        public ToggleNode RemoveDuplicatedTrades { get; set; } = new ToggleNode(true);
        //public ToggleNode PlaySound { get; set; } = new ToggleNode(true);
        public RangeNode<int> ChatScanDelay { get; set; } = new RangeNode<int>(1000, 10, 10000);
        //public RangeNode<int> BanMessageTimeMinutes { get; set; } = new RangeNode<int>(20, 1, 100);
        [Menu("Minimum time between chat messages in milliseconds")]
        public RangeNode<int> MessageCooldownMilliseconds { get; set; } = new RangeNode<int>(200, 0, 1000);
        //[Menu("Restore Clipboard after sending a message")]
        //public ToggleNode RestoreClipboard { get; set; } = new ToggleNode(true);
        [Menu("Enable resize of trade window")]
        public ToggleNode Resizable { get; set; } = new ToggleNode(true);
        [Menu("Enable move of trade window")]
        public ToggleNode Movable { get; set; } = new ToggleNode(true);
        //[Menu("Hide if no active trades")]
        //public ToggleNode HideIfNoTradeEntries { get; set; } = new ToggleNode(true);
        //[Menu("Hide ban button")]
        //public ToggleNode HideBanButton { get; set; } = new ToggleNode(true);
        public ToggleNode HighlightCell { get; set; } = new ToggleNode(false);
        public RangeNode<int> HighlightX { get; set; } = new RangeNode<int>(1, 1, 24);
        public RangeNode<int> HighlightY { get; set; } = new RangeNode<int>(1, 1, 24);
        public ToggleNode Debug { get; set; } = new ToggleNode(false);
        
    }
}