
using System.Globalization;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace PdqHash;

public struct PdqHash256
{
    private static readonly ObjectPool<StringBuilder> sbPool = new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy());

    public const int HASH256NUMSLOTS = 16;
    const int HASH256_HEX_NUM_NYBBLES = 4 * HASH256NUMSLOTS;
    private readonly int[] w;
    private Random _rnd;

    public PdqHash256()
    {
        w = new int[HASH256NUMSLOTS];
        _rnd = new Random();
    }

    public static int getNumWords => HASH256NUMSLOTS;

    public override string ToString()
    {
        var sb = sbPool.Get();
        var i = HASH256NUMSLOTS - 1;
        while (i >= 0)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0:x4}", w[i] & 0xFFFF);
            i--;
        }

        var result = sb.ToString();
        sbPool.Return(sb);
        return result;
    }

    public readonly void Clear()
    {
        for (var i = 0; i < HASH256NUMSLOTS; i++)
        {
            w[i] = 0;
        }
    }

    public void setAll()
    {
        for (var i = 0; i < HASH256NUMSLOTS; i++)
        {
            w[i] = 0xFFFF;
        }
    }

    public int hammingNorm()
    {
        int n = 0;
        int i = 0;
        while (i < HASH256NUMSLOTS)
        {
            n += hammingNorm16(this.w[i]);
            i++;
        }
        return n;
    }

    public int hammingDistance(PdqHash256 other)
    {
        var n = 0;
        for (var i = 0; i < HASH256NUMSLOTS; i++)
        {
            n += hammingNorm16(w[i] ^ other.w[i]);
        }
        return n;
    }

    public bool hammingDistanceLE(PdqHash256 that, int d)
    {
        var e = 0;
        for (var i = 0; i < HASH256NUMSLOTS; i++)
        {
            e += hammingNorm16(w[i] ^ that.w[i]);
            if (e > d)
            {
                return false;
            }
        }
        return true;
    }

    public void setBit(int k)
    {
        this.w[(k & 255) >> 4] |= 1 << (k & 15);
    }

    public void flipBit(int k)
    {
        this.w[(k & 255) >> 4] ^= 1 << (k & 15);
    }


    public static PdqHash256 operator ^(PdqHash256 left, PdqHash256 right)
    {
        // XOR
        var rv = new PdqHash256();
        var i = 0;
        while (i < HASH256NUMSLOTS)
        {
            rv.w[i] = (int)(left.w[i] ^ right.w[i]);
            i++;
        }
        return rv;
    }

    public static PdqHash256 operator &(PdqHash256 left, PdqHash256 right)
    {
        var rv = new PdqHash256();
        var i = 0;
        while (i < HASH256NUMSLOTS)
        {
            rv.w[i] = (int)(left.w[i] & right.w[i]);
            i++;
        }
        return rv;
    }

    public static bool operator ==(PdqHash256 left, PdqHash256 right)
    {
        for (var i = 0; i < HASH256NUMSLOTS; i++)
        {
            if (left.w[i] != right.w[i])
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is PdqHash256 hash)
        {
            return hash.w.SequenceEqual(w);
        }

        return false;
    }

    public override int GetHashCode() => w.GetHashCode();


    public static bool operator !=(PdqHash256 left, PdqHash256 right)
    {
        return (left == right) is false;
    }


    public static bool operator >(PdqHash256 left, PdqHash256 right)
    {
        for (var i = 0; i < HASH256NUMSLOTS; i++)
        {
            if (left.w[i] > right.w[i])
            {
                return true;
            }
            else if (left.w[i] < right.w[i])
            {
                return false;
            }
        }
        return false;
    }


    public static bool operator <(PdqHash256 left, PdqHash256 right)
    {
        for (var i = 0; i < HASH256NUMSLOTS; i++)
        {
            if (left.w[i] < right.w[i])
            {
                return true;
            }
            else if (left.w[i] > right.w[i])
            {
                return false;
            }
        }
        return false;
    }

    public static PdqHash256 operator |(PdqHash256 left, PdqHash256 right)
    {
        // OR
        var rv = new PdqHash256();
        var i = 0;
        while (i < HASH256NUMSLOTS)
        {
            rv.w[i] = (int)(left.w[i] | right.w[i]);
            i++;
        }
        return rv;
    }


    public PdqHash256 bitwiseNOT()
    {
        // NOT
        var rv = new PdqHash256();
        var i = 0;
        while (i < HASH256NUMSLOTS)
        {
            rv.w[i] = ~w[i] & 0xFFFF;
            i++;
        }
        return rv;
    }

    public string dumpBits()
    {
        var i = HASH256NUMSLOTS - 1;
        List<string> str = [];

        while (i >= 0)
        {
            var word = this.w[i] & 0xFFFF;
            var j = 15;
            List<string> bits = [];
            while (j >= 0)
            {
                if ((word & (1 << j)) != 0)
                {
                    bits.Add("1");
                }
                else
                {
                    bits.Add("0");
                }
                j--;
            }
            i--;
            str.Add(string.Join(" ", bits));
        }
        return string.Join(Environment.NewLine, str);
    }

    public IEnumerable<byte> ToBits()
    {
        var i = HASH256NUMSLOTS - 1;
        List<byte> bits = [];

        while (i >= 0)
        {
            var word = this.w[i] & 0xFFFF;
            var j = 15;
            while (j >= 0)
            {
                if ((word & (1 << j)) != 0)
                {
                    bits.Add(1);
                }
                else
                {
                    bits.Add(0);
                }
                j--;
            }
            i--;
        }
        return bits;
    }

    public string dumpBitsAcross()
    {
        var i = HASH256NUMSLOTS - 1;
        List<string> str = [];
        var word = this.w[i] & 0xFFFF;
        var j = 15;
        while (i >= 0)
        {
            while (j >= 0)
            {
                if ((word & (1 << j)) != 0)
                {
                    str.Add("1");
                }
                else
                {
                    str.Add("0");
                }
                j--;
            }
            i--;
        }
        return string.Join(" ", str);
    }

    public string dumpWords()
    {
        return string.Join(",", w.Reverse().Select(w => w.ToString(CultureInfo.InvariantCulture)));
    }

    public int[] Words() => w.ToArray();

    public PdqHash256 Clone()
    {
        var rv = new PdqHash256();
        var i = 0;
        while (i < HASH256NUMSLOTS)
        {
            rv.w[i] = this.w[i];
            i++;
        }
        return rv;
    }


    /// <summary>
    /// Flips some number of bits randomly, with replacement.  (I.e. not all
    /// flipped bits are guaranteed to be in different positions; if you pass
    /// argument of 10 then maybe 2 bits will be flipped and flipped back, and
    /// only 6 flipped once.)
    /// </summary>
    public PdqHash256 fuzz(int numErrorBits)
    {
        var rv = this.Clone();


        var i = 0;
        while (i < numErrorBits)
        {
            rv.flipBit(_rnd.Next(0, 255));
            i++;
        }
        return rv;
    }

    private static int hammingNorm16(int v)
    {
        return int.PopCount(v & 0xFFFF);
    }

    public string toHexString() => this.ToString();

    public static PdqHash256 fromHexString(string hexString)
    {
        if (hexString.Length != HASH256_HEX_NUM_NYBBLES)
        {
            throw new FormatException("Incorrect hex length for pdq hash");
        }

        var rv = new PdqHash256();
        var i = HASH256NUMSLOTS;

        for (var x = 0; x < hexString.Length; x += 4)
        {
            i--;
            rv.w[i] = int.Parse(hexString[x..(x + 4)], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return rv;
    }

}