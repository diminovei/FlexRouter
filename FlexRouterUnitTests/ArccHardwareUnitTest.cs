using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using FlexRouter.Hardware;
using FlexRouter.Hardware.Arcc;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlexRouterUnitTests
{
    [TestClass]
    public class ArccHardwareUnitTest
    {
        /// <summary>
        /// Проверка преобразования исходящего события установки текста на индикаторе в пакет байт для передачи железу
        /// </summary>
        [TestMethod]
        public void ArccCheckIndicatorEventWithFullText()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);

            // Коды для текста. Код - значение
            // 00 - "0"
            // 01 - "1"
            // 02 - "2"
            // 03 - "3"
            // 04 - "4"
            // 05 - "5"
            // 06 - "6"
            // 07 - "7"
            // 08 - "8"
            // 09 - "9"
            // 10 - "0."
            // 11 - "1."
            // 12 - "2."
            // 13 - "3."
            // 14 - "4."
            // 15 - "5."
            // 16 - "6."
            // 17 - "7."
            // 18 - "8."
            // 19 - "9."
            // 20 - " "
            // 21 - "F"
            // 22 - "D"
            // 23 - "C"
            // 24 - "-" - зажжённый средний сегмент
            // 25 - "_" - зажжённый нижний сегмент
            // 26 - "¯" - зажжённый верхний сегмент
            // 27 - "I"
            // 28 - "r"
            // 29 - "o"
            // 30 - "."
            // 31 - "E"

            // Длина пакета модуля индикаторов - 10 байт
            // Текст Error_8. (точка - не отдельный символ, а часть символа '8.')
            var testOutputDataForIndicatorModule = new byte[]
            {
                5, // 5 - признак группы плат индикаторов
                3, // ID платы
                1, // команда. 1 - вывести текст
                31, // "E" (текст) 
                28, // "r" (текст)
                28, // "r" (текст)
                29, // "o" (текст)
                28, // "r" (текст)
                25, // "_" (текст)
                18 // "8." (текст)
            };
            var ev = new IndicatorEvent
            {
                Hardware = new ControlProcessorHardware
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Indicator,
                    ModuleId = 3,
                    BlockId = 0,
                    ControlId = 0
                },
                IndicatorText = "Error_8." // Максимальная длина текста - 7 символов
            };
            var byteArray = arccProcessor.ConvertEventToByteArrayForHardware(ev);
            Assert.IsTrue(byteArray.SequenceEqual(testOutputDataForIndicatorModule));
        }
        /// <summary>
        /// Проверка преобразования исходящего события установки текста на индикаторе в пакет байт для передачи железу
        /// Максимальное число сегментов на индикаторе - 7. Если длина текста меньше 7 символов, то текст равняется по левому сегменту индикатора
        /// То есть, если устанавливаем текст из двух символов - "56", то на трёхсегментном индикаторе будет текст " 56"
        /// </summary>
        [TestMethod]
        public void ArccCheckIndicatorEventWithCuttedText()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);
            // Коды для текста. Код - значение
            // 00 - "0"
            // 01 - "1"
            // 02 - "2"
            // 03 - "3"
            // 04 - "4"
            // 05 - "5"
            // 06 - "6"
            // 07 - "7"
            // 08 - "8"
            // 09 - "9"
            // 10 - "0."
            // 11 - "1."
            // 12 - "2."
            // 13 - "3."
            // 14 - "4."
            // 15 - "5."
            // 16 - "6."
            // 17 - "7."
            // 18 - "8."
            // 19 - "9."
            // 20 - " "
            // 21 - "F"
            // 22 - "D"
            // 23 - "C"
            // 24 - "-" - зажжённый средний сегмент
            // 25 - "_" - зажжённый нижний сегмент
            // 26 - "¯" - зажжённый верхний сегмент
            // 27 - "I"
            // 28 - "r"
            // 29 - "o"
            // 30 - "."
            // 31 - "E"

            // Длина пакета модуля индикаторов - 10 байт
            // Текст для вывода "56"
            var testOutputDataForIndicatorModule = new byte[]
            {
                5, // 5 - признак группы плат индикаторов
                3, // ID платы
                1, // команда. 1 - вывести текст
                20, // " " (текст)
                20, // " " (текст)
                20, // " " (текст)
                20, // " " (текст)
                20, // " " (текст)
                5, // "5" (текст)
                6 // "6" (текст)
            };
            var ev = new IndicatorEvent
            {
                Hardware = new ControlProcessorHardware
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Indicator,
                    ModuleId = 3,
                    BlockId = 0,
                    ControlId = 0
                },
                IndicatorText = "56" // Максимальная длина текста - 7 символов
            };
            var byteArray = arccProcessor.ConvertEventToByteArrayForHardware(ev);
            Assert.IsTrue(byteArray.SequenceEqual(testOutputDataForIndicatorModule));
        }
        /// <summary>
        /// Проверка преобразования исходящего события установки состояния бинарного вывода в пакет байт для передачи железу
        /// </summary>
        [TestMethod]
        public void ArccCheckLampEvent()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);

            // Длина пакета модуля индикаторов - 5 байт
            var testDataFoBinaryOutputModule = new byte[]
            {
                4, // 4 - признак группы плат бинарного вывода
                2, // ID платы
                1, // команда. 1 - установить состояние выхода
                16, // номер линии 1-29
                1 // состояние. 1 - вкл, 0 - выкл
            };
            var ev = new LampEvent
            {
                Hardware = new ControlProcessorHardware
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.BinaryOutput,
                    ModuleId = 2,
                    BlockId = 0,
                    ControlId = 16
                },
                IsOn = true // Лампа включена
            };
            var byteArray = arccProcessor.ConvertEventToByteArrayForHardware(ev);
            Assert.IsTrue(byteArray.SequenceEqual(testDataFoBinaryOutputModule));
        }
        /// <summary>
        /// В самом первом пакете иногда приходит, а иногда не приходит 1 байт со значением 0.
        /// </summary>
        [TestMethod]
        public void ArccCheckProcessInputDataStartedWithZero()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);

            // Длина пакета модуля индикаторов - 18 байт
            var keyEventByteArray = new byte[]
            {
                2, // 2 - признак группы плат "клавиатурный модуль"
                2, // ID платы
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                // произвольные 13 байт, не занятые передачей полезных данных в пакете
                0, // признак дампа 0 - клавиша нажата в модуле, 1 - начат дамп, 2 - окончен дамп
                100, // номер кнопки 1-168
                1 // состояние. 1 - вкл, 0 - выкл
            };
            var keyEventStruct = new ButtonEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Button,
                    ModuleId = 2,
                    BlockId = 0,
                    ControlId = 100
                },
                IsPressed = true
            };

            // Отправляем массив данных события клавиатуры и часть события энкодера
            // В результате получаем событие клавиатуры
            var arrayToTransfer = new byte[keyEventByteArray.Length + 1];
            arrayToTransfer[0] = 0;
            keyEventByteArray.CopyTo(arrayToTransfer, 1);
            
            var events = arccProcessor.ProcessDataFromSerialPort(arrayToTransfer).ToList();
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events[0] is ButtonEvent);
            var resultEvent = events[0] as ButtonEvent;
            Assert.AreEqual(resultEvent.IsPressed, keyEventStruct.IsPressed);
            Assert.AreEqual(resultEvent.Hardware.GetHardwareGuid(), keyEventStruct.Hardware.GetHardwareGuid());
        }
        /// <summary>
        /// Проверяем ситуацию: 
        ///     при первой передече приходит событие клавиатуры и часть события энкодера
        ///     при второй передече приходят остатки события энкодера
        /// </summary>
        [TestMethod]
        public void ArccCheckProcessEventSentInTwoPackets()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);

            // Длина пакета модуля клавиатуры - 18 байт
            var keyEventByteArray = new byte[]
            {
                2, // 2 - признак группы плат "клавиатурный модуль"
                2, // ID платы
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                // произвольные 13 байт, не занятые передачей полезных данных в пакете
                0, // признак дампа 0 - клавиша нажата в модуле, 1 - начат дамп, 2 - окончен дамп
                100, // номер кнопки 1-168
                1 // состояние. 1 - вкл, 0 - выкл
            };
            var keyEventStruct = new ButtonEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Button,
                    ModuleId = 2,
                    BlockId = 0,
                    ControlId = 100
                },
                IsPressed = true
            };

            // Длина пакета модуля энкодеров - 18 байт
            var encoderEventByteArray = new byte[]
            {
                3, // 3 - признак группы плат "модуль энкодеров"
                2, // ID платы
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                // произвольные 13 байт, не занятые передачей полезных данных в пакете
                4, // номер энкодера 1-14
                100, // количество щелчков
                1 // направление. 0 - влево, 1 - вправо
            };
            var encoderEventStruct = new EncoderEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Encoder,
                    ModuleId = 2,
                    BlockId = 0,
                    ControlId = 4
                },
                ClicksCount = 100,
                RotateDirection = true
            };

            var trashByteArray = new byte[] { 1, 4, 55, 3 };

            const int encoderArrayPartToSendFirstTime = 16;

            // Отправляем массив данных события клавиатуры и часть события энкодера
            // В результате получаем событие клавиатуры
            var arrayToTransfer = new byte[keyEventByteArray.Length + encoderArrayPartToSendFirstTime];
            keyEventByteArray.CopyTo(arrayToTransfer, 0);
            Array.Copy(encoderEventByteArray, 0, arrayToTransfer, keyEventByteArray.Length, 16);

            var events = arccProcessor.ProcessDataFromSerialPort(arrayToTransfer).ToList();
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events[0] is ButtonEvent);
            var resultEvent = events[0] as ButtonEvent;
            Assert.AreEqual(resultEvent.IsPressed, keyEventStruct.IsPressed);
            Assert.AreEqual(resultEvent.Hardware.GetHardwareGuid(), keyEventStruct.Hardware.GetHardwareGuid());

            // Досылаем данные от события энкодера
            // Получаем событие энкодера
            arrayToTransfer = new byte[encoderEventByteArray.Length - encoderArrayPartToSendFirstTime + trashByteArray.Length];
            Array.Copy(encoderEventByteArray, encoderArrayPartToSendFirstTime, arrayToTransfer, 0, encoderEventByteArray.Length - encoderArrayPartToSendFirstTime);
            Array.Copy(trashByteArray, 0, arrayToTransfer, encoderEventByteArray.Length - encoderArrayPartToSendFirstTime, trashByteArray.Length);

            events = arccProcessor.ProcessDataFromSerialPort(arrayToTransfer).ToList();
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events[0] is EncoderEvent);
            var encoderResultEvent = events[0] as EncoderEvent;
            Assert.AreEqual(encoderResultEvent.ClicksCount, encoderEventStruct.ClicksCount);
            Assert.AreEqual(encoderResultEvent.RotateDirection, encoderEventStruct.RotateDirection);
            Assert.AreEqual(encoderResultEvent.Hardware.GetHardwareGuid(), encoderResultEvent.Hardware.GetHardwareGuid());
        }
        /// <summary>
        /// Тестирование обработки пакетов от модуля аналоговых осей
        /// </summary>
        [TestMethod]
        public void ArccCheckAxisDataProcessing()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);

            const int axis1Value = 300;
            const int axis4Value = 830;
            const int axis8Value = 2;
            // Длина пакета модуля индикаторов - 18 байт
            var axisEventByteArray = new byte[]
            {
                1, // 2 - признак группы плат "модуль аналоговых осей"
                4, // ID платы
                axis8Value/256, axis8Value%256, // Позиция оси №8
                0, 0, // Позиция оси №7
                0, 0, // Позиция оси №6
                0, 0, // Позиция оси №5
                axis4Value/256, axis4Value%256, // Позиция оси №4
                0, 0, // Позиция оси №3
                0, 0, // Позиция оси №2
                axis1Value/256, axis1Value%256 // Позиция оси №1
            };
            var axis8EventStruct = new AxisEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Axis,
                    ModuleId = 4,
                    BlockId = 0,
                    ControlId = 8
                },
                MaximumValue = 1023,
                MinimumValue = 0,
                Position = axis8Value
            };
            var axis4EventStruct = new AxisEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Axis,
                    ModuleId = 4,
                    BlockId = 0,
                    ControlId = 4
                },
                MaximumValue = 1023,
                MinimumValue = 0,
                Position = axis4Value
            };
            var axis1EventStruct = new AxisEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Axis,
                    ModuleId = 4,
                    BlockId = 0,
                    ControlId = 1
                },
                MaximumValue = 1023,
                MinimumValue = 0,
                Position = axis1Value
            };
            var eventsToTest = new List<AxisEvent> {axis1EventStruct, axis4EventStruct, axis8EventStruct};

            var eventsFromProcessor = arccProcessor.ProcessDataFromSerialPort(axisEventByteArray).ToList();
            Assert.AreEqual(3, eventsFromProcessor.Count);

            for (var i = 0; i < eventsFromProcessor.Count; i++)
            {
                Assert.AreEqual(eventsFromProcessor[i].Hardware.GetHardwareGuid(), eventsToTest[i].Hardware.GetHardwareGuid());
                Assert.AreEqual(((AxisEvent)eventsFromProcessor[i]).Position, eventsToTest[i].Position);
                Assert.AreEqual(((AxisEvent)eventsFromProcessor[i]).MaximumValue, eventsToTest[i].MaximumValue);
                Assert.AreEqual(((AxisEvent)eventsFromProcessor[i]).MinimumValue, eventsToTest[i].MinimumValue);
            }
        }
        /// <summary>
        /// Тестирование обработки пакетов от модуля клавиатуры
        /// </summary>
        [TestMethod]
        public void ArccCheckKeyDataProcessing()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);

            // Длина пакета модуля клавиатуры - 18 байт
            var keyEventByteArray = new byte[]
            {
                2, // 2 - признак группы плат "клавиатурный модуль"
                2, // ID платы
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                // произвольные 13 байт, не занятые передачей полезных данных в пакете
                0, // признак дампа 0 - клавиша нажата в модуле, 1 - начат дамп, 2 - окончен дамп
                100, // номер кнопки 1-168
                1 // состояние. 1 - вкл, 0 - выкл
            };
            var keyEventStruct = new ButtonEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Button,
                    ModuleId = 2,
                    BlockId = 0,
                    ControlId = 100
                },
                IsPressed = true
            };
            var events = arccProcessor.ProcessDataFromSerialPort(keyEventByteArray).ToList();
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events[0] is ButtonEvent);
            var resultEvent = events[0] as ButtonEvent;
            Assert.AreEqual(resultEvent.IsPressed, keyEventStruct.IsPressed);
            Assert.AreEqual(resultEvent.Hardware.GetHardwareGuid(), keyEventStruct.Hardware.GetHardwareGuid());
        }
        /// <summary>
        /// Тестирование обработки пакетов от модуля энкодеров
        /// </summary>
        [TestMethod]
        public void ArccCheckEncoderDataProcessing()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);

            // Длина пакета модуля энкодеров - 18 байт
            var encoderEventByteArray = new byte[]
            {
                3, // 3 - признак группы плат "модуль энкодеров"
                2, // ID платы
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                // произвольные 13 байт, не занятые передачей полезных данных в пакете
                4, // номер энкодера 1-14
                100, // количество щелчков
                1 // направление. 0 - влево, 1 - вправо
            };
            var encoderEventStruct = new EncoderEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Encoder,
                    ModuleId = 2,
                    BlockId = 0,
                    ControlId = 4
                },
                ClicksCount = 100,
                RotateDirection = true
            };
            var events = arccProcessor.ProcessDataFromSerialPort(encoderEventByteArray).ToList();
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events[0] is EncoderEvent);
            var resultEvent = events[0] as EncoderEvent;
            Assert.AreEqual(resultEvent.ClicksCount, encoderEventStruct.ClicksCount);
            Assert.AreEqual(resultEvent.RotateDirection, encoderEventStruct.RotateDirection);
            Assert.AreEqual(resultEvent.Hardware.GetHardwareGuid(), encoderEventStruct.Hardware.GetHardwareGuid());
        }
        /// <summary>
        /// Тестирование обработки пакетов от модуля аналоговых осей
        /// </summary>
        [TestMethod]
        public void ArccCheckBinaryInputDataProcessing()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);

            // Длина пакета модуля бинарного ввода - 18 байт
            var emptyBinaryInputEventByteArray = new byte[]
            {
                8, // 8 - признак группы плат "модуль бинарного ввода"
                7, // ID платы
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // произвольные 12 байт, не занятые передачей полезных данных в пакете
                0, // 10 00 01 00 В этом байт используются только последние 4 бита, первый - лишний, взведён только ради проверки
                0, // 01 00 00 00
                0, // 00 00 10 00
                0 // 00 00 00 01
            };
            var emptyKeyEventStruct = new ButtonEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Button,
                    ModuleId = 7 + ArccHardwareDataProcessor.IncreaseModuleIdForBinaryInput,
                    BlockId = 0,
                    ControlId = 1
                },
                IsPressed = false
            };

            // Длина пакета модуля бинарного ввода - 18 байт
            var binaryInputEventByteArray = new byte[]
            {
                8, // 8 - признак группы плат "модуль бинарного ввода"
                7, // ID платы
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // произвольные 12 байт, не занятые передачей полезных данных в пакете
                132, // 10 00 01 00 В этом байт используются только последние 4 бита, первый - лишний, взведён только ради проверки
                64, // 01 00 00 00
                8, // 00 00 10 00
                1 // 00 00 00 01
            };
            var key1EventStruct = new ButtonEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Button,
                    ModuleId = 7 + ArccHardwareDataProcessor.IncreaseModuleIdForBinaryInput,
                    BlockId = 0,
                    ControlId = 1
                },
                IsPressed = true
            };
            var key12EventStruct = new ButtonEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Button,
                    ModuleId = 7 + ArccHardwareDataProcessor.IncreaseModuleIdForBinaryInput,
                    BlockId = 0,
                    ControlId = 12
                },
                IsPressed = true
            };
            var key23EventStruct = new ButtonEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Button,
                    ModuleId = 7 + ArccHardwareDataProcessor.IncreaseModuleIdForBinaryInput,
                    BlockId = 0,
                    ControlId = 23
                },
                IsPressed = true
            };
            var key27EventStruct = new ButtonEvent
            {
                Hardware =
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Button,
                    ModuleId = 7 + ArccHardwareDataProcessor.IncreaseModuleIdForBinaryInput,
                    BlockId = 0,
                    ControlId = 27
                },
                IsPressed = true
            };
            var eventsToTest = new List<ButtonEvent> { key1EventStruct, key12EventStruct, key23EventStruct, key27EventStruct };

            // При первом вызове вернутся 28 событий от всех кнопок. Все должны быть отжаты
            var eventsFromProcessor = arccProcessor.ProcessDataFromSerialPort(emptyBinaryInputEventByteArray).ToList();
            Assert.AreEqual(28, eventsFromProcessor.Count);
            for (var i = 0; i < eventsFromProcessor.Count; i++)
            {
                emptyKeyEventStruct.Hardware.ControlId = (uint) (i + 1);
                Assert.AreEqual(eventsFromProcessor[i].Hardware.GetHardwareGuid(), emptyKeyEventStruct.Hardware.GetHardwareGuid());
                Assert.IsFalse(((ButtonEvent)eventsFromProcessor[i]).IsPressed);
            }
            
            eventsFromProcessor = arccProcessor.ProcessDataFromSerialPort(binaryInputEventByteArray).ToList();
            Assert.AreEqual(4, eventsFromProcessor.Count);

            for (var i = 0; i < eventsFromProcessor.Count; i++)
            {
                Assert.AreEqual(eventsFromProcessor[i].Hardware.GetHardwareGuid(), eventsToTest[i].Hardware.GetHardwareGuid());
                Assert.AreEqual(((ButtonEvent)eventsFromProcessor[i]).IsPressed, eventsToTest[i].IsPressed);
            }

            // "Выключаем" одну кнопку и ожидаем прихода только этого события
            binaryInputEventByteArray[17] = 0;

            eventsFromProcessor = arccProcessor.ProcessDataFromSerialPort(binaryInputEventByteArray).ToList();
            Assert.AreEqual(1, eventsFromProcessor.Count);
            Assert.AreEqual(eventsFromProcessor[0].Hardware.GetHardwareGuid(), key1EventStruct.Hardware.GetHardwareGuid());
            Assert.IsFalse(((ButtonEvent)eventsFromProcessor[0]).IsPressed);
        }
        /// <summary>
        /// Проверка преобразования исходящего события дамп кнопок в пакет байт для передачи железу
        /// </summary>
        [TestMethod]
        public void ArccCheckButtonsDumpEvent()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);

            // Длина пакета команды модулю кнопок - 5 байт
            var testDumpButtonsCommandForArccHardware = new byte[]
            {
                (byte)ArccModuleType.Button,  // 2 - признак группы плат кнопочный ввод
                2,  // ID платы
                (byte)ArccButtonCommand.DumpAllKeys,  // команда. 3 - дамп всех клавиш
                0,  // произвольные данные
                0   // произвольные данные
            };
            var dumpEvent = new DumpEvent
            { 
                Hardware = new ControlProcessorHardware
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Button,
                    ModuleId = 2,
                    BlockId = 0,
                    ControlId = 100
                } 
            };
            var byteArray = arccProcessor.ConvertEventToByteArrayForHardware(dumpEvent);
            Assert.IsTrue(byteArray.SequenceEqual(testDumpButtonsCommandForArccHardware));
        }
        /// <summary>
        /// Проверка преобразования исходящего события дамп кнопок модуля бинарного ввода в пакет байт для передачи железу
        /// </summary>
        [TestMethod]
        public void ArccCheckBinaryInputDumpEvent()
        {
            const string testMotherboardId = "TestMotherboard";
            var arccProcessor = new ArccHardwareDataProcessor(testMotherboardId);

            // Длина пакета команды модулю кнопок - 5 байт
            var testDumpButtonsCommandForArccHardware = new byte[]
            {
                (byte)ArccModuleType.BinaryInput,  // 2 - признак группы плат кнопочный ввод
                2,  // ID платы
                (byte)ArccBinaryInputCommand.DumpAllLines,  // команда. 2 - дамп всех клавиш
                0,  // произвольные данные
                0   // произвольные данные
            };
            var dumpEvent = new DumpEvent
            { 
                Hardware = new ControlProcessorHardware
                {
                    MotherBoardId = testMotherboardId,
                    ModuleType = HardwareModuleType.Button,
                    ModuleId = 2 + ArccHardwareDataProcessor.IncreaseModuleIdForBinaryInput,
                    BlockId = 0,
                    ControlId = 100
                } 
            };
            var byteArray = arccProcessor.ConvertEventToByteArrayForHardware(dumpEvent);
            Assert.IsTrue(byteArray.SequenceEqual(testDumpButtonsCommandForArccHardware));
        }
    }
}
