using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using RoSatGCS.Utils.Query;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.Models
{

    public static class DataTypeInfo
    {
        public static int SizeOf(DataType t) => t switch
        {
            DataType.Int8 or DataType.UInt8 or DataType.Boolean => 1,
            DataType.Int16 or DataType.UInt16 => 2,
            DataType.Int32 or DataType.UInt32 or DataType.Float => 4,
            DataType.Int64 or DataType.UInt64 or DataType.Double => 8,
            DataType.String or DataType.ByteArray => 0, // variable/unsupported for plotting
            _ => throw new ArgumentOutOfRangeException(nameof(t))
        };
    }


    public partial class PlotData : ObservableObject, IChartEntity
    {
        [ObservableProperty]
        private DateTime _dateTime;
        [ObservableProperty]
        private DataType _plotDataType;
        [ObservableProperty]
        private byte[] _data = [];
        [ObservableProperty]
        private double _value;

        public Coordinate Coordinate { get; protected set; }

        public ChartEntityMetaData? MetaData { get; set; }

        partial void OnDataChanged(byte[] value) => DecodeAndUpdate();
        partial void OnPlotDataTypeChanged(DataType value) => DecodeAndUpdate();
        partial void OnDateTimeChanged(DateTime value) => UpdateCoordinate();

        private void DecodeAndUpdate()
        {
            Value = TryDecodeDouble(PlotDataType, Data, out var v) ? v : 0.0;
            UpdateCoordinate();
        }

        private void UpdateCoordinate()
        {
            var pt = new DateTimePoint(DateTime, Value);
            Coordinate = pt.Coordinate;
        }

        private static bool TryDecodeDouble(DataType type, byte[] data, out double value)
        {
            value = 0;
            if (data is null) return false;

            int need = DataTypeInfo.SizeOf(type);
            if (need > 0 && data.Length < need) return false;

            var span = data.AsSpan();

            switch (type)
            {
                case DataType.Int8: value = (sbyte)span[0]; return true;
                case DataType.UInt8: value = span[0]; return true;

                case DataType.Boolean: value = span[0] != 0 ? 1 : 0; return true;

                case DataType.Int16: value = BinaryPrimitives.ReadInt16LittleEndian(span); return true;
                case DataType.UInt16: value = BinaryPrimitives.ReadUInt16LittleEndian(span); return true;

                case DataType.Int32: value = BinaryPrimitives.ReadInt32LittleEndian(span); return true;
                case DataType.UInt32: value = BinaryPrimitives.ReadUInt32LittleEndian(span); return true;

                case DataType.Int64: value = BinaryPrimitives.ReadInt64LittleEndian(span); return true;
                case DataType.UInt64:
                    value = BinaryPrimitives.ReadUInt64LittleEndian(span); return true;

                case DataType.Float:
                    {
                        var u = BinaryPrimitives.ReadUInt32LittleEndian(span);
                        value = BitConverter.Int32BitsToSingle((int)u);
                        return true;
                    }
                case DataType.Double:
                    {
                        var u = BinaryPrimitives.ReadUInt64LittleEndian(span);
                        value = BitConverter.Int64BitsToDouble((long)u);
                        return true;
                    }

                // not plotted:
                case DataType.String:
                case DataType.ByteArray:
                    return false;

                default: return false;
            }
        }
    }
}
