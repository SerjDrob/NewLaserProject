using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace NewLaserProject.Classes
{
    internal class Scale
    {
        public Scale(uint numerator, uint denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        public uint Numerator { get; set; } 
        public uint Denominator { get; set; } 
        public float Ratio { get => Numerator / Denominator; }
        public override string ToString()
        {
            return $"{Numerator} : {Denominator}";
        }
        public static implicit operator float(Scale scale) => scale?.Ratio ?? 1f;
    }

}
