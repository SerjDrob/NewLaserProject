using System.Runtime.InteropServices;

namespace ComApp;


[ComVisible(true)]
[Guid("0D3BDB92-FA40-4AEC-8DC8-A344A2264BF9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IServer
{
    public void TestMe();
    public int TestMeWithResult(string text);
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
[Guid("AEECCFE9-9D03-4532-AEB7-CB78A5AD21E2")]
public class ComServer:IServer
{
    /// <summary>
    /// Default constructor - necessary for using with COM
    /// </summary>
    public ComServer()
    {
    }

    /// <summary>
    /// Test method to be called by COM sonsumer
    /// </summary>
    public void TestMe()
    {
        Console.WriteLine("Hello from the 64-bit world!");
    }

    /// <summary>
    /// Test function to be called by COM consumer
    /// </summary>
    /// <param name="text">Any text message</param>
    /// <returns>4711 fixed returncode</returns>
    public int TestMeWithResult(string text)
    {
        Console.WriteLine("Hello from the 64-bit world, you provided the text:\n" + text);
        return 47131;
    }
}
