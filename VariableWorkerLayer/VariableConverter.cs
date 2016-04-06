using System;

namespace FlexRouter.VariableWorkerLayer
{
    class VariableConverter
    {
        /// <summary>
        /// Преобразование массива байт в число
        /// </summary>
        /// <param name="buffer">массив байт</param>
        /// <param name="variableSize">размер переменной в массиве</param>
        /// <returns></returns>
        public double ArrayToValue(byte[] buffer, MemoryVariableSize variableSize)
        {
            double result = 0;

            if (variableSize == MemoryVariableSize.Byte)
                result = buffer[0];
            if (variableSize == MemoryVariableSize.ByteSigned)
                result = (sbyte)buffer[0];
            if (variableSize == MemoryVariableSize.TwoBytes)
                result = BitConverter.ToUInt16(buffer, 0);
            if (variableSize == MemoryVariableSize.TwoBytesSigned)
                result = BitConverter.ToInt16(buffer, 0);
            if (variableSize == MemoryVariableSize.FourBytes)
                result = BitConverter.ToUInt32(buffer, 0);
            if (variableSize == MemoryVariableSize.FourBytesSigned)
                result = BitConverter.ToInt32(buffer, 0);
            if (variableSize == MemoryVariableSize.EightBytes)
                result = BitConverter.ToUInt64(buffer, 0);
            if (variableSize == MemoryVariableSize.EightBytesSigned)
                result = BitConverter.ToInt64(buffer, 0);
            if (variableSize == MemoryVariableSize.FourBytesFloat)
                result = BitConverter.ToSingle(buffer, 0);
            if (variableSize == MemoryVariableSize.EightByteFloat)
                result = BitConverter.ToDouble(buffer, 0);
            return result;
        }
        /// <summary>
        /// Преобразование числа в массив
        /// </summary>
        /// <param name="value">число</param>
        /// <param name="variableSize">в массив какого размера преобразовать</param>
        /// <returns></returns>
        public byte[] ValueToArray(double value, MemoryVariableSize variableSize)
        {
            var buffer = new byte[ConvertSize(variableSize)];
            if (variableSize == MemoryVariableSize.Byte)
                buffer[0] = (byte)value;
            if (variableSize == MemoryVariableSize.ByteSigned)
                buffer[0] = (byte)value;
            if (variableSize == MemoryVariableSize.TwoBytes)
                buffer = BitConverter.GetBytes((ushort)value);
            if (variableSize == MemoryVariableSize.TwoBytesSigned)
                buffer = BitConverter.GetBytes((short)value);
            if (variableSize == MemoryVariableSize.FourBytes)
                buffer = BitConverter.GetBytes((uint)value);
            if (variableSize == MemoryVariableSize.FourBytesSigned)
                buffer = BitConverter.GetBytes((int)value);
            if (variableSize == MemoryVariableSize.EightBytes)
                buffer = BitConverter.GetBytes((ulong)value);
            if (variableSize == MemoryVariableSize.EightBytesSigned)
                buffer = BitConverter.GetBytes((long)value);
            if (variableSize == MemoryVariableSize.FourBytesFloat)
                buffer = BitConverter.GetBytes((float)value);
            if (variableSize == MemoryVariableSize.EightByteFloat)
                buffer = BitConverter.GetBytes(value);
            return buffer;
        }
        /// <summary>
        /// Convert MemoryVariableSize to bytes number
        /// </summary>
        /// <param name="size">Size type</param>
        /// <returns>Size in bytes</returns>
        public int ConvertSize(MemoryVariableSize size)
        {
            if (size == MemoryVariableSize.Byte || size == MemoryVariableSize.ByteSigned)
                return 1;
            if (size == MemoryVariableSize.TwoBytes || size == MemoryVariableSize.TwoBytesSigned)
                return 2;
            if (size == MemoryVariableSize.FourBytes || size == MemoryVariableSize.FourBytesSigned || size == MemoryVariableSize.FourBytesFloat)
                return 4;
            if (size == MemoryVariableSize.EightBytes || size == MemoryVariableSize.EightBytesSigned || size == MemoryVariableSize.EightByteFloat)
                return 8;
            return 0;
        }
    }
}
