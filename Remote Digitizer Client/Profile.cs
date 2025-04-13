using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remote_Digitizer_Client
{
    class Profile
    {
        public class InputMap
        {
            public InputMapSelection.StylusInputType InputType { get; set; }

            public string Input { get; set; }

            public InputMap(InputMapSelection.StylusInputType inputType = InputMapSelection.StylusInputType.Nothing, string input = "")
            {
                InputType = inputType;
                Input = input;
            }
        }

        public string Name { get; set; } = "Profile";

        public InputMap EnteredNormally { get; set;} = new();

        public InputMap EnteredAlternate { get; set;} = new();

        public InputMap EnteredInverted { get; set;} = new();

        public InputMap Touch { get; set; }  = new();

        public override int GetHashCode() => Name.GetHashCode();
    }
}
