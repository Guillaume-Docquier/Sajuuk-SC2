using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;

namespace Bot;

public static class ImageDataUtils {
    public static ByteString CreateByeString(IReadOnlyCollection<bool> booleans) {
        var byteList = new List<byte>();
        for (var i = 0; i < booleans.Count; i += 8) {
            // Each byte represents 8 grid cells
            byteList.Add(BooleansToByte(booleans.Skip(i).Take(8)));
        }

        return ByteString.CopyFrom(byteList.ToArray());
    }

    public static ByteString CreateByeString(IReadOnlyCollection<float> floats) {
        var byteList = floats.Select(FloatToByte);

        return ByteString.CopyFrom(byteList.ToArray());
    }

    public static bool[] ByteToBoolArray(byte byteValue)
    {
        // Each byte represents 8 grid cells
        var values = new bool[8];

        values[7] = (byteValue & 1) != 0;
        values[6] = (byteValue & 2) != 0;
        values[5] = (byteValue & 4) != 0;
        values[4] = (byteValue & 8) != 0;
        values[3] = (byteValue & 16) != 0;
        values[2] = (byteValue & 32) != 0;
        values[1] = (byteValue & 64) != 0;
        values[0] = (byteValue & 128) != 0;

        return values;
    }

    public static byte BooleansToByte(IEnumerable<bool> eightBooleans) {
        var outByte = (byte)0;

        // For some reason, the API encodes the booleans in reverse order
        var reversedBooleans = eightBooleans.Reverse().ToList();
        for (var i = 0; i < reversedBooleans.Count; i++) {
            SetBit(ref outByte, i, reversedBooleans[i]);
        }

        return outByte;
    }

    private static void SetBit(ref byte inByte, int bitIndex, bool value)
    {
        if (value) {
            inByte = (byte)(inByte | (1 << bitIndex));
        }
        else {
            inByte = (byte)(inByte & ~(1 << bitIndex));
        }
    }

    public static float ByteToFloat(byte byteValue) {
        // Computed from 3 unit positions and 3 height map bytes
        // Seems to work fine
        return 0.125f * byteValue - 15.888f;
    }

    public static byte FloatToByte(float inFloat) {
        return (byte)((inFloat + 15.888f) / 0.125f);
    }
}
