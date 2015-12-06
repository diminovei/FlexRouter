using Microsoft.Win32.SafeHandles;

namespace FlexRouter.VariableWorkerLayer.MethodMemoryPatch
{
    // ==++== 
    //
    //   Copyright (c) Microsoft Corporation.  All rights reserved.
    //
    // ==--== 
    /*============================================================
    ** 
    ** Class:  SafeProcessHandle 
    **
    ** A wrapper for a process handle 
    **
    **
    ===========================================================*/

    using System;
    using System.Security;
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;

    namespace Microsoft.Win32.SafeHandles
    {
        [HostProtection(MayLeakOnAbort = true)]
        [SuppressUnmanagedCodeSecurity]
        public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal static SafeProcessHandle InvalidHandle = new SafeProcessHandle(IntPtr.Zero);

            // Note that OpenProcess returns 0 on failure 

            internal SafeProcessHandle() : base(true) { }

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            internal SafeProcessHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern SafeProcessHandle OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, Int32 dwProcessId);
            [DllImport("Kernel32", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool CloseHandle(IntPtr handle);


            internal void InitialSetHandle(IntPtr h)
            {
                Debug.Assert(base.IsInvalid, "Safe handle should only be set once");
                base.handle = h;
            }

            override protected bool ReleaseHandle()
            {
                //return SafeNativeMethods.CloseHandle(handle);
                return CloseHandle(handle);
            }
        }
    }
}
