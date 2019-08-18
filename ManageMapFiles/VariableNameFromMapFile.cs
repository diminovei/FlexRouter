using System;

namespace FlexRouter.ManageMapFiles
{
    internal class VariableNameFromMapFile
    {
        public Guid VarId;
        public string NameInMapFile;
        public uint DistanceToClosestVarBelow; // Количество байт до ближайшей переменной "снизу"
    }
}
