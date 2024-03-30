using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SupermarketSimulatorCheatMenu
{
    internal enum ModOptionType
    {
        Button,
        Toggle,
        Label,
        HorizontalSlider,
    }

    internal interface IModOption
    {
        string Name { get; }
        string Description { get; }
        string Key { get; }
        ModOptionType ModOptionType { get; }
        Rect Position { get; set; }

        void DrawOption();
        Action<object> OptionInteraction { get; set; }
        object Value { get; set; }
    }

    internal class ButtonOption : IModOption
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Key { get; private set; }
        public Rect Position { get; set; }
        public ModOptionType ModOptionType => ModOptionType.Button;
        public Action<object> OptionInteraction { get; set; }
        public object Value { get; set; }
        public ButtonOption(string a_key, string a_name, string a_description, Rect a_position, bool a_ogValue)
        {
            Key = a_key;
            Name = a_name;
            Description = a_description;
            Position = a_position;
            Value = a_ogValue;
        }

        public void DrawOption()
        {
            if (GUI.Button(Position, new GUIContent(Name, Description)))
            {
                Value = !(bool)Value;
                OptionInteraction?.Invoke(Value);
            }
        }
    }
    internal class ToggleOption : IModOption
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Key { get; private set; }
        public Rect Position { get; set; }
        public ModOptionType ModOptionType => ModOptionType.Button;
        public Action<object> OptionInteraction { get; set; }
        public object Value { get; set; }
        private bool valueCache;
        public ToggleOption(string a_key, string a_name, string a_description, Rect a_position, bool a_ogValue)
        {
            Key = a_key;
            Name = a_name;
            Description = a_description;
            Position = a_position;
            Value = a_ogValue;
        }

        public void DrawOption()
        {
            bool newValue = GUI.Toggle(Position, valueCache, new GUIContent(Name, Description));
            if (newValue != valueCache)
            {
                OptionInteraction?.Invoke(newValue);
                valueCache = newValue;
            }
        }
    }
}
