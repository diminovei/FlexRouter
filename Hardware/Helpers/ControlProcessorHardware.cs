using System;

namespace FlexRouter.Hardware.Helpers
{
    public class ElectricDevice
    {
        public int Id;
        
    }
    public class ControlProcessorHardware
    {
        public HardwareModuleType ModuleType { get; set; }
        public string MotherBoardId { get; set; }
        public uint ModuleId { get; set; }
        public uint ControlId { get; set; }
        public ControlProcessorHardware Copy()
        {
            return new ControlProcessorHardware { ControlId = ControlId, ModuleId = ModuleId, MotherBoardId = MotherBoardId, ModuleType = ModuleType };
        }
        public override bool Equals(Object obj)
        {
            return obj is ControlProcessorHardware && this == (ControlProcessorHardware)obj;
        }
        public static bool operator ==(ControlProcessorHardware x, ControlProcessorHardware y)
        {
            if ((object)x == null && (object)y == null)
                return true;
            if (((object)x == null && (object)y != null) || ((object)x != null && (object)y == null))
                return false;
            return x.MotherBoardId == y.MotherBoardId && x.ModuleType == y.ModuleType && x.ModuleId == y.ModuleId && x.ControlId == y.ControlId;
        }
        public static bool operator !=(ControlProcessorHardware x, ControlProcessorHardware y)
        {
            return !(x == y);
        }

        public bool Equals(ControlProcessorHardware other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.MotherBoardId == MotherBoardId && other.ModuleId == ModuleId && other.ControlId == ControlId && other.ModuleType == ModuleType;
        }
        public override int GetHashCode()
            {
            unchecked
            {
                var result = (MotherBoardId != null ? MotherBoardId.GetHashCode() : 0);
                result = (result * 397) ^ ModuleId.GetHashCode();
                result = (result * 397) ^ ControlId.GetHashCode();
                result = (result * 397) ^ ModuleType.GetHashCode();
                return result;
            }
        }
        public string GetHardwareGuid()
        {
            return string.Format("{0}|{1}|{2}|{3}", MotherBoardId, ModuleType, ModuleId, ControlId);
        }
        /// <summary>
        /// Сгенерировать класс на базе guid-строки
        /// </summary>
        /// <param name="guid">guid-строка</param>
        /// <returns>экземпляр класса</returns>
        public static ControlProcessorHardware GenerateByGuid(string guid)
        {
            var cph = new ControlProcessorHardware();
            var s = guid.Split('|');
            if (s.Length != 4)
                return null;
            cph.MotherBoardId = s[0];
            cph.ModuleType = GetHardwareModuleTypeByName(s[1]);
            cph.ModuleId = uint.Parse(s[2]);
            cph.ControlId = uint.Parse(s[3]);
            return cph;
        }
        private static HardwareModuleType GetHardwareModuleTypeByName(string moduleType)
        {
            foreach (HardwareModuleType mt in Enum.GetValues(typeof(HardwareModuleType)))
            {
                if (mt.ToString() == moduleType)
                    return mt;
            }
            return HardwareModuleType.Unknown;
        }
    }
}
