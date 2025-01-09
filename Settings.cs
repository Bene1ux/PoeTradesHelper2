using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
namespace PoeTradesHelper
{

    public class Settings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        public RangeNode<float> PosX { get; set; } = new(100, 0, 2500);
        public RangeNode<float> PosY { get; set; }= new(100,0,1500);
        public RangeNode<float> EntryWidth { get; set; }= new(300,100,1000);
        public ColorNode TradeEntryBorder { get; set; } = new ColorNode(System.Drawing.Color.Gray);
        public ColorNode ElapsedTimeColor { get; set; } = new ColorNode(System.Drawing.Color.Cyan);
        public ColorNode CurrencyColor { get; set; } = new ColorNode(System.Drawing.Color.Yellow);
        public ColorNode ButtonBorder { get; set; } = new ColorNode(System.Drawing.Color.White);
        public ToggleNode RemoveDuplicatedTrades { get; set; } = new ToggleNode(true);
        //public ToggleNode PlaySound { get; set; } = new ToggleNode(true);
        public RangeNode<int> ChatScanDelay { get; set; } = new RangeNode<int>(1000, 10, 10000);
        [Menu("Delay between chat messages")]
        public RangeNode<int> MessageCooldownMilliseconds { get; set; } = new RangeNode<int>(200, 0, 1000);
        [Menu("Enable resize of trade window")]
        public ToggleNode Resizable { get; set; } = new ToggleNode(true);
        [Menu("Enable move of trade window")]
        public ToggleNode Movable { get; set; } = new ToggleNode(true);
        public ToggleNode HighlightCell { get; set; } = new ToggleNode(false);
        public RangeNode<int> HighlightX { get; set; } = new RangeNode<int>(1, 1, 24);
        public RangeNode<int> HighlightY { get; set; } = new RangeNode<int>(1, 1, 24);
        public ToggleNode Debug { get; set; } = new ToggleNode(false);
        
    }
}