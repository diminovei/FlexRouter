using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace FlexRouter.Localizers
{
    enum ControlProcessorType
    {
        Button,
        Encoder,
        ButtonPlusMinus,
        Indicator,
        BinaryOutput,
        BinaryInput,
//        Axis,
    }

    static class ControlProcessorListLocalizer
    {
        private static readonly Dictionary<ControlProcessorType, string> ListCurrent = new Dictionary<ControlProcessorType, string>();
        private static readonly Dictionary<ControlProcessorType, string> ListOld = new Dictionary<ControlProcessorType, string>();
        static private ControlProcessorType? GetOldTypeByText(string text)
        {
            foreach (var cp in ListOld)
            {
                if (cp.Value != text)
                    continue;
                return cp.Key;
            }
            return null;
        }

        static public void Initialize()
        {
            PrepareToLocalize();
        }
        static public void PrepareToLocalize()
        {
            ListOld.Clear();
            foreach (var cp in ListCurrent)
                ListOld.Add(cp.Key, cp.Value);
            // Локализуем список размеров переменной
            ListCurrent.Clear();
            ListCurrent.Add(ControlProcessorType.Button, LanguageManager.GetPhrase(Phrases.HardwareButton));
            ListCurrent.Add(ControlProcessorType.ButtonPlusMinus, LanguageManager.GetPhrase(Phrases.HardwareButtonPlusMinus));
            ListCurrent.Add(ControlProcessorType.Encoder, LanguageManager.GetPhrase(Phrases.HardwareEncoder));
            ListCurrent.Add(ControlProcessorType.Indicator, LanguageManager.GetPhrase(Phrases.HardwareIndicator));
            ListCurrent.Add(ControlProcessorType.BinaryOutput, LanguageManager.GetPhrase(Phrases.HardwareBinaryOutput));
            ListCurrent.Add(ControlProcessorType.BinaryInput, LanguageManager.GetPhrase(Phrases.HardwareBinaryInput));
        }

        static public ControlProcessorType GetTypeByText(string text)
        {
            return (from cp in ListCurrent where cp.Value == text select cp.Key).FirstOrDefault();
        }
        static public string GetTextByType(ControlProcessorType cpType)
        {
            return ListCurrent.ContainsKey(cpType) ? ListCurrent[cpType] : null;
        }

        static public void LocalizeCombobox(ref ComboBox control)
        {
            control.Items.Clear();
            foreach (var cp in ListCurrent)
                control.Items.Add(cp.Value);

            control.Text = LocalizeTextAfterLanguageChange(control.Text);
        }
        static public string LocalizeTextAfterLanguageChange(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var currentKey = GetOldTypeByText(text);
            if (currentKey == null)
                return string.Empty;
            return GetTextByType((ControlProcessorType)currentKey);
        }

    }
}
