using System;
using System.Collections.Generic;
using System.Text;
using WinShell;
using System.IO;

namespace FMListProperties
{
    class Program
    {

        const string c_helpText = @"FMListProperties:
Lists all Windows Property System properties on a file.

Syntax: FMListProperties: [flags] [filenames]

Flags:
   -h  Show this help text.
   -c  Use Canonical names. By default uses display names.
   -b  Show Both canonical and display names.
   -f  Show flags.
   -k  Include PropKeys as well as names.
   -l  Show source code license.

Filenames:
   One or more filenames must be specified. Wildcards may be included.

Info:
   Source code and latest version at https://github.com/FileMeta/FMListProperties
   Source code available under BSD 3-Clause License.";

        const string c_licenseText = @"BSD 3-Clause License

Copyright (c) 2018, Brandt Redd
All rights reserved.
https://github.com/FileMeta/FMListProperties

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

* Neither the name of the copyright holder nor the names of its
  contributors may be used to endorse or promote products derived from
  this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ""AS IS""
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.";

        static PropertySystem s_propSys;
        static bool s_useCanonicalNames = false;
        static bool s_useBothNames = false;
        static bool s_includePropKeys = false;
        static bool s_includeFlags = false;

        static void Main(string[] args)
        {
            var paths = new List<string>();

            bool showHelp = (args.Length == 0);
            bool showLicense = false;

            // Get flags first
            foreach (var arg in args)
            {
                switch (arg.ToLower())
                {
                    case "-c":
                        s_useCanonicalNames = true;
                        break;

                    case "-b":
                        s_useBothNames = true;
                        break;

                    case "-k":
                        s_includePropKeys = true;
                        break;

                    case "-f":
                        s_includeFlags = true;
                        break;

                    case "-l":
                        showLicense = true;
                        break;

                    case "-h":
                    case "-?":
                        showHelp = true;
                        break;

                    default:
                        paths.Add(arg);
                        break;
                }
            }

            if (showHelp)
            {
                Console.WriteLine(c_helpText);
            }
            else if (showLicense)
            {
                Console.WriteLine(c_licenseText);
            }
            else
            {
                WriteProperties(paths);
            }

            Win32Interop.ConsoleHelper.PromptAndWaitIfSoleConsole();
        }

        static void WriteProperties(IEnumerable<string> paths)
        {
            try
            {
                s_propSys = new PropertySystem();

                foreach (var path in paths)
                {
                    try
                    {
                        string folder = Path.GetDirectoryName(path);
                        if (string.IsNullOrEmpty(folder)) folder = Environment.CurrentDirectory;
                        string pattern = Path.GetFileName(path);
                        foreach (var filename in Directory.EnumerateFiles(folder, pattern, SearchOption.TopDirectoryOnly))
                        {
                            Console.WriteLine(filename);
                            WriteProperties(filename);
                            Console.WriteLine();
                        }
                    }
                    catch (Exception err)
                    {
#if DEBUG
                        Console.WriteLine(err.ToString());
#else
                        Console.WriteLine(err.Message);
#endif
                    }
                }
            }
            finally
            {
                if (s_propSys != null)
                    s_propSys.Dispose();
                s_propSys = null;
            }
        }

        static void WriteProperties(string filename)
        {
            var properties = new List<Property>();

            using (var ps = PropertyStore.Open(filename))
            {
                int n = ps.Count;
                for (int i = 0; i < n; ++i)
                {
                    properties.Add(new Property(s_propSys, ps, i));
                }
            }

            {
                var isom = FileMeta.IsomCoreMetadata.TryOpen(filename);
                if (isom != null)
                {
                    using (isom)
                    {
                        properties.Add(new Property("Isom.Brand", isom.MajorBrand));
                        if (isom.CreationTime != null)
                        {
                            properties.Add(new Property("Isom.CreationTime", isom.CreationTime?.ToString("o")));
                        }
                        if (isom.ModificationTime != null)
                        {
                            properties.Add(new Property("Isom.ModificationTime", isom.ModificationTime?.ToString("o")));
                        }
                    }
                }
            }

            properties.Sort(CompareProperties);

            foreach(var prop in properties)
            {
                Console.Write("   ");
                if (s_includeFlags)
                {
                    Console.Write(prop.Flags);
                    Console.Write(' ');
                }

                if (s_includePropKeys)
                {
                    Console.Write($"{prop.PropertyKey.ToString(),-45}");
                }

                if (s_useBothNames)
                {
                    Console.Write($"{prop.CanonicalName + '(' + prop.DisplayName + "):",-45} ");
                }
                else if (s_useCanonicalNames)
                {
                    Console.Write($"{prop.CanonicalName + ':',-45} ");
                }
                else
                {
                    Console.Write($"{prop.DisplayName + ':',-45} ");
                }

                Console.WriteLine(prop.Value);
            }
        }

        static int CompareProperties(Property a, Property b)
        {
            int cmp = string.Compare(a.CanonicalName, b.CanonicalName, StringComparison.OrdinalIgnoreCase);
            if (cmp != 0) return cmp;

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

    }

    class Property
    {
        public PROPERTYKEY PropertyKey { get; set; }
        public string DisplayName { get; set; }
        public string CanonicalName { get; set; }
        public string Value { get; set; }
        public string Flags { get; set; }

        public Property(PropertySystem propSys, PropertyStore ps, int index)
        {
            var pk = ps.GetAt(index);
            PropertyKey = pk;

            var propDef = propSys.GetPropertyDescription(pk);

            if (propDef != null)
            {
                // Names
                DisplayName = propDef.DisplayName;
                CanonicalName = propDef.CanonicalName;
                if (string.IsNullOrEmpty(DisplayName))
                {
                    DisplayName = CanonicalName;
                    if (string.IsNullOrEmpty(DisplayName))
                    {
                        DisplayName = pk.ToString();
                    }
                }
                if (string.IsNullOrEmpty(CanonicalName))
                {
                    CanonicalName = DisplayName;
                }

                // Flags
                {
                    var f = propDef.TypeFlags;
                    char[] cf = new char[4];
                    cf[0] = (f & PROPDESC_TYPE_FLAGS.PDTF_ISSYSTEMPROPERTY) == 0 ? '-' : 'S';
                    cf[1] = (f & PROPDESC_TYPE_FLAGS.PDTF_ISINNATE) == 0 ? '-' : 'I';
                    cf[2] = (f & PROPDESC_TYPE_FLAGS.PDTF_CANBEPURGED) == 0 ? '-' : 'P';
                    cf[3] = (f & PROPDESC_TYPE_FLAGS.PDTF_ISVIEWABLE) == 0 ? '-' : 'V';
                    Flags = new string(cf);
                }
            }
            else
            {
                CanonicalName = DisplayName = pk.ToString();
                Flags = "????";
            }

            Value = ValueToString(ps.GetValue(pk), pk);
        }

        public Property(string displayName, string value)
        {
            PropertyKey = new PROPERTYKEY(Guid.Empty, 0);
            DisplayName = displayName;
            CanonicalName = displayName;
            Value = value;
            Flags = "----";
        }

        static PROPERTYKEY s_pkDuration = new PROPERTYKEY("64440490-4c8b-11d1-8b70-080036b11a03",3);

        static string ValueToString(object value, PROPERTYKEY pk)
        {
            if (value == null) return "(null)";

            {
                var array = value as Array;
                if (array != null)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (object obj in array)
                    {
                        if (sb.Length > 0)
                            sb.Append("; ");
                        sb.Append(obj.ToString());
                    }
                    return sb.ToString();
                }
            }

            if (value is DateTime)
            {
                var dt = (DateTime)value;
                if (dt.Kind == DateTimeKind.Local)
                    dt = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified); // Prevent reporting local timezone on output string.
                return dt.ToString("o");
            }

            if (pk.Equals(s_pkDuration))
            {
                var ts = TimeSpan.FromTicks((long)(UInt64)value);
                return ts.ToString("c");
            }

            return value.ToString();
        }

    }
}
