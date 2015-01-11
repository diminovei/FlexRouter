using System;
using System.Collections.Generic;
using FlexRouter.AccessDescriptors.Helpers;
using FlexRouter.ControlProcessors;
using FlexRouter.ControlProcessors.Helpers;
using FlexRouter.EditorsUI.Helpers;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using FlexRouter.Localizers;

namespace FlexRouter.EditorsUI.ControlProcessorEditors
{
    /// <summary>
    /// Interaction logic for DescriptorValueEditor.xaml
    /// </summary>
    partial class AxisSetLimitsEditor : IEditor, IControlProcessorEditor
    {
        private readonly IControlProcessor _assignedControlProcessor;
        private int _axisMinimum;
        private int _axisMaximum;
        private bool _initializationModeOn;

        public AxisSetLimitsEditor(IControlProcessor processor)
        {
            InitializeComponent();
            _assignedControlProcessor = processor;
            _axisMinimum = ((AxisRangeProcessor)_assignedControlProcessor).GetAxisMinimum();
            _axisMaximum = ((AxisRangeProcessor)_assignedControlProcessor).GetAxisMaximum();
            _axisCurrentPosition.Minimum = AxisDefaultRange.GetAxisDefaultMinimum();
            _axisCurrentPosition.Maximum = AxisDefaultRange.GetAxisDefaultMaximum();
            Localize();
        }
        /// <summary>
        /// Заполнить форму данными из описателя доступа
        /// </summary>
        private void ShowData()
        {
            _limits.Text = _axisMinimum + "-" + _axisMaximum;
        }
        public void Save()
        {
            ((AxisRangeProcessor) _assignedControlProcessor).SetAxisRangeMinimum(_axisMinimum);
            ((AxisRangeProcessor)_assignedControlProcessor).SetAxisRangeMaximum(_axisMaximum);
            ShowData();
        }

        public void Localize()
        {
            ShowData();
            _resetLimits.Content = LanguageManager.GetPhrase(Phrases.EditorAxisReset);
            _calibrate.Content = LanguageManager.GetPhrase(Phrases.EditorAxisCalibrate);
            _limitsLabel.Content = LanguageManager.GetPhrase(Phrases.EditorAxisLimitsLabel);
        }

        public bool IsDataChanged()
        {
            return ((AxisRangeProcessor)_assignedControlProcessor).GetAxisMinimum() != _axisMinimum || ((AxisRangeProcessor)_assignedControlProcessor).GetAxisMaximum()!=_axisMaximum;
        }

        public EditorFieldsErrors IsCorrectData()
        {
            return new EditorFieldsErrors(null);
        }

        AxisDebouncer _axisDebouncer = new AxisDebouncer();
        /// <summary>
        /// Функция обрабатывает нажатие кнопки или кручение энкодера
        /// Для того, чтобы корректно обрабатывать галетники, функция реагирует преимущестенно на состояние "включено"
        /// </summary>
        /// <param name="controlEvent">Структура, описывающая произошедшее событие</param>
        public void OnNewControlEvent(ControlEventBase controlEvent)
        {
            var ev = controlEvent as AxisEvent;
            if (ev == null)
                return;
            var axisControlProcessor = _assignedControlProcessor as AxisRangeProcessor;
            if (axisControlProcessor == null)
                return;

            if (!_axisDebouncer.IsNeedToProcessAxisEvent(ev, 10))
                return;

            // Приводим 0-1023 arcc к 0-1000 роутера
            var position = axisControlProcessor.CalculateRelativeAxisPosition(AxisDefaultRange.GetAxisDefaultMinimum(), AxisDefaultRange.GetAxisDefaultMaximum(), controlEvent);
            if (_initializationModeOn)
            {
                if (position < _axisMinimum)
                {
                    _axisMinimum = position;
                    ShowData();
                }
                if (position > _axisMaximum)
                {
                    _axisMaximum = position;
                    ShowData();
                }
            }
            else
            {
                if (position < _axisMinimum)
                    position = _axisMinimum;
                if (position > _axisMaximum)
                    position = _axisMaximum;
            }
            _axisCurrentPosition.Value = position;
        }

        private void CalibrateChecked(object sender, System.Windows.RoutedEventArgs e)
        {
            _initializationModeOn = true;
            _axisMinimum = (int)_axisCurrentPosition.Value;
            _axisMaximum = (int)_axisCurrentPosition.Value;
            ShowData();
        }

        private void CalibrateUnchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            _initializationModeOn = false;
        }

        private void ResetLimitsClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_initializationModeOn)
            {
                _axisMinimum = (int)_axisCurrentPosition.Value;
                _axisMaximum = (int)_axisCurrentPosition.Value;
            }
            else
            {
                _axisMaximum = AxisDefaultRange.GetAxisDefaultMaximum();
                _axisMinimum = AxisDefaultRange.GetAxisDefaultMinimum();
            }
            ShowData();
        }
    }
}
