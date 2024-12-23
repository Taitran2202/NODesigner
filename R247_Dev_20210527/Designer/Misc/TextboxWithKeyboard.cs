using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace NOVisionDesigner.Designer.Misc
{
    [TemplatePart(Name = ElementVirtualKeyboard, Type = typeof(Button))]
    public class TextboxWithKeyboard : TextBox
    {
        private const string ElementVirtualKeyboard = "PART_Keyboard";
        private Button _virtual_keyboard;
        private Popup _pop_up;

        public static DependencyProperty IsKeyboardOpenProperty =
DependencyProperty.Register("IsKeyboardOpen", typeof(bool), typeof(TextboxWithKeyboard), new PropertyMetadata(false));
        public bool IsKeyboardOpen
        {
            get
            {
                return (this.GetValue(IsKeyboardOpenProperty) as bool?) == true;
            }
            set
            {
                this.SetValue(IsKeyboardOpenProperty, value);
            }
        }



        public static DependencyProperty IsPasswordProperty =
DependencyProperty.Register("IsPassword", typeof(bool), typeof(TextboxWithKeyboard), new PropertyMetadata(false));
        public bool IsPassword
        {
            get
            {
                return (this.GetValue(IsPasswordProperty) as bool?) == true;
            }
            set
            {
                this.SetValue(IsPasswordProperty, value);
            }
        }

        private void OnVirtualKeyboardPress()
        {

            Keyboards.VirtualFullKeyboardWindow kb = new Keyboards.VirtualFullKeyboardWindow(Text,IsPassword);

            var result = kb.ShowDialog();
            // kb.Focus();
            if (result == true)
            {
                Text = kb.result;
            }
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _virtual_keyboard = GetTemplateChild(ElementVirtualKeyboard) as Button;
            if (
               _virtual_keyboard == null)
            {
                return;
                throw new InvalidOperationException(string.Format("You have missed to specify {0} in your template", ElementVirtualKeyboard));
            }

            _virtual_keyboard.Click += (o, e) => OnVirtualKeyboardPress();
            _pop_up = GetTemplateChild("popup") as Popup;
            
            
            // base.OnApplyTemplate(); 
        }
    }
}
