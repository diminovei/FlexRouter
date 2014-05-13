using System.Collections.Generic;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;
using FsuipcSdk;

namespace FlexRouter.VariableSynchronization
{
/*    public class FsuipcMethod
    {
        readonly Dictionary<int, FsuipcVariable> _fsuipcVariablesForRead = new Dictionary<int, FsuipcVariable>();
        readonly Dictionary<int, FsuipcVariable> _fsuipcVariablesForWrite = new Dictionary<int, FsuipcVariable>();
        private readonly Fsuipc _fsuipc = new Fsuipc();	// Our main fsuipc object!
        private int _dwResult = -1;				// Variable to hold returned results
        public bool Initialize()
        {
            const int dwFsReq = 0;				// Any version of FS is OK
            _fsuipc.FSUIPC_Initialization();
            return _fsuipc.FSUIPC_Open(dwFsReq, ref _dwResult);
            // dwResult - результат вызова
        }
        public void UnInitialize()
        {
            _fsuipc.FSUIPC_Close();
        }
        
        public void Open()
        {
            _fsuipcVariablesForRead.Clear();
            _fsuipcVariablesForWrite.Clear();
        }
        public bool AddVariableToRead(FsuipcVariable variable)
        {
            var varConverter = new VariableConverter();
            var convertedVariableSize = varConverter.ConvertSize(variable.Size);
            _fsuipcVariablesForRead.Add(variable.Id, variable);

            return _fsuipc.FSUIPC_Read(variable.Offset, convertedVariableSize, ref _fsuipcVariablesForRead[variable.Id].Id, ref _dwResult);
            // что делать с dwResult. Куда-то возвращать?
        }
        public void AddVariableToWrite(FsuipcVariable variable)
        {
            var varConverter = new VariableConverter();
            var convertedVariableSize = varConverter.ConvertSize(variable.Size);
            _fsuipcVariablesForWrite.Add(variable.Id, variable);

            _fsuipcVariablesForWrite[variable.Id].Buffer = varConverter.ValueToArray(variable.ValueToSet, variable.Size);
            _fsuipc.FSUIPC_Write(variable.Offset, convertedVariableSize, ref _fsuipcVariablesForWrite[variable.Id].Buffer, ref _fsuipcVariablesForWrite[variable.Id].Id, ref _dwResult);
            // что делать с dwResult. Куда-то возвращать?
        }
        public void Process()
        {
            var res = _fsuipc.FSUIPC_Process(ref _dwResult);
            foreach (var variable in _fsuipcVariablesForRead)
            {
                var varConverter = new VariableConverter();
                var convertedVariableSize = varConverter.ConvertSize(variable.Value.Size);
                _fsuipc.FSUIPC_Get(ref variable.Value.Id, convertedVariableSize, ref variable.Value.Buffer);
            }
        }
        public Dictionary<int, FsuipcVariable> GetVariables()
        {
            return _fsuipcVariablesForRead;
        }*/
    public class FsuipcMethod
    {
        internal class InternalVariableDesc
        {
            public InternalVariableDesc(ref FsuipcVariable variable)
            {
                var vc = new VariableConverter();
                Variable = variable;
                Buffer = new byte[vc.ConvertSize(variable.Size)];
            }

            internal FsuipcVariable Variable;
            internal byte[] Buffer;
            internal int Token;
        }
        readonly Dictionary<int, InternalVariableDesc> _fsuipcVariablesForRead = new Dictionary<int, InternalVariableDesc>();
        readonly Dictionary<int, InternalVariableDesc> _fsuipcVariablesForWrite = new Dictionary<int, InternalVariableDesc>();
        private readonly Fsuipc _fsuipc = new Fsuipc();	// Our main fsuipc object!
        private int _dwResult = -1;				// Variable to hold returned results
        public bool Initialize()
        {
            const int dwFsReq = 0;				// Any version of FS is OK
            _fsuipc.FSUIPC_Initialization();
            return _fsuipc.FSUIPC_Open(dwFsReq, ref _dwResult);
            // dwResult - результат вызова
        }
        public void UnInitialize()
        {
            _fsuipc.FSUIPC_Close();
        }
        
        public void Open()
        {
            _fsuipcVariablesForRead.Clear();
            _fsuipcVariablesForWrite.Clear();
        }
        public bool AddVariableToRead(FsuipcVariable variable)
        {
            var varConverter = new VariableConverter();
            var convertedVariableSize = varConverter.ConvertSize(variable.Size);
            _fsuipcVariablesForRead[variable.Id] = new InternalVariableDesc(ref variable);

            return _fsuipc.FSUIPC_Read(variable.Offset, convertedVariableSize, ref _fsuipcVariablesForRead[variable.Id].Token, ref _dwResult);
            // что делать с dwResult. Куда-то возвращать?
        }
        public bool AddVariableToWrite(FsuipcVariable variable)
        {
            var varConverter = new VariableConverter();
            var convertedVariableSize = varConverter.ConvertSize(variable.Size);
            _fsuipcVariablesForWrite[variable.Id] = new InternalVariableDesc(ref variable);

            _fsuipcVariablesForWrite[variable.Id].Buffer = varConverter.ValueToArray(variable.ValueToSet, variable.Size);
            var result = _fsuipc.FSUIPC_Write(variable.Offset, convertedVariableSize, ref _fsuipcVariablesForWrite[variable.Id].Buffer, ref _fsuipcVariablesForWrite[variable.Id].Token, ref _dwResult);
            return result;
            // что делать с dwResult. Куда-то возвращать?
        }
        public void Process()
        {
            var res = _fsuipc.FSUIPC_Process(ref _dwResult);
            foreach (var variable in _fsuipcVariablesForRead)
            {
                var varConverter = new VariableConverter();
                var convertedVariableSize = varConverter.ConvertSize(variable.Value.Variable.Size);
                _fsuipc.FSUIPC_Get(ref variable.Value.Token, convertedVariableSize, ref variable.Value.Buffer);
                variable.Value.Variable.ValueInMemory = varConverter.ArrayToValue(variable.Value.Buffer, variable.Value.Variable.Size);
            }
        }
        public double GetValue(int id)
        {
            if(!_fsuipcVariablesForRead.ContainsKey(id))
                return -1;
            return _fsuipcVariablesForRead[id].Variable.ValueInMemory;
        }
    }
}
