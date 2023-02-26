using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEZUG.Features.Console
{
    internal class FezugVariable
    {
        public const string VariablesFileName = "FezugVars";

        public static List<FezugVariable> DefinedList { get; private set; } = new List<FezugVariable>();
        private static Dictionary<string, string> LoadedVariables { get; set; }

        public readonly string Name;
        public readonly string HelpText;

        private string _valueString;
        private float _valueFloat;
        private int _valueInt;
        private bool _valueBool;

        public string ValueString
        {
            get => _valueString; 
            set
            {
                if (value.Length == 0) return;
                _valueString = value;
                if (!float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _valueFloat)) _valueFloat = 0.0f;
                _valueInt = (int)_valueFloat;
                _valueBool = value == "true" || value == "on" || value == "yes" || _valueFloat != 0.0f;

                VariableChanged();
            }
        }

        public float ValueFloat { 
            get => _valueFloat; 
            set
            {
                _valueString = value.ToString();
                _valueFloat = value;
                _valueInt = (int)value;
                _valueBool = _valueInt != 0;

                VariableChanged();
            }
        }
        public int ValueInt { 
            get => _valueInt;
            set
            {
                _valueString = value.ToString();
                _valueFloat = value;
                _valueInt = value;
                _valueBool = value != 0;

                VariableChanged();
            }
        }
        public bool ValueBool { 
            get => _valueBool;
            set
            {
                _valueString = value.ToString();
                _valueFloat = value ? 1.0f : 0.0f;
                _valueInt = value ? 1 : 0;
                _valueBool = value;

                VariableChanged();
            }
        }

        public bool SaveOnChange { get; set; }
        public int Min { get; set; } = -1;
        public int Max { get; set; } = -1;
        public event Action OnChanged;

        public FezugVariable(string name, string helpText, string defaultValue = "0")
        {
            Name = name;
            HelpText = helpText;
            ValueString = defaultValue;

            DefinedList.Add(this);
            if (LoadedVariables.ContainsKey(name))
            {
                ValueString = LoadedVariables[name];
            }
        }

        private void VariableChanged()
        {
            VerifyVariable();
            OnChanged?.Invoke();
            SaveVariables();
        }

        private void VerifyVariable()
        {
            if (Min < 0 && Max < 0) return;
            if(ValueInt < Min) ValueInt = Min;
            if(ValueInt > Max) ValueInt = Max;
        }

        static FezugVariable()
        {
            LoadVariables();
        }

        public static FezugVariable Get(string name)
        {
            return DefinedList.Find(c => c.Name == name);
        }

        private static string GetVariablesFilePath()
        {
            return Path.Combine(Util.LocalConfigFolder, VariablesFileName);
        }

        private static void SaveVariables()
        {
            using (StreamWriter varsFile = new StreamWriter(GetVariablesFilePath()))
            {
                foreach (var command in DefinedList)
                {
                    if (!command.SaveOnChange) continue;
                    varsFile.WriteLine($"{command.Name} {command.ValueString}");
                }
            }
        }

        private static void LoadVariables()
        {
            if (LoadedVariables != null) return;
            LoadedVariables = new Dictionary<string, string>();

            var varsFilePath = GetVariablesFilePath();
            if (!File.Exists(varsFilePath)) return;
            var varsFileLines = File.ReadAllLines(varsFilePath);
            foreach (var line in varsFileLines)
            {
                string[] tokens = line.Split(new char[] { ' ' }, 2);
                if (tokens.Length < 2) continue;

                LoadedVariables[tokens[0]] = tokens[1];
            }
        }
    }
}
