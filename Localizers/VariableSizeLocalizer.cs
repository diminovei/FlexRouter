using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using FlexRouter.VariableWorkerLayer;

namespace FlexRouter.Localizers
{
    static class VariableSizeLocalizer
    {
        private static readonly Dictionary<MemoryVariableSize, string> Sizes = new Dictionary<MemoryVariableSize, string>();        // Список размеров переменной для комбобокса
        private static readonly Dictionary<MemoryVariableSize, string> SizesOld = new Dictionary<MemoryVariableSize, string>();        // Список размеров переменной для комбобокса

        static public void Initialize()
        {
            PrepareToLocalize();
        }
        static private MemoryVariableSize? OldSizeTypeByText(string text)
        {
            foreach (var size in SizesOld)
            {
                if (size.Value != text)
                    continue;
                return size.Key;
            }
            return null;
        }
        static public MemoryVariableSize SizeTypeByText(string text)
        {
            return (from size in Sizes where size.Value == text select size.Key).FirstOrDefault();
        }

        static public void LocalizeSizes(ref ComboBox control)
        {
            var controlText = control.Text;
            control.Items.Clear();
            foreach (var size in Sizes)
                control.Items.Add(size.Value);

            control.Text = LocalizeSizeString(controlText);
        }

        static public string LocalizeSizeString(string text)
        {
//            if (string.IsNullOrEmpty(text))
//                return string.Empty;

            var currentSizeKey = OldSizeTypeByText(text);
            if (currentSizeKey == null)
                return string.Empty;
            return SizeBySizeType((MemoryVariableSize)currentSizeKey);
        }

        static public void PrepareToLocalize()
        {
            SizesOld.Clear();
            foreach (var size in Sizes)
                SizesOld.Add(size.Key, size.Value);
            // Локализуем список размеров переменной
            Sizes.Clear();
            Sizes.Add(MemoryVariableSize.Byte, LanguageManager.GetPhrase(Phrases.SizeByte));
            Sizes.Add(MemoryVariableSize.ByteSigned, LanguageManager.GetPhrase(Phrases.SizeByteSigned));
            Sizes.Add(MemoryVariableSize.TwoBytes, LanguageManager.GetPhrase(Phrases.SizeTwoBytes));
            Sizes.Add(MemoryVariableSize.TwoBytesSigned, LanguageManager.GetPhrase(Phrases.SizeTwoBytesSigned));
            Sizes.Add(MemoryVariableSize.FourBytes, LanguageManager.GetPhrase(Phrases.SizeFourBytes));
            Sizes.Add(MemoryVariableSize.FourBytesSigned, LanguageManager.GetPhrase(Phrases.SizeFourBytesSigned));
            Sizes.Add(MemoryVariableSize.FourBytesFloat, LanguageManager.GetPhrase(Phrases.SizeFourBytesFloat));
            Sizes.Add(MemoryVariableSize.EightBytes, LanguageManager.GetPhrase(Phrases.SizeEightBytes));
            Sizes.Add(MemoryVariableSize.EightBytesSigned, LanguageManager.GetPhrase(Phrases.SizeEightBytesSigned));
            Sizes.Add(MemoryVariableSize.EightByteFloat, LanguageManager.GetPhrase(Phrases.SizeEightByteFloat));
        }
        static public string SizeBySizeType(MemoryVariableSize size)
        {
            return Sizes.ContainsKey(size) ? Sizes[size] : null;
        }
    }
}
