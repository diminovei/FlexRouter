﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using FlexRouter.AccessDescriptors;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.EditorsUI.Dialogues;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Localizers;
using FlexRouter.ProfileItems;

namespace FlexRouter.EditorsUI.AccessDescriptorsEditor
{
    /// <summary>
    /// Interaction logic for DescriptorFormulaEditor.xaml
    /// </summary>
    public partial class RangeUnionEditor : IEditor
    {
        private readonly RangeUnion _assignedAccessDescriptor;
        public RangeUnionEditor(RangeUnion assignedAccessDescriptor)
        {
            InitializeComponent();
            Localize();
            _assignedAccessDescriptor = assignedAccessDescriptor;
            var savedList = _assignedAccessDescriptor.GetDependentDescriptorsList();
            foreach (var s in savedList)
                AddDescriptorToList(s);
        }

        private void AddDescriptorToList(DescriptorBase accessDescriptor)
        {
            var lbi = new ListBoxItem { Content = Profile.PanelStorage.GetPanelById(accessDescriptor.GetAssignedPanelId()).Name + "." + accessDescriptor.GetName(), Tag = accessDescriptor };
            _dependendDescriptorList.Items.Add(lbi);
        }

        public void Save()
        {
            _assignedAccessDescriptor.ClearDependentDescriptorList();
            foreach (ListBoxItem item in _dependendDescriptorList.Items)
                _assignedAccessDescriptor.AddDependentDescriptor((DescriptorRange)item.Tag);
        }

        public void Localize()
        {
            _dependendDescriptorListLabel.Content = LanguageManager.GetPhrase(Phrases.EditorDependentDescriptorsList);
        }

        public bool IsDataChanged()
        {
            if (_assignedAccessDescriptor.GetDependentDescriptorsList().Length != _dependendDescriptorList.Items.Count)
                return true;
            var savedList = _assignedAccessDescriptor.GetDependentDescriptorsList();
            return savedList.Select(sl => _dependendDescriptorList.Items.Cast<object>().Any(item => ((DescriptorRange) ((ListBoxItem) item).Tag).GetId() == sl.GetId())).Any(found => !found);
        }
        /// <summary>
        /// Корректно ли заполнены поля
        /// </summary>
        /// <returns>string.Empty или null, если корректно, иначе текст ошибок</returns>
        public EditorFieldsErrors IsCorrectData()
        {
            return new EditorFieldsErrors(null);
        }

        private void AddDescriptorClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var d = new Selector(SelectedType.AccessDescriptor);
            d.ShowDialog();
            var selectedAdId = d.GetSelectedItemId();
            if (selectedAdId == Guid.Empty)
                return;
            var ad = Profile.AccessDescriptor.GetAccessDesciptorById(selectedAdId);
            if (!(ad is DescriptorRange))
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorAccessDescriptorTypeIsNotSuitable);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            if (_dependendDescriptorList.Items.Cast<ListBoxItem>().Any(item => ((DescriptorBase) item.Tag).GetId() == ad.GetId()))
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorAccessDescriptorIsAlreadyInList);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            if (Profile.ControlProcessor.GetControlProcessor(ad.GetId()) != null)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorDependentAssignmentWasRemoved);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxWarningHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Information);
                Profile.ControlProcessor.RemoveControlProcessor(ad.GetId());
            }
            AddDescriptorToList(ad);
        }

        private void RemoveDescriptorClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_dependendDescriptorList.SelectedItems.Count == 0)
            {
                var message = LanguageManager.GetPhrase(Phrases.EditorSelectAnItemFirst);
                var header = LanguageManager.GetPhrase(Phrases.MessageBoxErrorHeader);
                MessageBox.Show(message, header, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
            _dependendDescriptorList.Items.Remove(_dependendDescriptorList.SelectedItems[0]);
        }
    }
}
