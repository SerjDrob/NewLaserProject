namespace NewLaserProject.Classes
{
    public class Scale
    {
        public Scale()
        {
            
        }
        public Scale(uint numerator, uint denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }
        public static Scale ThousandToOne => new(1000, 1);
        public static Scale HundredToOne => new(100, 1); 
        public static Scale OneToOne => new(1, 1);
        public uint Numerator
        {
            get; set;
        }
        public uint Denominator
        {
            get; set;
        }
        public float Ratio => Numerator / Denominator;
        public override string ToString()
        {
            return $"{Numerator} : {Denominator}";
        }
        public static implicit operator float(Scale scale) => scale?.Ratio ?? 1f;
    }
}
