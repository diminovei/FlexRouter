using System;

namespace FlexRouter.Hardware.Helpers
{
    //public class ElectricDevice
    //{
    //    public int Id;
        
    //}
    public class ControlProcessorHardware
    {
        public ControlProcessorHardware()
        {
            RefreshHardwareGuid();
        }
        private string _hardwareGuid;
        private HardwareModuleType _moduleType;
        public HardwareModuleType ModuleType
        {
            get
            {
                return _moduleType;
            }
            set
            {
                _moduleType = value;
                RefreshHardwareGuid();
            }
        }

        private string _motherBoardId;
        public string MotherBoardId
        {
            get
            {
                return _motherBoardId;
            }
            set
            {
                _motherBoardId = value;
                RefreshHardwareGuid();
            }
        }
        private uint _moduleId;
        public uint ModuleId
        {
            get
            {
                return _moduleId;
            }
            set
            {
                _moduleId = value;
                RefreshHardwareGuid();
            }
        }
        private uint _blockId;
        public uint BlockId
        {
            get
            {
                return _blockId;
            }
            set
            {
                _blockId = value;
                RefreshHardwareGuid();
            }
        }
        private uint _controlId;
        public uint ControlId
        {
            get
            {
                return _controlId;
            }
            set
            {
                _controlId = value;
                RefreshHardwareGuid();
            }
        }
        public ControlProcessorHardware Copy()
        {
            return new ControlProcessorHardware { ControlId = ControlId, ModuleId = ModuleId, MotherBoardId = MotherBoardId, ModuleType = ModuleType, BlockId = BlockId};
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
            return x.MotherBoardId == y.MotherBoardId && x.ModuleType == y.ModuleType && x.ModuleId == y.ModuleId && x.ControlId == y.ControlId && x.BlockId==y.BlockId;
        }
        public static bool operator !=(ControlProcessorHardware x, ControlProcessorHardware y)
        {
            return !(x == y);
        }

        public bool Equals(ControlProcessorHardware other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.MotherBoardId == MotherBoardId && other.ModuleId == ModuleId && other.ControlId == ControlId &&
                   other.ModuleType == ModuleType && other.BlockId == BlockId;
        }
        public override int GetHashCode()
            {
            unchecked
            {
                var result = (MotherBoardId != null ? MotherBoardId.GetHashCode() : 0);
                result = (result * 397) ^ ModuleId.GetHashCode();
                result = (result * 397) ^ ControlId.GetHashCode();
                result = (result * 397) ^ ModuleType.GetHashCode();
                result = (result * 397) ^ BlockId.GetHashCode();
                return result;
            }
        }
        private void RefreshHardwareGuid()
        {
            _hardwareGuid = string.Format("{0}{1}|{2}{3}|{4}{5}|{6}{7}|{8}{9}", TagForMotherboard, MotherBoardId, TagForModuleType, ModuleType, TagForExtensionModule, ModuleId, TagForBlock, BlockId, TagForControl, ControlId);
        }
        public string GetHardwareGuid()
        {
            return _hardwareGuid;
        }

        private const string TagForMotherboard = "Mb.";
        private const string TagForModuleType = "Type.";
        private const string TagForExtensionModule = "Ext.";
        private const string TagForBlock = "Block.";
        private const string TagForControl = "Control.";

        public static string FixForNewVersion(string guid)
        {
            var s = guid.Split('|');
            if (s.Length != 4)
                return guid;
            return string.Format("{0}{1}|{2}{3}|{4}{5}|{6}{7}|{8}{9}", TagForMotherboard, s[0], TagForModuleType, s[1], TagForExtensionModule, s[2], TagForBlock, 0, TagForControl, s[3]);
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
            if (s.Length == 4)
            {
                cph.MotherBoardId = s[0];
                cph.ModuleType = GetHardwareModuleTypeByName(s[1]);
                cph.ModuleId = uint.Parse(s[2]);
                cph.ControlId = uint.Parse(s[3]);
                return cph;
            }
            if (s.Length == 5)
            {
                foreach (var sub in s)
                {
                    if (sub.StartsWith(TagForMotherboard))
                        cph.MotherBoardId = sub.Remove(0, TagForMotherboard.Length);
                    if (sub.StartsWith(TagForModuleType))
                        cph.ModuleType = GetHardwareModuleTypeByName(sub.Remove(0, TagForModuleType.Length));
                    if (sub.StartsWith(TagForExtensionModule))
                        cph.ModuleId = uint.Parse(sub.Remove(0, TagForExtensionModule.Length));
                    if (sub.StartsWith(TagForBlock))
                        cph.BlockId = uint.Parse(sub.Remove(0, TagForBlock.Length));
                    if (sub.StartsWith(TagForControl))
                        cph.ControlId = uint.Parse(sub.Remove(0, TagForControl.Length));
                }
                return cph;
            }
            return null;
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
