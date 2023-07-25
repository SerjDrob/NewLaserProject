// See https://aka.ms/new-console-template for more information
var xline = new CoeffLine((18, 17), (77, 32), (127, 59), (196, 70));

Console.WriteLine(xline[10]);
Console.WriteLine(xline[40]);
Console.WriteLine(xline[102]);
Console.WriteLine(xline[155]);
Console.WriteLine(xline[223]);


class CoeffLine
{
    private readonly double[] originPoints;
    private readonly double[] derivedPoints;

    public CoeffLine(params (double orig, double derived)[] values)
    {
        if (values.Length < 2) throw new ArgumentException("points count must be no less than two");
        var res = values.OrderBy(val => val.orig);
        originPoints = res.Select(val => val.orig).ToArray();
        originPoints = res.Select(val => val.derived).ToArray();
    }

    public double this[double par]
    {
        get
        {
            var range = GetRange(originPoints, par);
            var k = Math.Atan((originPoints[range.End] - originPoints[range.Start]) / (derivedPoints[range.End] - derivedPoints[range.Start]));
            return k * par;
        }
    }

    private static Range GetRange(double[] points, double checkVal)
    {
        if (checkVal < points[0] || checkVal > points[^1] || points.Length == 2) return new Range(0, ^1);
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (checkVal > points[i] && checkVal < points[i + 1]) return new Range(i, i + 1);
        }
        throw new InvalidOperationException("A range was not found");
    }
}