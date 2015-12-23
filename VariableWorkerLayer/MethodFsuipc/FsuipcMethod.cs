using System;
using System.Collections.Generic;
using FsuipcSdk;

namespace FlexRouter.VariableWorkerLayer.MethodFsuipc
{
    public class FsuipcMethod
    {
        internal class InternalVariableDesc
        {
            public InternalVariableDesc(ref FsuipcVariable variable)
            {
                var vc = new VariableConverter();
                Variable = variable;
                Buffer = new byte[vc.ConvertSize(variable.GetVariableSize())];
            }

            internal FsuipcVariable Variable;
            internal byte[] Buffer;
            internal int Token;
        }
        readonly Dictionary<Guid, InternalVariableDesc> _fsuipcVariablesForRead = new Dictionary<Guid, InternalVariableDesc>();
        readonly Dictionary<Guid, InternalVariableDesc> _fsuipcVariablesForWrite = new Dictionary<Guid, InternalVariableDesc>();
        private readonly Fsuipc _fsuipc = new Fsuipc();	// Our main fsuipc object!
        private int _dwResult = -1;				// Variable to hold returned results
        private InitializationState _lastInitStatus;
        private DateTime _lastTimeTryToInitialize = DateTime.MinValue;
        public InitializationState Initialize()
        {
            const string systemName = "FSUIPC";
            // Без этого процессор грузится на 50%, пока симулятор не загружен
            if (DateTime.Now < _lastTimeTryToInitialize + new TimeSpan(0, 0, 0, 2))
            {
                if (_lastInitStatus != null)
                    return _lastInitStatus;
                return new InitializationState
                {
                    System = systemName,
                    ErrorCode = -1,
                    ErrorMessage = "Attempted to initialize too often",
                    IsOk = false
                };
            }
            _lastTimeTryToInitialize = DateTime.Now;            
            _fsuipcVariablesForRead.Clear();
            _fsuipcVariablesForWrite.Clear();
            const int dwFsReq = 0;				// Any version of FS is OK
            _fsuipc.FSUIPC_Initialization();
            var initStatus = _fsuipc.FSUIPC_Open(dwFsReq, ref _dwResult);
            _lastInitStatus = new InitializationState {IsOk = initStatus, System = systemName, ErrorCode = _dwResult};

            switch (_lastInitStatus.ErrorCode)
            {
                case 0:
                    _lastInitStatus.ErrorMessage = "";
                    break;
                case 1:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_OPEN: Attempt to Open when already Open";
                    break;
                case 2:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_NOFS: Cannot link to FSUIPC or WideClient";
                    break;
                case 3:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_REGMSG: Failed to Register common message with Windows";
                    break;
                case 4:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_ATOM: Failed to create Atom for mapping filename";
                    break;
                case 5:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_MAP: Failed to create a file mapping object";
                    break;
                case 6:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_VIEW: Failed to open a view to the file map";
                    break;
                case 7:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_VERSION: Incorrect version of FSUIPC, or not FSUIPC";
                    break;
                case 8:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_WRONGFS: Sim is not version requested";
                    break;
                case 9:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_NOTOPEN: Call cannot execute, link not Open";
                    break;
                case 10:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_NODATA: Call cannot execute: no requests accumulated";
                    break;
                case 11:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_TIMEOUT: IPC timed out all retries";
                    break;
                case 12:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_SENDMSG: IPC sendmessage failed all retries";
                    break;
                case 13:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_DATA: IPC request contains bad data";
                    break;
                case 14:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_RUNNING: Maybe running on WideClient, but FS not running on Server, or wrong FSUIPC";
                    break;
                case 15:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_SIZE";
                    break;
                case 16:
                    _lastInitStatus.ErrorMessage = "FSUIPC_ERR_BUFOVERFLOW";
                    break;
                default:
                    _lastInitStatus.ErrorMessage = "Unknown error";
                    break;
            }
            return _lastInitStatus;
        }
        public void UnInitialize()
        {
            _fsuipc.FSUIPC_Close();
        }

        public bool AddVariableToRead(FsuipcVariable variable)
        {
            var varConverter = new VariableConverter();
            var convertedVariableSize = varConverter.ConvertSize(variable.GetVariableSize());
            _fsuipcVariablesForRead[variable.Id] = new InternalVariableDesc(ref variable);

            return _fsuipc.FSUIPC_Read(variable.Offset, convertedVariableSize, ref _fsuipcVariablesForRead[variable.Id].Token, ref _dwResult);
            // что делать с dwResult. Куда-то возвращать?
        }
        public bool AddVariableToWrite(FsuipcVariable variable)
        {
            var varConverter = new VariableConverter();
            var convertedVariableSize = varConverter.ConvertSize(variable.GetVariableSize());
            _fsuipcVariablesForWrite[variable.Id] = new InternalVariableDesc(ref variable);
            _fsuipcVariablesForWrite[variable.Id].Buffer = varConverter.ValueToArray((double)variable.GetValueToSet(), variable.GetVariableSize());
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
                var convertedVariableSize = varConverter.ConvertSize(variable.Value.Variable.GetVariableSize());
                _fsuipc.FSUIPC_Get(ref variable.Value.Token, convertedVariableSize, ref variable.Value.Buffer);
                variable.Value.Variable.SetValueInMemory(varConverter.ArrayToValue(variable.Value.Buffer, variable.Value.Variable.GetVariableSize()));
            }
        }

        public void Prepare()
        {
            _fsuipcVariablesForRead.Clear();
            _fsuipcVariablesForWrite.Clear();
        }
        public double? GetValue(Guid id)
        {
            if(!_fsuipcVariablesForRead.ContainsKey(id))
                return -1;
//            return _fsuipcVariablesForRead[id].Variable.GetValueInMemory();
            var varConverter = new VariableConverter();
            return varConverter.ArrayToValue(_fsuipcVariablesForRead[id].Buffer, _fsuipcVariablesForRead[id].Variable.GetVariableSize());
        }
    }
}
