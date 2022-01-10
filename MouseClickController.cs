using System.Windows.Forms;
using ExileCore;
using SharpDX;

namespace PoeTradesHelper
{
    public class MouseClickController
    {
        private bool _mouseDown;
        public bool MouseClick { get; private set; }

        public Vector2 InitialMousePosition { get; private set; }

        public MouseClickController()
        {
            Input.RegisterKey(Keys.LButton);
        }

        public void Update()
        {
            MouseClick = false;
            if (Input.GetKeyState(Keys.LButton))
            {
                if (!_mouseDown)
                {
                    InitialMousePosition = Input.MousePosition;
                }
                _mouseDown = true;
            }
            else
            {
                if (_mouseDown)
                {
                    _mouseDown = false;
                    MouseClick = true;
                }
            }
        }
    }
}