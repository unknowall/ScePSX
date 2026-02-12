using System;
using System.Collections.Generic;
using Avalonia.Input;
using static ScePSX.Controller;

namespace ScePSX.UI
{
    public class KeyMange
    {
        public static KeyMappingManager KMM1 = new KeyMappingManager();
        public static KeyMappingManager KMM2 = new KeyMappingManager();

        IniFile ini;

        public KeyMange(IniFile ini)
        {
            this.ini = ini;

            InitKeyMap();
        }

        public void SaveKeyMap()
        {
            ini.WriteDictionary("Player1Key", KMM1._keyMapping);
            ini.WriteDictionary("Player2Key", KMM2._keyMapping);
            //ini.WriteDictionary("JoyKeyMap", AnalogMap);
        }

        public void InitKeyMap()
        {
            try
            {
                KMM1._keyMapping = ini.ReadDictionary<Key, InputAction>("Player1Key");
                KMM2._keyMapping = ini.ReadDictionary<Key, InputAction>("Player2Key");
            } catch { }

            if (KMM1._keyMapping.Count == 0)
            {
                KMM1.SetKeyMapping(Key.D2, InputAction.Select);
                KMM1.SetKeyMapping(Key.D1, InputAction.Start);
                KMM1.SetKeyMapping(Key.W, InputAction.DPadUp);
                KMM1.SetKeyMapping(Key.D, InputAction.DPadRight);
                KMM1.SetKeyMapping(Key.S, InputAction.DPadDown);
                KMM1.SetKeyMapping(Key.A, InputAction.DPadLeft);
                KMM1.SetKeyMapping(Key.R, InputAction.L2);
                KMM1.SetKeyMapping(Key.T, InputAction.R2);
                KMM1.SetKeyMapping(Key.Q, InputAction.L1);
                KMM1.SetKeyMapping(Key.E, InputAction.R1);
                KMM1.SetKeyMapping(Key.J, InputAction.Triangle);
                KMM1.SetKeyMapping(Key.K, InputAction.Circle);
                KMM1.SetKeyMapping(Key.I, InputAction.Cross);
                KMM1.SetKeyMapping(Key.U, InputAction.Square);

            }

            if (KMM2._keyMapping.Count == 0)
            {
                KMM2.SetKeyMapping(Key.D2, InputAction.Select);
                KMM2.SetKeyMapping(Key.D1, InputAction.Start);
                KMM2.SetKeyMapping(Key.W, InputAction.DPadUp);
                KMM2.SetKeyMapping(Key.D, InputAction.DPadRight);
                KMM2.SetKeyMapping(Key.S, InputAction.DPadDown);
                KMM2.SetKeyMapping(Key.A, InputAction.DPadLeft);
                KMM2.SetKeyMapping(Key.R, InputAction.L2);
                KMM2.SetKeyMapping(Key.T, InputAction.R2);
                KMM2.SetKeyMapping(Key.Q, InputAction.L1);
                KMM2.SetKeyMapping(Key.E, InputAction.R1);
                KMM2.SetKeyMapping(Key.J, InputAction.Triangle);
                KMM2.SetKeyMapping(Key.K, InputAction.Circle);
                KMM2.SetKeyMapping(Key.I, InputAction.Cross);
                KMM2.SetKeyMapping(Key.U, InputAction.Square);
            }

            ini.WriteDictionary<Key, InputAction>("Player1Key", KMM1._keyMapping);
            ini.WriteDictionary<Key, InputAction>("Player2Key", KMM2._keyMapping);
        }

    }

    public class KeyMappingManager
    {
        public Dictionary<Key, InputAction> _keyMapping = new();

        public void SetKeyMapping(Key key, InputAction button)
        {
            _keyMapping.Remove(GetKeyCode(button));
            _keyMapping.Remove(key);

            _keyMapping[key] = button;
        }

        public InputAction GetKeyButton(Key key)
        {
            if (_keyMapping.TryGetValue(key, out var button))
            {
                return button;
            }
            return (InputAction)0xFF;
            //throw new KeyNotFoundException($"No mapping found for key: {key}");
        }

        public string GetKeyName(Key key)
        {
            if (_keyMapping.TryGetValue(key, out var button))
            {
                return button.ToString();
            }
            //throw new KeyNotFoundException($"No mapping found for key: {key}");
            return "";
        }

        public Key GetKeyCode(InputAction button)
        {
            foreach (var mapping in _keyMapping)
            {
                if (mapping.Value == button)
                {
                    return mapping.Key;
                }
            }
            return Key.None;
        }

        public void ClearKeyMapping(Key key)
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
