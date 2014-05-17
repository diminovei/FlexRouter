using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace FlexRouter
{
    public enum Phrases
    {
        MainFormName,
        SizeByte,
        SizeByteSigned,
        SizeTwoBytes,
        SizeTwoBytesSigned,
        SizeFourBytes,
        SizeFourBytesSigned,
        SizeFourBytesFloat,
        SizeEightBytes,
        SizeEightBytesSigned,
        SizeEightByteFloat,

        EditorPanel,
        EditorAccessDescriptor,
        EditorName,
        EditorBadSymbols,
        EditorPanelName,
        EditorVariableRelativeOffset,
        EditorVariableModule,
        EditorVariableSize,
        EditorVariableHexValue,
        EditorVariableDecValue,
        EditorDescription,
        EditorAbsoluteOffset,
        EditorConvertAbsoluteOffsetToRelative,
        EditorPowerFormula,
        EditorDigitsAfterPoint,
        EditorUsePanelPowerFormula,
        EditorOutputFormula,
        EditorRangeMinimumValue,
        EditorRangeMaximumValue,
        EditorRangeDefaultValue,
        EditorRangeUseDefaultValue,
        EditorRangeStep,
        EditorRangeGetValueFormula,
        EditorLoopRange,
        EditorHardware,
        EditorInvert,
        EditorToggleEmulator,
        EditorRepeater,
        EditorHardwareMotherboard,
        EditorHardwareModule,
        EditorHardwareControl,
        EditorRenameState,
        EditorRemoveState,
        EditorAddState,
        EditorAddVariable,
        EditorRemoveVariable,
        EditorDefaultStateColumnHeader,
        EditorAxisReset,
        EditorAxisCalibrate,
        EditorVariableIsAlreadyExists,
        EditorDependentDescriptorsList,

        EditorMessageAbsoluteOffsetIsOutOfModule,
        EditorMessageAbsoluteIsNotAhexNumber,
        EditorMessageDataIsIncorrect,
        EditorMessageRemoveVariableFromAccessDescriptor,
        EditorMessageRemoveStateFromAccessDescriptor,
        EditorMessageStateNameIsAlreadyExist,
        EditorMessageInputStateName,
        EditorMessageRemoveAccessDescriptor,
        EditorMessageRemovePanel,
        EditorMessageRemoveControlProcessor,
        EditorMessageCantRemoveNotEmptyPanel,
        EditorMessageCantRemoveVariableInUse,
        EditorMessageRemoveVariable,
        EditorAccessDescriptorTypeIsNotSuitable,
        EditorAccessDescriptorIsAlreadyInList,
        EditorSelectAnItemFirst,

        MainFormRemove,
       
        EditorTypeMemoryMultistate,
        EditorTypeMemoryIndicator,
        EditorTypeMemoryBinaryOutput,
        EditorTypeMemoryRange,
        EditorTypeRangeUnion,
        
        EditorHeaderMemoryPatch,
        EditorHeaderFsuipc,
        EditorHeaderPanel,
        EditorHeaderFakeVariable,
        EditorState,

        MessageBoxErrorHeader,
        MessageBoxWarningHeader,
        MessageBoxUnsavedEditorData,

        HardwareButton,
        HardwareEncoder,
        HardwareIndicator,
        HardwareBinaryOutput,
        HardwareAxis,
        HardwareButtonPlusMinus,
        HardwareBinaryInput,

        FormulaErrorUnexpectedSymbols,
        FormulaErrorSimilarTokensOneByOne,
        FormulaErrorTokenMustBeOperation,
        FormulaErrorTokenMustBeValue,
        FormulaErrorLastTokenCantBeOperation,
        FormulaErrorOpeningBracketNotClosed,
        FormulaErrorClosingBracketNotOpened,
        FormulaErrorMultiplyPointInNumber,
        FormulaErrorTokenPointsAbsentItem,
        FormulaErrorPointCantBeLastSymbolOfNumber,
        FormulaErrorUnknownMathOperation,
        FormulaErrorUnknownLogicOperation,
        FormulaErrorCantOperateMathAndLogicValues,
        FormulaErrorThisFormulaPartMustBeLogic,
        FormulaErrorThisFormulaPartMustBeMath,
        FormulaErrorException
    }

    static public class LanguageManager
    {
        private static Dictionary<string, string> _languageList = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> LanguagePhrases = new Dictionary<string, string>();

        static public string[] GetProfileList()
        {
            _languageList = Utils.GetXmlList("Languages", "*.lng", "FlexRouterProfile", "Language");
            return _languageList.Keys.ToArray();
        }
        
        static public void Initialize()
        {
            GetProfileList();
            foreach (Phrases phrase in Enum.GetValues(typeof(Phrases)))
                LanguagePhrases.Add(phrase.ToString(), string.Empty);
        }

        static public bool LoadLanguage(string language)
        {
            if(!_languageList.ContainsKey(language))
                return false;
            var xp = new XPathDocument(_languageList[language]);
            var nav = xp.CreateNavigator();

            var phraseNav = nav.Select("/FlexRouterProfile/Phrase");
            while(phraseNav.MoveNext())
            {
                var name = phraseNav.Current.GetAttribute("Name", phraseNav.Current.NamespaceURI);
                var value = phraseNav.Current.GetAttribute("Value", phraseNav.Current.NamespaceURI);
                if(!LanguagePhrases.ContainsKey(name))
                    continue;
                LanguagePhrases[name] = value;
            }
            return true;
        }

        static public bool SaveLanguageTemplate()
        {
            var file = Path.Combine(Utils.GetFullSubfolderPath("Languages"), "template.lng");
            var writer = new XmlTextWriter(file, Encoding.UTF8);
            writer.WriteStartDocument();
            writer.WriteStartElement("FlexRouterProfile");
            writer.WriteAttributeString("ControlCategory", "Language");
            writer.WriteAttributeString("Name", "Template");

            foreach (Phrases phrase in Enum.GetValues(typeof(Phrases)))
            {
                writer.WriteString("\n ");
                writer.WriteStartElement("Phrase");
                writer.WriteAttributeString("Name", phrase.ToString());
                writer.WriteAttributeString("Value", "");
                writer.WriteEndElement();
            }
            writer.WriteString("\n");
            writer.WriteEndElement();
            
            writer.WriteEndDocument();
            writer.Close();
            return true;
        }

        static public string GetPhrase(Phrases phraseId)
        {
            return LanguagePhrases.ContainsKey(phraseId.ToString()) ? LanguagePhrases[phraseId.ToString()] : string.Empty;
        }
    }
}
