using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RoSatGCS.Utils.Exception;
using static RoSatGCS.Models.SatelliteFunctionFileModel;
using System.Drawing.Printing;
using System.Diagnostics;
using MessagePack;
using NLog;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace RoSatGCS.Models
{
    [MessagePackObject(AllowPrivate = true)]
    public partial class SatelliteFunctionFileModel : ObservableObject
    {
        public enum DataType
        {
            None = 0,
            UInt8 = 1, Int8 = 2, UInt16 = 3, Int16 = 4,
            UInt32 = 5, Int32 = 6, UInt64 = 7, Int64 = 8,
            Integer = 9, Boolean = 10, Float = 11, Double = 12,
            String = 13, ByteBuffer = 14, Enumeration = 15,
            UserDefined = 20
        }

        [MessagePackObject]
        public class Version
        {
            [Key("VMajor")]
            public int Major { get; set; }
            [Key("VMinor")]
            public int Minor { get; set; }
        }

        [MessagePackObject]
        public struct EnumerationValue
        {
            [Key("EVName")]
            public string Name { get; set; }
            [Key("EVDesc")]
            public string Description { get; set; }
            [Key("EVId")]
            public int Id { get; set; }
        }

        [MessagePackObject]
        public struct Enumeration
        {
            [Key("EName")]
            public string Name { get; set; }
            [Key("EDesc")]
            public string Description { get; set; }
            [Key("EValues")]
            public List<EnumerationValue> Values { get; set; }
            [Key("ESize")]
            public int Size { get; set; }
        }

        [MessagePackObject]
        public struct StructValue
        {
            [Key("SVName")]
            public string Name { get; set; }
            [Key("SVDesc")]
            public string Description { get; set; }
            [Key("SVType")]
            public string Type { get; set; }
            [Key("SVIsArray")]
            public bool Is_Array { get; set; }
            [Key("SVSize")]
            public int Size { get; set; }
        }

        [MessagePackObject]
        public struct Struct
        {
            [Key("SName")]
            public string Name { get; set; }
            [Key("SDesc")]
            public string Description { get; set; }
            [Key("SSize")]
            public int Size { get; set; }
            [Key("SValues")]
            public List<StructValue> Values { get; set; }
        }

        [MessagePackObject]
        public struct MethodValue
        {
            [Key("MVName")]
            public string Name { get; set; }
            [Key("MVDesc")]
            public string Description { get; set; }
            [Key("MVType")]
            public string Type { get; set; }
            [Key("MVIsArray")]
            public bool Is_Array { get; set; }
            [Key("MVSize")]
            public int Size { get; set; }
        }

        [MessagePackObject]
        public struct Method
        {
            [Key("MName")]
            public string Name { get; set; }
            [Key("MDesc")]
            public string Description { get; set; }
            [Key("MId")]
            public int Id { get; set; }
            [Key("MIn")]
            public List<MethodValue> In { get; set; }
            [Key("MOut")]
            public List<MethodValue> Out { get; set; }
        }

        [MessagePackObject]
        public struct FunctionFileStructure
        {
            [Key("Package")]
            public string Package { get; set; }
            [Key("Interface")]
            public string Interface { get; set; }
            [Key("Description")]
            public string Description { get; set; }
            [Key("Id")]
            public int Id { get; set; }
            [Key("Version")]
            public Version Version { get; set; }
            [Key("ListEnum")]
            public List<Enumeration> Enumeration { get; set; }
            [Key("ListStruct")]
            public List<Struct> Struct { get; set; }
            [Key("ListMethod")]
            public List<Method> Method { get; set; }
        }
        public SatelliteFunctionFileModel() {

        }

        #region fields

        [Key("filename")]
        private string _fileName = "";
        [Key("filepath")]
        private string _filePath = "";
        [Key("strueture")]
        private FunctionFileStructure? _structure;
        [Key("visibility")]
        private bool _visibility = true;
        [Key("initializing")]
        private bool _initializing = true;
        [Key("valid")]
        private bool _valid = false;

        #endregion

        #region properties

        [Key("FileName")]
        public string Name
        {
            get => _fileName;
        }
        [Key("FilePath")]
        public string FilePath
        {
            get => _filePath;
        }

        [Key("Structure")]
        public FunctionFileStructure? Structure
        {
            get => _structure;
        }

        [Key("Visibility")]
        public bool Visibility { get => _visibility; set { SetProperty(ref _visibility, value); } }

        [Key("Initializing")]
        public bool Initializing { get => _initializing; set { SetProperty(ref _initializing, value); } }

        [Key("Valid")]
        public bool Valid { get => _valid; set { SetProperty(ref _valid, value); } }

        #endregion
        public void Initialize(string path)
        {
            Initializing = true;
            FileCheck(path);
            _fileName = Path.GetFileNameWithoutExtension(path);
            _filePath = path;

            if (!Load(out string error))
            {
                throw new InvalidFunctionFileException(error);
            }
        }

        private static void FileCheck(string path)
        {
            if (!Path.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
            if (Path.GetExtension(path) != ".json")
            {
                throw new FileFormatException("File must be .json file");
            }
        }

        private readonly static JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private bool Load(out string error)
        {

            using (StreamReader r = new(_filePath))
            {
                string json = r.ReadToEnd();
                var data = JsonSerializer.Deserialize<FunctionFileStructure>(json, jsonOptions);

                // Validate Metadata    
                if (string.IsNullOrEmpty(data.Package)) { error = "Package is missing or empty."; return false; }
                if (string.IsNullOrEmpty(data.Interface)) { error = "Interface is missing or empty."; return false; }
                if (data.Version == null || data.Version.Major < 0 || data.Version.Minor < 0)
                {
                    error = "Version is invalid.";
                    return false;
                }

                // Validate Enumeration
                foreach (var enumeration in data.Enumeration)
                {
                    if (string.IsNullOrEmpty(enumeration.Name))
                    {
                        error = "Enumeration Name is missing.";
                        return false;
                    }

                    if (enumeration.Values.Count == 0)
                    {
                        error = "Enumeration Value is not defined.";
                        return false;
                    }

                    foreach (var value in enumeration.Values)
                    {
                        if (string.IsNullOrEmpty(value.Name))
                        {
                            error = "Enumeration Value Name is missing.";
                            return false;
                        }
                        if (value.Id < 0)
                        {
                            error = "Enumeration Value Id must be positive.";
                            return false;
                        }
                    }
                }

                // Validate Struct
                foreach (var structure in data.Struct)
                {
                    if (string.IsNullOrEmpty(structure.Name))
                    {
                        error = "Struct Name is missing.";
                        return false;
                    }

                    if (structure.Values.Count == 0)
                    {
                        error = "Struct Value is not defined.";
                        return false;
                    }

                    foreach (var value in structure.Values)
                    {
                        
                        if (string.IsNullOrEmpty(value.Name) || value.Size < 0)
                        {
                            error = "Struct Value is invalid.";
                            return false;
                        }
                        if (value.Size < 0)
                        {
                            error = "Struct Value Size must be positive";
                            return false;
                        }
                    }
                }

                // Validate Method
                foreach (var method in data.Method)
                {
                    if (string.IsNullOrEmpty(method.Name))
                    {
                        error = "Method Name is missing.";
                        return false;
                    }

                    if (method.Id < 0)
                    {
                        error = "Method Id must be positive";
                        return false;
                    }

                    foreach (var input in method.In)
                    {
                        if (string.IsNullOrEmpty(input.Name))
                        {
                            error = "Method Input Name is missing.";
                            return false;
                        }
                        if (!Enum.TryParse(input.Type, ignoreCase: false, out DataType _))
                        {
                            var sp = input.Type.Trim().Split('|');
                            
                            if (sp.Length != 2 || sp[0] != "UserDefined" || sp[1].Length == 0)
                            {
                                error = "Not supported input type:" + input.Type;
                                return false;
                            }
                        }
                    }

                    foreach (var output in method.Out)
                    {
                        if (string.IsNullOrEmpty(output.Name))
                        {
                            error = "Method Output Name is missing.";
                            return false;
                        }
                        if (!Enum.TryParse(output.Type, ignoreCase: false, out DataType _))
                        {
                            var sp = output.Type.Split('|');

                            if (sp.Length != 2 || sp[0] != "UserDefined" || sp[1].Length == 0)
                            {
                                error = "Not supported output type:" + output.Type;
                                return false;
                            }
                        }
                    }
                }

                _structure = data;

            }
            error = "";
            Initializing = false;
            return true;
        }

    }
}
