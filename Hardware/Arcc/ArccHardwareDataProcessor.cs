using System;
using System.Collections.Generic;
using FlexRouter.Hardware.HardwareEvents;
using FlexRouter.Hardware.Helpers;

namespace FlexRouter.Hardware.Arcc
{
    public class ArccHardwareDataProcessor
    {
        public ArccHardwareDataProcessor(string motherboardId)
        {
            _isFirstPacket = true;
            _motherboardId = motherboardId;
        }
        /// <summary>
        /// id материнской платы
        /// </summary>
        private readonly string _motherboardId;
        /// <summary>
        /// Для того, чтобы не вводить новую сущность и логику, BinaryInput воспринимается, как 28 кнопок.
        /// Максимальный Id модуля в ARCC - 255, так как в пакете занимает 1 байт.
        /// Для того, чтобы внутри этого класса отличать модуль бинарного ввода от модуля кнопок при получении данных и при дампе, ModuleId увеличивается на 256
        /// Таким образом, даже если Id двух разных модулей кнопок и бинарного ввода пересекаются, обрабатываться они будут корректно
        /// </summary>
        public const uint IncreaseModuleIdForBinaryInput = 256;
        /// <summary>
        /// Здесь сохраняются остатки пришедших данных, не кратные размеру пакета. Учитываются при следующем чтении данных из устройства
        /// </summary>
        private byte[] _prevBufferTail = new byte[0];
        /// <summary>
        /// Переменная, обозначающая, что это первый пакет данных, принимаемый из порта
        /// </summary>
        private bool _isFirstPacket = true;
        /// <summary>
        /// В этом словаре запоминаются состояния всех кнопок модулей бинарного ввода, чтобы можно было понять, что изменилось с прошлого пакета данных от модуля
        /// Ключ - ID платы, значение - битовая маска для кнопок от 1 до 28
        /// </summary>
        //readonly Dictionary<uint, string> _binaryInputModulesState = new Dictionary<uint, string>();
        readonly Dictionary<uint, uint> _binaryInputModulesState = new Dictionary<uint, uint>();
        /// <summary>
        /// Последние состояния осей. Используется для подавления помех (фильтрация мелкого дребезга)
        /// </summary>
        private readonly Dictionary<uint, ushort[]> _axisValues = new Dictionary<uint, ushort[]>();
        /// <summary>
        /// Преобразовать исходящее событие в массив данных, готовый к передаче железу ARCC через COM-порт
        /// </summary>
        /// <param name="ev">исходящее событие роутера (зажечь/погасить лампу, вывести данные на индикатор, ...)</param>
        /// <returns>массив байт, готовый к передаче железу ARCC через COM-порт</returns>
        public byte[] ConvertEventToByteArrayForHardware(ControlEventBase ev)
        {
            byte[] outDataBuffer = null;
            // Отправка события включить линию модуля бинарного вывода
            if (ev.Hardware.ModuleType == HardwareModuleType.BinaryOutput && ev is LampEvent)
            {
                var hEvent = ev as LampEvent;
                outDataBuffer = new byte[5];
                outDataBuffer[0] = (byte)ArccModuleType.LinearOutput;
                outDataBuffer[1] = (byte)ev.Hardware.ModuleId;
                outDataBuffer[2] = (byte)ArccLinearOutputCommand.SetLampState;
                outDataBuffer[3] = (byte)ev.Hardware.ControlId;
                outDataBuffer[4] = (byte)(hEvent.IsOn ? 1 : 0);
            }
            // Отправка события установить на индикаторе текст
            // ToDo: костыль, чтобы работал поиск: ev.Hardware.ControlId != 0
            if (ev.Hardware.ModuleType == HardwareModuleType.Indicator && ev is IndicatorEvent && ev.Hardware.ControlId == 0)
            {
                    var hEvent = ev as IndicatorEvent;
                    // Размер буфера - 10 байт
                    outDataBuffer = StringToIndicatorBuffer(hEvent.IndicatorText);
                    outDataBuffer[0] = (byte) ArccModuleType.Indicator;
                    outDataBuffer[1] = (byte) ev.Hardware.ModuleId;
                    outDataBuffer[2] = (byte) ArccIndicatorCommand.SetText;
            }
            // Отправка события сдампить состояние кнопок модуля клавиатуры
            if (ev.Hardware.ModuleType == HardwareModuleType.Button && ev is DumpEvent)
            {
                outDataBuffer = new byte[5];
                outDataBuffer[0] = (byte)ArccModuleType.Button;
                outDataBuffer[1] = (byte)ev.Hardware.ModuleId;
                outDataBuffer[2] = (byte)ArccButtonCommand.DumpAllKeys;//command;
                outDataBuffer[3] = 0;
                outDataBuffer[4] = 0;
            }
            // Отправка события сдампить состояние кнопок модуля бинарного ввода
            if (ev.Hardware.ModuleType == HardwareModuleType.Button && ev is DumpEvent && ev.Hardware.ModuleId > 255)
            {
                outDataBuffer = new byte[5];
                outDataBuffer[0] = (byte)ArccModuleType.BinaryInput; // 8 - Группа модулей бинарного ввода. Всегда дампим вместе с клавишами, 
                outDataBuffer[1] = (byte)(ev.Hardware.ModuleId - IncreaseModuleIdForBinaryInput);
                outDataBuffer[2] = (byte)ArccBinaryInputCommand.DumpAllLines; // 1- настройка фильтра, 2 - дамп
                outDataBuffer[3] = 0;
                outDataBuffer[4] = 0;
            }
            return outDataBuffer;
        }
        /// <summary>
        /// Преобразование текста в массив байт для передачи индикатору. Возвращаемый буфер больше текста и соответствует длине и формату пакета, передаваемого индикатору
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Коды для передачи в индикатор</returns>
        private static byte[] StringToIndicatorBuffer(string text)
        {
            const string signs = "01234567890123456789 FDC-_^Iro.E";

            var result = new byte[10];
            for (var i = 0; i < result.Length; i++)
                result[i] = 20;
            
            var bufferPos = 9;
            for (var i = text.Length - 1; i >= 0; i--)
            {
                if (bufferPos == 2)
                    break;

                var symbol = text[i];
                if (symbol == ',')
                    symbol = '.';
                var pos = signs.IndexOf(symbol);
                if (pos == -1)
                {
                    result[bufferPos] = 20;
                    bufferPos--;
                    continue;
                }
                if (i > 0 && symbol == '.' && text[i - 1] >= '0' && text[i - 1] <= '9')
                {
                    pos = signs.IndexOf(text[i - 1], 10);
                    i -= 1;
                }

                result[bufferPos] = (byte)pos;

                bufferPos--;
            }
            return result;
        }
        /// <summary>
        /// Преобразовать данные, пришедшие от железа ARCC в события, понятные роутеру
        /// </summary>
        /// <param name="dataFromSerialPort">массив данных, пришедших от железа Arcc через COM-порт</param>
        /// <returns>массив входящих событий для роутера</returns>
        public IEnumerable<ControlEventBase> ProcessDataFromSerialPort(byte[] dataFromSerialPort)
        {
            var events = new List<ControlEventBase>();
            if (dataFromSerialPort.Length == 0)
                return events;
            // Если это первый пакет, то локальная переменная isFirstPacketLocal будет проинициализирована в true только один раз, затем всегда false
            // В первом пакете иногда приходит, а иногда не приходит 1 байт со значением 0. Для отлова этой непостоянной ситуации и служит эта переменная
            var isFirstPacketLocal = _isFirstPacket;
            _isFirstPacket = false;

            // Сначала в буфер помещаем "хвост", оставшийся от предыдущих пакетов, затем вновь пришедшие данные.
            // Хвост - остаток, не кратный 18 байтам (стандартная длина пакетов от модулей)
            var buffer = new byte[_prevBufferTail.Length + dataFromSerialPort.Length];
            _prevBufferTail.CopyTo(buffer, 0);
            dataFromSerialPort.CopyTo(buffer, _prevBufferTail.Length);

            // Если это первое получение данных их COM-порта с момента инициализации и самый первый байт равен нулю, то это не стандартный пакет ARCC, потому что первым байтом должен идти ID модуля. 0 - не является ID модуля.
            // 0 нужно проигнорировать. Так (вроде бы) железо сообщает о том, что готово к работе, но этот 0 приходит не всегда. Чаще при первом включении после подключения железа к USB.
            if (isFirstPacketLocal && buffer[0] == 0)
            {
                if (buffer.Length == 1)
                    return events;
                var newArray = new byte[buffer.Length - 1];
                Array.Copy(buffer, 1, newArray, 0, buffer.Length - 1);
                buffer = newArray;
            }

            // Обрабатываем все имеющиеся в буфере пакеты. Длина каждого 18 байт
            const int blockLength = 18;
            var blocksReceived = buffer.Length / blockLength;
            for (var i = 0; i < blocksReceived; i++)
            {
                var blockOffset = i == 0 ? i : i * blockLength;
                if (buffer[blockOffset] == (byte)ArccModuleType.Button)
                {
                    System.Diagnostics.Debug.Print("Button: " + buffer[blockOffset + 16]);
                    events.AddRange(ProcessButtonEvent(buffer, blockOffset));
                }

                // Модуль бинарного ввода
                if (buffer[blockOffset] == (byte)ArccModuleType.BinaryInput) // 8 - группа модулей бинарного ввода
                    events.AddRange(ProcessBinaryInputEvent(buffer, blockOffset));

                if (buffer[blockOffset] == (byte)ArccModuleType.Encoder)
                    events.AddRange(ProcessEncoderEvent(buffer, blockOffset));

                if (buffer[blockOffset] == (byte)ArccModuleType.Axis)
                    events.AddRange(ProcessAxisEvent(buffer, blockOffset));
            }

            // Если размер данных в буфере кратен 18, значит мы обработали все пакеты
            if (buffer.Length % blockLength == 0)
            {
                _prevBufferTail = new byte[0];
                return events;
            }
            // Если в буфере остались необработанные данные менее 18 байт, это значит, что остальная часть придёт в следущий раз
            // Сохраним остатки во временном буфере и в следующий раз склеим сохранённые данные с вновь пришедшими
            _prevBufferTail = new byte[buffer.Length % blockLength];
            Array.Copy(buffer, blocksReceived * blockLength, _prevBufferTail, 0, buffer.Length % blockLength);
            return events;
        }
        /// <summary>
        /// Обработать и добавить событие от модуля осей
        /// </summary>
        /// <param name="buffer">буфер, считанный из COM-порта</param>
        /// <param name="blockOffset">смещение с которого начинается пакет</param>
        /// <returns>набор полученных событий</returns>
        private IEnumerable<ControlEventBase> ProcessAxisEvent(byte[] buffer, int blockOffset)
        {
            var events = new List<ControlEventBase>();
            var controlEvent = new AxisEvent { Hardware = new ControlProcessorHardware { ModuleType = HardwareModuleType.Axis, ModuleId = buffer[blockOffset + 1], MotherBoardId = _motherboardId } };
            if (!_axisValues.ContainsKey(controlEvent.Hardware.ModuleId))
                _axisValues.Add(controlEvent.Hardware.ModuleId, new ushort[8]);

            const int threshold = 1;
            for (var j = 7; j >= 0; j--)
            {
                var value = buffer[blockOffset + 2 + j * 2] * 256 + buffer[blockOffset + 2 + j * 2 + 1];

                // Фильтрация дребезга оси
                if (value >= _axisValues[controlEvent.Hardware.ModuleId][j] - threshold &&
                    value <= _axisValues[controlEvent.Hardware.ModuleId][j] + threshold) continue;

                // Тест на дребезг пройден
                controlEvent = new AxisEvent
                {
                    Hardware = new ControlProcessorHardware
                    {
                        ModuleType = HardwareModuleType.Axis,
                        ModuleId = buffer[blockOffset + 1],
                        MotherBoardId = _motherboardId,
                        ControlId = (uint) ((uint) 8 - j)
                    },
                    Position = (ushort) value,
                    MinimumValue = 0,
                    MaximumValue = 1023
                };
                events.Add(controlEvent);
                _axisValues[controlEvent.Hardware.ModuleId][j] = (ushort)value;
            }
            return events;
        }
        /// <summary>
        /// Обработать и добавить событие от модуля энкодеров
        /// </summary>
        /// <param name="buffer">буфер, считанный из COM-порта</param>
        /// <param name="blockOffset">смещение с которого начинается пакет</param>
        /// <returns>набор полученных событий</returns>
        private IEnumerable<ControlEventBase> ProcessEncoderEvent(byte[] buffer, int blockOffset)
        {
            var events = new List<ControlEventBase>();
            var controlEvent = new EncoderEvent
            {
                Hardware = new ControlProcessorHardware { ModuleType = HardwareModuleType.Encoder, ControlId = buffer[blockOffset + 15], ModuleId = buffer[blockOffset + 1], MotherBoardId = _motherboardId },
                RotateDirection = (buffer[blockOffset + 17] == 1),
                ClicksCount = buffer[blockOffset + 16],
            };
            events.Add(controlEvent);
            return events;
        }
        /// <summary>
        /// Установлен ли бит
        /// </summary>
        /// <param name="b">число</param>
        /// <param name="pos">номер бита</param>
        /// <returns>true - установлен</returns>
        private bool BitCheck(uint b, int pos)
        {
            return (b & (1 << (pos - 1))) > 0;
        }
        /// <summary>
        /// Обработать и добавить событие от модуля бинарного ввода
        /// </summary>
        /// <param name="buffer">буфер, считанный из COM-порта</param>
        /// <param name="blockOffset">смещение с которого начинается пакет</param>
        /// <returns>набор полученных событий</returns>
        private IEnumerable<ControlEventBase> ProcessBinaryInputEvent(byte[] buffer, int blockOffset)
        {
            var buttonsCurrentState = (uint) (buffer[14] << 24) + (uint) (buffer[15] << 16) + (uint) (buffer[16] << 8) + (buffer[17]);

            var events = new List<ControlEventBase>();
            // Когда данные поступают в первый раз, нужно сдампить все линии, потому что, когда все линии установлены в 0 не ясно, какая кнопка сработала
            // Однако, если выключить тумблер при назначении и все линии встанут в 0, будет неверно определён сработавший тумблер (ситуация редкая, только если ни один тумблер с этой платы не назначен)
            var needToDump = false;
            // Для того, чтобы не поддерживать ещё один тип плат, moduleId увеличивается на 256 (IncreaseModuleIdForBinaryInput) и представляется модулем кнопок
            // в результате id модуля получается больше 256, что гарантирует непересечение с модулями кнопок, так как id модуля задаётся байтом
            var moduleId = buffer[blockOffset + 1] + IncreaseModuleIdForBinaryInput;
            if (!_binaryInputModulesState.ContainsKey(moduleId))
            {
                needToDump = true;
                _binaryInputModulesState.Add(moduleId, buttonsCurrentState);
            }

            var oldButtonsState = _binaryInputModulesState[moduleId];

            for (var i = 1; i < 29; i++)
            {
                if (BitCheck(oldButtonsState, i) == BitCheck(buttonsCurrentState, i) && !needToDump)
                    continue;
                var ev = new ButtonEvent
                {
                    Hardware = new ControlProcessorHardware { ModuleType = HardwareModuleType.Button, ModuleId = moduleId, ControlId = (uint)i, MotherBoardId = _motherboardId },
                    IsPressed = BitCheck(buttonsCurrentState, i),
                };
                events.Add(ev);
            }

            _binaryInputModulesState[moduleId] = buttonsCurrentState;
            return events;
        }
        /// <summary>
        /// Обработать и добавить событие от модуля кнопок
        /// </summary>
        /// <param name="buffer">буфер, считанный из COM-порта</param>
        /// <param name="blockOffset">смещение с которого начинается пакет</param>
        /// <returns>набор полученных событий</returns>
        private IEnumerable<ControlEventBase> ProcessButtonEvent(byte[] buffer, int blockOffset)
        {
            var events = new List<ControlEventBase>();
            // blockOffset + 15 == 1 - Начат дамп кнопок
            // blockOffset + 15 == 2 - Завершён дамп кнопок (не совпадает с приходящими событиями, видимо, сообщает о том, что внутри железа дамп завершён)
            if (buffer[blockOffset + 15] == 1 || buffer[blockOffset + 15] == 2 || buffer[blockOffset + 16] == 0)
                return events;
            var controlEvent = new ButtonEvent
            {
                Hardware = new ControlProcessorHardware { ModuleType = HardwareModuleType.Button, ControlId = buffer[blockOffset + 16], ModuleId = buffer[blockOffset + 1], MotherBoardId = _motherboardId },
                IsPressed = (buffer[blockOffset + 17] == 1),
            };
            //            System.Diagnostics.Debug.Print("Button: " + controlEvent.Hardware.ControlId);
            events.Add(controlEvent);
            return events;
        }
    }
}
