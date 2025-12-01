using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace PdqHash.Hashing.Video;

public class VPdqHash : IParsable<VPdqHash>
{
    public required PdqHash256 Hash { get; init; }
    public required int? Distance 
    { 
        get; 
        init => field = value is null or >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(Distance), "Distance must be non-negative"); 
    }
    public required int Frame 
    { 
        get; 
        init => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(Frame), "Frame must be non-negative"); 
    }
    public required TimeSpan? Timestamp { get; init; }

    public static VPdqHash Parse(string s, IFormatProvider? provider)
    {
        var args = s.Split(",");

        int frame = 0;
        int? distance = null;
        TimeSpan? timestamp = null;
        PdqHash256? hash = null;

        if (args.Length > 0 && int.TryParse(args[0], out var d))
        {
            frame = d;
        }
        if (args.Length > 1)
        {
            hash = PdqHash256.fromHexString(args[1]);
        }

        if (args.Length > 2 && int.TryParse(args[2], out d))
        {
            distance = d;
        }
        if (args.Length > 3 && TimeSpan.TryParse(args[2], out var ts))
        {
            timestamp = ts;
        }

        return new VPdqHash
        {
            Distance = distance,
            Frame = frame,
            Hash = hash ?? throw new FormatException("Could not parse vPDQ string"),
            Timestamp = timestamp,
        };
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out VPdqHash result)
    {
        if (string.IsNullOrEmpty(s))
        {
            result = null;
            return false;
        }

        result = Parse(s, provider);
        return true;
    }


    public static VPdqHash FromBytes(ReadOnlySpan<byte> line)
    {
        int frame = 0;
        int? distance = null;
        TimeSpan? timestamp = null;
        PdqHash256? hash = null;

        var value = "";
        var commaCount = 0;

        for (var i = 0; i < line.Length; i++)
        {
            switch (line[i])
            {
                case (byte)',':
                    switch (commaCount)
                    {
                        case 0:
                            frame = int.Parse(value, CultureInfo.InvariantCulture);
                            break;
                        case 1:
                            hash = PdqHash256.fromHexString(value);
                            break;
                        case 2:
                            distance = int.Parse(value, CultureInfo.InvariantCulture);
                            break;
                        case 3:
                            timestamp = TimeSpan.Parse(value, CultureInfo.InvariantCulture);
                            break;
                        default:
                            throw new FormatException("Unparsable line, too many commas detected. Valid format is: <frame>,<hash>,<distance>,<timestamp>");
                    }

                    value = "";
                    commaCount++;
                    break;
                default:
                    value += (char)line[i];
                    break;
            }
        }

        return new VPdqHash
        {
            Distance = distance,
            Frame = frame,
            Hash = hash ?? throw new FormatException("Could not parse vPDQ string"),
            Timestamp = timestamp,
        };
    }
}