// See https://aka.ms/new-console-template for more information
using System.Reflection;
using System.Runtime.InteropServices;




Console.WriteLine("Hello, World!");
var ComType = Type.GetTypeFromProgID("ComLmc.ComLmc");

if(ComType == null) return;

var ComObject = Activator.CreateInstance(ComType);

object[] methodArgs = [0];

var result = (int)ComType.InvokeMember("Initialize",
                                       BindingFlags.InvokeMethod, null,
                                       ComObject, ["C:\\Users\\Serj\\source\\repos\\NewLaserProject\\ComLmc\\bin\\Debug\\net8.0", true]);

Console.WriteLine("Result is: " + result);

// Don't forget to release the late bound COM object,
// otherwise the surrogate process (dllhost.exe) would live further ...
if (Marshal.IsComObject(ComObject))
    Marshal.ReleaseComObject(ComObject);

Console.ReadKey();
