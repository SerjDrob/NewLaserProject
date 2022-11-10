using System.Windows.Input;

namespace NewLaserProject.ViewModels
{
    /// <summary>
    /// Arguments for KeyProcessorCommands
    /// </summary>
    /// <param name="KeyEventArgs">event arguments</param>
    /// <param name="IsKeyDown">true if there's KeyDown event, false if there's KeyUp event</param>
    internal record KeyProcessorArgs(KeyEventArgs KeyEventArgs, bool IsKeyDown);
}

