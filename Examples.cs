﻿using System.Collections.Generic;
using System.Management;
using System.Threading;
using System.Diagnostics;
using FlexRouter.VariableWorkerLayer;
using FlexRouter.VariableWorkerLayer.MethodClick;
using FlexRouter.VariableWorkerLayer.MethodFsuipc;

namespace FlexRouter
{
    class Examples
    {
//        private static void TestInit()
//        {
//                        var panelId1 = RegisterPanel(new Panel {Name = "Оверхед"}, true);
//                        var panelId2 = RegisterPanel(new Panel {Name = "НВУ"}, true);
//                        var panelId3 = RegisterPanel(new Panel {Name = "КВС"}, true);
//                        var mpv = new MemoryPatchVariable
//                            {
//                                Offset = 0x124F08,
//                                Size = MemoryVariableSize.Byte,
//                                ModuleName = "NN_pnk_154m_v1_25.GAU",
//                                Name = "Переключатель ДМЕ (3 позиции)",
//                                PanelId = panelId2,
//                                Description = "Тестовая переменная 1\nНовая строка"
//                            };
//                        var var1Id = StoreVariable(mpv, true);

//                        var mpv1 = new MemoryPatchVariable
//                            {
//                                Offset = 0x12BEB0,
//                                Size = MemoryVariableSize.Byte,
//                                ModuleName = "NN_pnk_154m_v1_25.GAU",
//                                Name = "Ввод ЗК (2 позиции)",
//                                PanelId = panelId3,
//                                Description = "Тестовая переменная 2\nНовая строка"
//                            };
//                        var var2Id = StoreVariable(mpv1, true);
//                        var fv2 = new FsuipcVariable
//                            {
//                                Offset = 0x342,
//                                Size = MemoryVariableSize.Byte,
//                                Name = "Выпуск чего-нибудь",
//                                PanelId = panelId3,
//                                Description = "Тестовая переменная 3\nНовая строка"
//                            };
//                        var var3Id = StoreVariable(fv2, true);

//                        var mpv2 = new MemoryPatchVariable
//                            {
//                                Offset = 0x36179,
//                                Size = MemoryVariableSize.Byte,
//                                ModuleName = "NN_pnk_154m_v1_25.GAU",
//                                Name = "РСБН (единицы)",
//                                PanelId = panelId1,
//                                Description = "РСБН Единицы"
//                            };
//                        var mpv3 = new MemoryPatchVariable
//                            {
//                                Offset = 0x36497,
//                                Size = MemoryVariableSize.Byte,
//                                ModuleName = "NN_pnk_154m_v1_25.GAU",
//                                Name = "АРК1 (сотни)",
//                                PanelId = panelId1,
//                                Description = "111"
//                            };

//                        var var5Id = StoreVariable(mpv3, true);
//                        var var4Id = StoreVariable(mpv2, true);
            

//            // Button
//            var ad = new DescriptorValue();
//            ad.AddConnector("Off");
//            ad.AddConnector("On");
//            ad.AddVariable(/*var1Id*/0);
//            ad.AddVariable(/*var2Id*/1);
//            ad.SetFormula(0, 0, "0");
//            ad.SetFormula(0, 1, "0");
//            ad.SetFormula(1, 0, "1");
//            ad.SetFormula(1, 1, "2");
//            ad.AssignDefaultStateId(0);
//            ad.SetAssignedPanelId(/*panelId3*/2);
//            ad.SetName("TestAD");
//            RegisterAccessDescriptor(ad, true);

//            var cp = new ButtonProcessor(ad.GetId());
//            var cpId = Add(cp, ad.GetId());
//            //            ad.AssignControlProcessor(cpId);
//            cp.AssignHardware(1, "Arcc:2905A4F9|Button|1|81");

//            // BinaryInput
//            var ad3 = new DescriptorValue();
//            ad3.AddConnector("Off");
//            ad3.AddConnector("On");
//            ad3.AddVariable(/*var1Id*/0);
//            ad3.AddVariable(/*var2Id*/1);
//            ad3.SetFormula(0, 0, "0");
//            ad3.SetFormula(0, 1, "0");
//            ad3.SetFormula(1, 0, "1");
//            ad3.SetFormula(1, 1, "2");
//            //            ad3.AssignDefaultState(0);
//            ad3.SetAssignedPanelId(/*panelId3*/2);
//            ad3.SetName("TestADBI");
//            RegisterAccessDescriptor(ad3, true);

//            /*            var cp3 = new ButtonBinaryInputProcessor(ad3.GetId());
//                        var cpId3 = Add(cp3);
//                        ad3.AssignControlProcessor(cpId3);*/


//            /////////// Encoder
//            var ad1 = new DescriptorRange();
//            ad1.SetFormulaToGetValues("[Оверхед.АРК1 (сотни)]");
//            ad1.AddVariable(/*var5Id*/4);
//            ad1.SetAssignedPanelId(/*panelId1*/0);
//            ad1.SetName("Оверхед.АРК1 (сотни)");
//            ad1.SetFormula(0, /*var5Id*/4, "[R]");
//            ad1.SetFormula(1, /*var5Id*/4, "[R]");
//            ad1.MinimumValue = 0;
//            ad1.MaximumValue = 16;
//            ad1.Step = 1;
//            ad1.IsLooped = true;
//            var encoderAdId = RegisterAccessDescriptor(ad1, true);

//            var ecp = new EncoderProcessor(encoderAdId);
//            ecp.AssignHardware("Arcc:2905A4F9|Encoder|1|4");
//            var ecpId = Add(ecp, ad1.GetId());
//            //            ad1.AssignControlProcessor(ecpId);
//            ecp.SetInversion(true);
//            /////////// Indicator
//            var indicatorAd = new DescriptorIndicator();
//            indicatorAd.SetName("Оверхед.АРК1 (сотни) индикатор");
//            indicatorAd.SetAssignedPanelId(/*panelId1*/0);
//            indicatorAd.SetFormula("[Оверхед.АРК1 (сотни)]");
//            indicatorAd.SetNumberOfDigitsAfterPoint(3);
//            var indicatorId = RegisterAccessDescriptor(indicatorAd, true);
//            var icp = new IndicatorProcessor(indicatorId);
//            icp.AssignHardware("Arcc:2905A4F9|Indicator|8|0");
//            var icpId = Add(icp, indicatorAd.GetId());
//            //            indicatorAd.AssignControlProcessor(icpId);
//            /////////// BinaryOutput
//            var boAd = new DescriptorBinaryOutput();
//            boAd.SetFormula("[НВУ.Переключатель ДМЕ (3 позиции)]==1");
//            boAd.SetName("НВУ.Переключатель ДМЕ");

//            boAd.SetAssignedPanelId(/*panelId1*/0);
//            var boId = RegisterAccessDescriptor(boAd, true);
//            var lcp = new LampProcessor(boId);
//            lcp.AssignHardware("Arcc:2905A4F9|BinaryOutput|1|7");
//            var lcpId = Add(lcp, boAd.GetId());
//            //           boAd.AssignControlProcessor(lcpId);
//        }
//*/
//        /*       private void CheckVarManager()
//               {
//                   var variableMeanager = new VariableManager();
//                   variableMeanager.Initialize();
//                   var mpv = new MemoryPatchVariable();
//                   mpv.ModuleName = "NN_pnk_154m_v1_25.GAU";
//                   mpv.Offset = 0x1200D0; // Индикатор широты на ТКС
//                   mpv.Size = MemoryVariableSize.EightByteFloat;

//                   var mpv1 = new MemoryPatchVariable();
//                   mpv1.ModuleName = "NN_pnk_154m_v1_25.GAU";
//                   mpv1.Offset = 0x36148; // Индикатор широты на ТКС
//                   mpv1.Size = MemoryVariableSize.EightByteFloat;


//                   var id = variableMeanager.StoreVariable(mpv, true);
//                   var id1 = variableMeanager.StoreVariable(mpv1, true);

//                   variableMeanager.Start();
//                   double res = 0;
//                   var val = -90;
//                   while (true)
//                   {
//                       var res1 = variableMeanager.ReadValue(id);
//                       var xxx = variableMeanager.WriteValue(id1, val);
//                       val++;
//                       if (val > 90)
//                           val = -90;
//                       if (res1.Value != res)
//                       {
//                           //                    Debug.Print(res1.Value + "\t" + res1.Error);
//                           //                    res = res1.Value;
//                           System.Windows.Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate { Output.Text = res1.Value.ToString(); });

//                           //Output.Text = res1.Value.ToString();
//                           //                    Output1.Text = res1.Value.ToString();
//                       }
//                       Thread.Sleep(10);
//                   }
//               }
        private void CheckPlayMacro()
        {
            var windowInfo = new WindowInfo { Name = "Over" };
            var clickMethod = new ClickMethodForFs2004();
            var winId = clickMethod.AddWindow(windowInfo, true);
            clickMethod.FindAddedWindows();
            foreach (var win in clickMethod._simWindows)
                win.Value.SaveChanges();
            var macroProcessor = new ClickMacroProcessor();
            var wi = new MacroProcessorWindowInfo();
            wi.Hwnd = clickMethod._simWindows[winId].Hwnd;
            wi.Id = winId;
            wi.Size = clickMethod._simWindows[winId].Coordinares;
            var wi1 = new MacroProcessorWindowInfo();
            wi1.Hwnd = clickMethod._simWindows[clickMethod._simulatorMainWindowId].Hwnd;
            wi1.Id = clickMethod._simulatorMainWindowId;
            wi1.Size = clickMethod._simWindows[clickMethod._simulatorMainWindowId].Coordinares;
            macroProcessor.RenewWindowsInfo(new[] { wi, wi1 }, wi1.Id);
            var macro = new MacroToken();
            macro.WindowId = wi.Id;
            macro.Actions = new[] { new MouseEvent { Action = MouseAction.MouseLeftClick, MouseX = 587, MouseY = 244, WindowWidth = 1216, WindowHeight = 797 } };
            var macro2 = new MacroToken();
            macro2.WindowId = wi.Id;
            macro2.Actions = new[] { new MouseEvent { Action = MouseAction.MouseLeftClick, MouseX = 652, MouseY = 245, WindowWidth = 1216, WindowHeight = 797 } };
            var macro3 = new MacroToken();
            macro3.WindowId = wi.Id;
            macro3.Actions = new[] { new MouseEvent { Action = MouseAction.MouseRightClick, MouseX = 255, MouseY = 38, WindowWidth = 591, WindowHeight = 648 } };
            while (true)
            {
                macroProcessor.PlayMacro(new[] { macro });
                macroProcessor.PlayMacro(new[] { macro3 });
                Debug.Print("1");
                Thread.Sleep(1000);
                macroProcessor.PlayMacro(new[] { macro2 });
                macroProcessor.PlayMacro(new[] { macro3 });
                Debug.Print("2");
                Thread.Sleep(1000);

            }
        }
        private void CheckClick()
        {
            /*            windowInfo = new WindowInfo { Name = "Throttle" };
                        clickMethod.AddWindow(windowInfo);

                        windowInfo = new WindowInfo { Name = "NVU"};
                        clickMethod.AddWindow(windowInfo);

                        windowInfo = new WindowInfo { Name = "Over"};
                        clickMethod.AddWindow(windowInfo);*/

            /*            while (true)
                        {
                            if (clickMethod.Initialize(simWindowId, planeWindowId))
                            {
                                Debug.Print("ClickMethod Initialized");
                                break;
                            }
                            Thread.Sleep(1000);
                        }*/
            //            clickMethod.WatchWindows();
            /*            var prevCC = ClickMethodForFs2004.Fs2004ChangeSightEvent.Nothing;
                        bool OutSight = false;
                        while(true)
                        {
                            clickMethod.FindAddedWindows();
            //                var changes = clickMethod._simWindows[windowInfo1.Id].GetChanges();
                            var cc = clickMethod.GetSimulatorChangeSightEvent();
            //                Debug.Print("----------");
                            foreach (var win in clickMethod._simWindows)
                            {
                                win.Value.SaveChanges();
            //                    Debug.Print(win.Value.ZFactor + ":" + win.Value.UserInfo.Name);
                            }
                            var changes = clickMethod._simWindows[windowInfo1.Id].SaveChanges();
                            if (prevCC != cc)
                                Debug.Print(cc.ToString());
                            if (clickMethod._simWindows[windowInfo2.Id].ZFactor != -1)
                            {
                                if (clickMethod._simWindows[windowInfo2.Id].ZFactor < clickMethod._simWindows[windowInfo1.Id].ZFactor && !OutSight)
                                {
                                    Debug.Print("OutSide");
                                    OutSight = true;
                                }
                                if (clickMethod._simWindows[windowInfo2.Id].ZFactor > clickMethod._simWindows[windowInfo1.Id].ZFactor && OutSight)
                                {
                                    Debug.Print("Inside");
                                    OutSight = false;
                                }
                            }
                            Thread.Sleep(100);    
            //                Debug.Print("Waiting");
                        }*/
        }
        private void CheckMemoryPatch()
        {
            /*            var mem = new MemoryPatchMethod();
                        var main = "fs9";
                        var module = "NN_pnk_154m_v1_25.GAU";
            //            var a = mem.Initialize(main);
                        var b = mem.CheckModulePresence(main, module);
                        var ind = mem.GetVariableValue(module, 0x1200D0, MemoryVariableSize.EightByteFloat);*/
            //            b = b;
        }
        private void CheckFSUIPC()
        {
/*            var v = new FsuipcVariable { Id = 0, Name = "Flaps", Offset = 0x0BDC, Set = MemoryVariableSize.FourBytes };
            var v2 = new FsuipcVariable { Id = 2, Name = "Bano", Offset = 0x280, Size = MemoryVariableSize.Byte };
            var v3 = new FsuipcVariable { Id = 3, Name = "Ready to fly", Offset = 0x3364, Size = MemoryVariableSize.Byte };

            var fsuipc = new FsuipcMethod();
            fsuipc.Initialize();
            var val = new[] { 0, 4095, 8191, 12287, 16383 };
            //            var val = new int[] { 0,1 };
            var i = 0;
            while (1 == 1)
            {
                v.ValueToSet = val[i];
                fsuipc.AddVariableToWrite(v);
                fsuipc.AddVariableToRead(v3);
                fsuipc.Process();
                var a = fsuipc.GetValue(3);
                Debug.Print(a.ToString());
                Thread.Sleep(100);
                i++;
                if (i > val.Length - 1)
                    i = 0;
            }*/
        }
    }
    //protected override void OnSourceInitialized(EventArgs e)
    //   {
    //       base.OnSourceInitialized(e);

    //       // Adds the windows message processing hook and registers USB device add/removal notification.
    //       HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
    //       if (source != null)
    //       {
    //           windowHandle = source.Handle;
    //           source.AddHook(HwndHandler);
    //           UsbNotification.RegisterUsbDeviceNotification(windowHandle);
    //       }
    //   }

    /*        /// <summary>
            /// Method that receives window messages.
            /// </summary>
           private IntPtr HwndHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
            {
                if (msg == UsbNotification.WmDevicechange)
                {
                    switch ((int) wparam)
                    {
                        case UsbNotification.DbtDeviceremovecomplete:
                            //Usb_DeviceRemoved(); // this is where you do your magic
                            System.Diagnostics.Debug.Print(DateTime.Now + ": UsbRemoved");
                            break;
                        case UsbNotification.DbtDevicearrival:
                            System.Diagnostics.Debug.Print(DateTime.Now + ": UsbAdded");

    //                    Usb_DeviceAdded(); // this is where you do your magic
                            break;
                    }
                }

                handled = false;
                return IntPtr.Zero;
            }*/
/*        class USBDeviceInfo
        {
            public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
            {
                this.DeviceID = deviceID;
                this.PnpDeviceID = pnpDeviceID;
                this.Description = description;
            }
            public string DeviceID { get; private set; }
            public string PnpDeviceID { get; private set; }
            public string Description { get; private set; }
        }
          private static List<USBDeviceInfo> GetUsbDevices()
            {
                var devices = new List<USBDeviceInfo>();

                ManagementObjectCollection collection;
                using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub"))
                    collection = searcher.Get();

                foreach (var device in collection)
                {
                    devices.Add(new USBDeviceInfo(
                        (string) device.GetPropertyValue("DeviceID"),
                        (string) device.GetPropertyValue("PNPDeviceID"),
                        (string) device.GetPropertyValue("Description")
                        ));
                }

                collection.Dispose();
                return devices;
            }*/


}
