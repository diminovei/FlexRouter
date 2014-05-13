﻿using FlexRouter.AccessDescriptors.Helpers;

namespace FlexRouter.EditorsUI.AccessDescriptorsEditor
{
    /// <summary>
    /// Interaction logic for DescriptorValueEditor.xaml
    /// </summary>
    partial class RepeaterEditor : IEditor
    {
        private readonly DescriptorMultistateBase _accessDescriptor;
        public RepeaterEditor(DescriptorMultistateBase accessDescriptor)
        {
            _accessDescriptor = accessDescriptor;
            InitializeComponent();
            ShowData();
            Localize();
        }
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        private void ShowData()
        {
//            _repeater.IsChecked = ((DescriptorMultistateBase)_accessDescriptor).IsRepeaterOn();
        }
        public void Save()
        {
//            (_accessDescriptor).EnableRepeater(_repeater.IsChecked == true);
            ShowData();
        }

        public void Localize()
        {
            ShowData();
            _repeaterLabel.Content = LanguageManager.GetPhrase(Phrases.EditorRepeater);
        }

        public bool IsDataChanged()
        {
            return false;
        }

        public EditorFieldsErrors IsCorrectData()
        {
            return new EditorFieldsErrors(null);
        }
    }
}
