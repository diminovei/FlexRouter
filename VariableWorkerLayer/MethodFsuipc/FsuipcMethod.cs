using System.Collections.Generic;
using FlexRouter.VariableSynchronization;
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
        public InitializationState Initialize()
        {
            _fsuipcVariablesForRead.Clear();
            _fsuipcVariablesForWrite.Clear();
            const int dwFsReq = 0;				// Any version of FS is OK
            _fsuipc.FSUIPC_Initialization();
            var initStatus = _fsuipc.FSUIPC_Open(dwFsReq, ref _dwResult);
            var fsuipcResult = new InitializationState();
            fsuipcResult.IsOk = initStatus;
            fsuipcResult.System = "FSUIPC";
            fsuipcResult.ErrorCode = _dwResult;

            switch (fsuipcResult.ErrorCode)
            {
                case 0:
                    fsuipcResult.ErrorMessage = "";
                    break;
                case 1:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_OPEN: Attempt to Open when already Open";
                    break;
                case 2:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_NOFS: Cannot link to FSUIPC or WideClient";
                    break;
                case 3:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_REGMSG: Failed to Register common message with Windows";
                    break;
                case 4:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_ATOM: Failed to create Atom for mapping filename";
                    break;
                case 5:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_MAP: Failed to create a file mapping object";
                    break;
                case 6:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_VIEW: Failed to open a view to the file map";
                    break;
                case 7:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_VERSION: Incorrect version of FSUIPC, or not FSUIPC";
                    break;
                case 8:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_WRONGFS: Sim is not version requested";
                    break;
                case 9:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_NOTOPEN: Call cannot execute, link not Open";
                    break;
                case 10:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_NODATA: Call cannot execute: no requests accumulated";
                    break;
                case 11:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_TIMEOUT: IPC timed out all retries";
                    break;
                case 12:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_SENDMSG: IPC sendmessage failed all retries";
                    break;
                case 13:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_DATA: IPC request contains bad data";
                    break;
                case 14:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_RUNNING: Maybe running on WideClient, but FS not running on Server, or wrong FSUIPC";
                    break;
                case 15:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_SIZE";
                    break;
                case 16:
                    fsuipcResult.ErrorMessage = "FSUIPC_ERR_BUFOVERFLOW";
                    break;
                default:
                    fsuipcResult.ErrorMessage = "Unknown error";
                    break;
            }
            return fsuipcResult;
        }
        public void UnInitialize()
        {
            _fsuipc.FSUIPC_Close();
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
