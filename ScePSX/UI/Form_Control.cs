using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

using static ScePSX.Controller;
using static SDL2.SDL;

#pragma warning disable SYSLIB0011

namespace ScePSX
{
    public partial class FrmInput : Form
    {
        public static KeyMappingManager KMM1 = new KeyMappingManager();
        public static KeyMappingManager KMM2 = new KeyMappingManager();
        //public static JoyMappingManager JMM = new JoyMappingManager();

        public static Dictionary<SDL_GameControllerButton, InputAction> AnalogMap;

        private InputAction SetKey;
        private Button Btn;

        public FrmInput()
        {
            InitializeComponent();

            InitKeyMap();

            InitControllerMap();

            if (SDL_NumJoysticks() > 0)
            {
                cbmode.Items.Add(SDL_JoystickNameForIndex(0));
            }

            cbcon.SelectedIndex = 0;
            cbmode.SelectedIndex = 0;

            //cbcon.Enabled = false;
            cbmode.Enabled = false;

            this.KeyUp += FrmInput_KeyUp;
        }

        public static void InitKeyMap()
        {
            KmmStore KMM;

            try
            {
                KMM = LoadKeys<KmmStore>("./Keys.cfg");
                KMM1 = KMM.KMM1;
                KMM2 = KMM.KMM2;
            } catch { }

            if (KMM1._keyMapping.Count == 0)
            {
                KMM1.SetKeyMapping(Keys.D2, InputAction.Select);
                KMM1.SetKeyMapping(Keys.D1, InputAction.Start);
                KMM1.SetKeyMapping(Keys.W, InputAction.DPadUp);
                KMM1.SetKeyMapping(Keys.D, InputAction.DPadRight);
                KMM1.SetKeyMapping(Keys.S, InputAction.DPadDown);
                KMM1.SetKeyMapping(Keys.A, InputAction.DPadLeft);
                KMM1.SetKeyMapping(Keys.R, InputAction.L2);
                KMM1.SetKeyMapping(Keys.T, InputAction.R2);
                KMM1.SetKeyMapping(Keys.Q, InputAction.L1);
                KMM1.SetKeyMapping(Keys.E, InputAction.R1);
                KMM1.SetKeyMapping(Keys.J, InputAction.Triangle);
                KMM1.SetKeyMapping(Keys.K, InputAction.Circle);
                KMM1.SetKeyMapping(Keys.I, InputAction.Cross);
                KMM1.SetKeyMapping(Keys.U, InputAction.Square);

            }

            if (KMM2._keyMapping.Count == 0)
            {
                KMM2.SetKeyMapping(Keys.D2, InputAction.Select);
                KMM2.SetKeyMapping(Keys.D1, InputAction.Start);
                KMM2.SetKeyMapping(Keys.W, InputAction.DPadUp);
                KMM2.SetKeyMapping(Keys.D, InputAction.DPadRight);
                KMM2.SetKeyMapping(Keys.S, InputAction.DPadDown);
                KMM2.SetKeyMapping(Keys.A, InputAction.DPadLeft);
                KMM2.SetKeyMapping(Keys.R, InputAction.L2);
                KMM2.SetKeyMapping(Keys.T, InputAction.R2);
                KMM2.SetKeyMapping(Keys.Q, InputAction.L1);
                KMM2.SetKeyMapping(Keys.E, InputAction.R1);
                KMM2.SetKeyMapping(Keys.J, InputAction.Triangle);
                KMM2.SetKeyMapping(Keys.K, InputAction.Circle);
                KMM2.SetKeyMapping(Keys.I, InputAction.Cross);
                KMM2.SetKeyMapping(Keys.U, InputAction.Square);
            }

            KMM = new KmmStore(KMM1, KMM2);
            SaveKeys<KmmStore>(KMM, "./Keys.cfg");
        }

        public static void InitControllerMap()
        {
            AnalogMap = new()
            {
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A, InputAction.Circle },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B, InputAction.Cross },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X, InputAction.Triangle },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y, InputAction.Square },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK, InputAction.Select },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START, InputAction.Start },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER, InputAction.L1 },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER, InputAction.R1 },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP, InputAction.DPadUp },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN, InputAction.DPadDown },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT, InputAction.DPadLeft },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT, InputAction.DPadRight }
            };
        }

        private static KmmStore LoadKeys<KmmStore>(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (KmmStore)formatter.Deserialize(fs);
            }
        }

        private static void SaveKeys<KmmStore>(KmmStore obj, string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, obj);
            }
        }

        private void FrmInput_Shown(object sender, EventArgs e)
        {

        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            KmmStore KMM = new KmmStore(KMM1, KMM2);
            SaveKeys<KmmStore>(KMM, "./Keys.cfg");
        }

        private void FrmInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                plwait.Visible = false;
                if (cbmode.SelectedIndex == 1)
                {
                    //SdlQuit = true;
                }
                return;
            }

            if (!plwait.Visible || cbmode.SelectedIndex == 1)
                return;

            Btn.Text = e.KeyCode.ToString();

            if (cbcon.SelectedIndex == 0)
                KMM1.SetKeyMapping(e.KeyCode, SetKey);
            else
                KMM2.SetKeyMapping(e.KeyCode, SetKey);

            plwait.Visible = false;
        }

        private void ReadyGetKey(object sender, InputAction val)
        {
            plwait.Visible = true;
            Btn = (Button)sender;
            SetKey = val;
            if (cbmode.SelectedIndex == 1)
            {
                // todo
            }
        }

        private void L2_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.L2);
        }

        private void L1_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.L1);
        }

        private void R2_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.R2);
        }

        private void R1_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.R1);
        }

        private void U_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.DPadUp);
        }

        private void L_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.DPadLeft);
        }

        private void D_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.DPadDown);
        }

        private void R_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.DPadRight);
        }

        private void TRI_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.Triangle);
        }

        private void SQUAD_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.Square);
        }

        private void O_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.Circle);
        }

        private void X_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.Cross);
        }

        private void SELE_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.Select);
        }

        private void START_Click(object sender, EventArgs e)
        {
            ReadyGetKey(sender, InputAction.Start);
        }

        private void cbcon_SelectedIndexChanged(object sender, EventArgs e)
        {
            KeyMappingManager kmm;

            if (cbcon.SelectedIndex == 0)
            {
                kmm = KMM1;
            } else
            {
                kmm = KMM2;
            }

            foreach (var mapping in kmm._keyMapping)
            {
                if (mapping.Value == InputAction.Start)
                    START.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.Select)
                    SELE.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.DPadUp)
                    U.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.DPadDown)
                    D.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.DPadLeft)
                    L.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.DPadRight)
                    R.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.L1)
                    L1.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.R1)
                    R1.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.L2)
                    L2.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.R2)
                    R2.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.Cross)
                    X.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.Circle)
                    O.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.Triangle)
                    TRI.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();

                if (mapping.Value == InputAction.Square)
                    SQUAD.Text = kmm.GetKeyCode(mapping.Value).ToString().ToUpper();
            }

        }

    }

    [Serializable]
    public class KmmStore
    {
        public KeyMappingManager KMM1;
        public KeyMappingManager KMM2;

        public KmmStore(KeyMappingManager k1, KeyMappingManager k2)
        {
            KMM1 = k1;
            KMM2 = k2;
        }
    }

    [Serializable]
    public class KeyMappingManager
    {
        public Dictionary<Keys, InputAction> _keyMapping = new();

        public void SetKeyMapping(Keys key, InputAction button)
        {
            _keyMapping.Remove(GetKeyCode(button));
            _keyMapping.Remove(key);

            _keyMapping[key] = button;
        }

        public InputAction GetKeyButton(Keys key)
        {
            if (_keyMapping.TryGetValue(key, out var button))
            {
                return button;
            }
            return (InputAction)0xFF;
            //throw new KeyNotFoundException($"No mapping found for key: {key}");
        }

        public string GetKeyName(Keys key)
        {
            if (_keyMapping.TryGetValue(key, out var button))
            {
                return button.ToString();
            }
            //throw new KeyNotFoundException($"No mapping found for key: {key}");
            return "";
        }

        public Keys GetKeyCode(InputAction button)
        {
            foreach (var mapping in _keyMapping)
            {
                if (mapping.Value == button)
                {
                    return mapping.Key;
                }
            }
            return Keys.None;
        }

        public void ClearKeyMapping(Keys key)
        {
            _keyMapping.Remove(key);
        }

        public void ClearAllMappings()
        {
            _keyMapping.Clear();
        }

        public void PrintMappings()
        {
            Console.WriteLine("Current Key Mappings:");
            foreach (var mapping in _keyMapping)
            {
                Console.WriteLine($"{mapping.Key} -> {mapping.Value}");
            }
        }

    }

}
