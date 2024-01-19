namespace NewLaserProject.Classes
{
    public class Scale
    {
        public Scale(uint numerator, uint denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
            Ratio = (float)numerator / denominator;
        }
        public static Scale ThousandToOne => new(1000, 1);
        public static Scale HundredToOne => new(100, 1); 
        public static Scale OneToOne => new(1, 1);
        public static Scale OneToHundred => new(1, 100);
        public static Scale OneToThousand => new(1, 1000);

        public uint Numerator
        {
            get; init;
        }
        public uint Denominator
        {
            get; init;
        }
        public float Ratio { get; init; } 
        public override string ToString()
        {
            return $"{Numerator} : {Denominator}";
        }
        public static implicit operator float(Scale scale) => scale?.Ratio ?? 1f;
    }
}
