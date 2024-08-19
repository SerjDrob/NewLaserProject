// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;

Console.WriteLine("Hello, World!");

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
[GuidAttribute("D698BA94-AEFF-3D4F-9D11-BC6DE81D330B")]
public class ComServer
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
        return 4711;
    }
}
