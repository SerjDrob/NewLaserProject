namespace NewLaserProject.Classes
{
    public record CurrentPierceBlock(
        double MarkSpeed,
        int MarkLoop,
        int PWMFrequency,
        int PWMDutyCycle,
        int HatchWidth
    );
}
