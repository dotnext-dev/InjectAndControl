using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Injector.Common
{
    class Injector
    {
        static string Suffix(IntPtr windowHandle)
        {
            var window = new WindowInfo(windowHandle);
            string bitness = IntPtr.Size == 8 ? "64" : "32";
            string clr = "3.5";


            foreach (var module in window.Modules)
            {
                // a process is valid to snoop if it contains a dependency on PresentationFramework, PresentationCore, or milcore (wpfgfx).
                // this includes the files:
                // PresentationFramework.dll, PresentationFramework.ni.dll
                // PresentationCore.dll, PresentationCore.ni.dll
                // wpfgfx_v0300.dll (WPF 3.0/3.5)
                // wpfgrx_v0400.dll (WPF 4.0)

                // note: sometimes PresentationFramework.dll doesn't show up in the list of modules.
                // so, it makes sense to also check for the unmanaged milcore component (wpfgfx_vxxxx.dll).
                // see for more info: http://snoopwpf.codeplex.com/Thread/View.aspx?ThreadId=236335

                // sometimes the module names aren't always the same case. compare case insensitive.
                // see for more info: http://snoopwpf.codeplex.com/workitem/6090

                if
                (
                    module.szModule.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
                    module.szModule.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                    module.szModule.StartsWith("wpfgfx", StringComparison.OrdinalIgnoreCase) ||
                    module.szModule.StartsWith("clr", StringComparison.OrdinalIgnoreCase)
                )
                {
                    if (FileVersionInfo.GetVersionInfo(module.szExePath).FileMajorPart > 3)
                    {
                        clr = "4.0";
                    }
                }
                if (module.szModule.Contains("wow64.dll"))
                {
                    if (FileVersionInfo.GetVersionInfo(module.szExePath).FileMajorPart > 3)
                    {
                        bitness = "32";
                    }
                }
            }
            return bitness + "-" + clr;
        }

        internal static void Launch(IntPtr windowHandle, Assembly assembly, string className, string methodName)
        {
            var location = Assembly.GetEntryAssembly().Location;
            var directory = Path.GetDirectoryName(location);
            var file = Path.Combine(directory, "HelperDlls", "ManagedInjectorLauncher" + Suffix(windowHandle) + ".exe");

            Debug.WriteLine(file + " " + windowHandle + " \"" + assembly.Location + "\" \"" + className + "\" \"" + methodName + "\"");
            Process.Start(file, windowHandle + " \"" + assembly.Location + "\" \"" + className + "\" \"" + methodName + "\"");
        }
    }

    public class WindowInfo
    {
        public WindowInfo(IntPtr hwnd)
        {
            this.hwnd = hwnd;
        }

        public IEnumerable<NativeMethods.MODULEENTRY32> Modules
        {
            get
            {
                if (_modules == null)
                    _modules = GetModules().ToArray();
                return _modules;
            }
        }
        /// <summary>
        /// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
        /// except that we include 32 bit modules when Snoop runs in 64 bit mode.
        /// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
        /// </summary>
        private IEnumerable<NativeMethods.MODULEENTRY32> GetModules()
        {
            int processId;
            NativeMethods.GetWindowThreadProcessId(hwnd, out processId);

            var me32 = new NativeMethods.MODULEENTRY32();
            var hModuleSnap = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.SnapshotFlags.Module | NativeMethods.SnapshotFlags.Module32, processId);
            if (!hModuleSnap.IsInvalid)
            {
                using (hModuleSnap)
                {
                    me32.dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(me32);
                    if (NativeMethods.Module32First(hModuleSnap, ref me32))
                    {
                        do
                        {
                            yield return me32;
                        } while (NativeMethods.Module32Next(hModuleSnap, ref me32));
                    }
                }
            }
        }
        private IEnumerable<NativeMethods.MODULEENTRY32> _modules;

        public bool IsWpfProcess
        {
            get
            {
                bool isWpf = false;
                try
                {
                    if (this.hwnd == IntPtr.Zero)
                        return false;

                    Process process = this.OwningProcess;
                    if (process == null)
                        return false;

                    // see if we have cached the process validity previously, if so, return it.
                    if (WindowInfo.processIDToValidityMap.TryGetValue(process.Id, out isWpf))
                        return isWpf;

                    // else determine the process validity and cache it.
                    if (process.Id == Process.GetCurrentProcess().Id)
                    {
                        isWpf = false;

                        // the above line stops the user from snooping on snoop, since we assume that ... that isn't their goal.
                        // to get around this, the user can bring up two snoops and use the second snoop ... to snoop the first snoop.
                        // well, that let's you snoop the app chooser. in order to snoop the main snoop ui, you have to bring up three snoops.
                        // in this case, bring up two snoops, as before, and then bring up the third snoop, using it to snoop the first snoop.
                        // since the second snoop inserted itself into the first snoop's process, you can now spy the main snoop ui from the
                        // second snoop (bring up another main snoop ui to do so). pretty tricky, huh! and useful!
                    }
                    else
                    {
                        // a process is valid to snoop if it contains a dependency on PresentationFramework, PresentationCore, or milcore (wpfgfx).
                        // this includes the files:
                        // PresentationFramework.dll, PresentationFramework.ni.dll
                        // PresentationCore.dll, PresentationCore.ni.dll
                        // wpfgfx_v0300.dll (WPF 3.0/3.5)
                        // wpfgrx_v0400.dll (WPF 4.0)

                        // note: sometimes PresentationFramework.dll doesn't show up in the list of modules.
                        // so, it makes sense to also check for the unmanaged milcore component (wpfgfx_vxxxx.dll).
                        // see for more info: http://snoopwpf.codeplex.com/Thread/View.aspx?ThreadId=236335

                        // sometimes the module names aren't always the same case. compare case insensitive.
                        // see for more info: http://snoopwpf.codeplex.com/workitem/6090

                        foreach (var module in Modules)
                        {
                            if
                            (
                                module.szModule.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
                                module.szModule.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                                module.szModule.StartsWith("wpfgfx", StringComparison.OrdinalIgnoreCase)
                            )
                            {
                                isWpf = true;
                                break;
                            }
                        }
                    }

                    WindowInfo.processIDToValidityMap[process.Id] = isWpf;
                }
                catch (Exception) { }
                return isWpf;
            }
        }
        public Process OwningProcess
        {
            get { return NativeMethods.GetWindowThreadProcess(this.hwnd); }
        }
        public IntPtr HWnd
        {
            get { return this.hwnd; }
        }
        private IntPtr hwnd;
        public string Description
        {
            get
            {
                Process process = this.OwningProcess;
                return process.MainWindowTitle + " - " + process.ProcessName + " [" + process.Id.ToString() + "]";
            }
        }
        public override string ToString()
        {
            return this.Description;
        }

        public void Inject()
        {
            try
            {
                Type interceptor;
                if (IsWpfProcess)
                    interceptor = typeof(Interceptor.Wpf.Setup);
                else
                    interceptor = typeof(Interceptor.WindowsForms.Setup);

                Injector.Launch(this.HWnd, interceptor.Assembly, interceptor.FullName, "Start");
            }
            catch (Exception e)
            {
                Debug.WriteLine("error: " + e.Message);
            }
        }

        private static Dictionary<int, bool> processIDToValidityMap = new Dictionary<int, bool>();
    }

    public static class NativeMethods
    {
        public static IntPtr[] ToplevelWindows
        {
            get
            {
                List<IntPtr> windowList = new List<IntPtr>();
                GCHandle handle = GCHandle.Alloc(windowList);
                try
                {
                    NativeMethods.EnumWindows(NativeMethods.EnumWindowsCallback, (IntPtr)handle);
                }
                finally
                {
                    handle.Free();
                }

                return windowList.ToArray();
            }
        }
        public static Process GetWindowThreadProcess(IntPtr hwnd)
        {
            int processID;
            NativeMethods.GetWindowThreadProcessId(hwnd, out processID);

            try
            {
                return Process.GetProcessById(processID);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private delegate bool EnumWindowsCallBackDelegate(IntPtr hwnd, IntPtr lParam);
        private static bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam)
        {
            ((List<IntPtr>)((GCHandle)lParam).Target).Add(hwnd);
            return true;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct MODULEENTRY32
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            IntPtr modBaseAddr;
            public uint modBaseSize;
            IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        };

        public class ToolHelpHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private ToolHelpHandle()
                : base(true)
            {
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            override protected bool ReleaseHandle()
            {
                return NativeMethods.CloseHandle(handle);
            }
        }

        [Flags]
        public enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F
        }

        [DllImport("user32.dll")]
        private static extern int EnumWindows(EnumWindowsCallBackDelegate callback, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32")]
        public extern static IntPtr LoadLibrary(string librayName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static public extern ToolHelpHandle CreateToolhelp32Snapshot(SnapshotFlags dwFlags, int th32ProcessID);

        [DllImport("kernel32.dll")]
        static public extern bool Module32First(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll")]
        static public extern bool Module32Next(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll", SetLastError = true)]
        static public extern bool CloseHandle(IntPtr hHandle);

        public static Rect GetWindowRect(IntPtr hwnd)
        {
            RECT rect = new RECT();
            GetWindowRect(hwnd, out rect);
            return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    }

    // RECT structure required by WINDOWPLACEMENT structure
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }
    }
}
