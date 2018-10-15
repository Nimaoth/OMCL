using System;
using System.IO;
using System.Text;
using OMCL.Data;
using OMCL.Serialization;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            RunTests();
            try {
                var file = args.Length > 0 ? args[0] : @"D:\Programming\C#\OMCL\examples\generated.omcl";

                var parser = Parser.FromFile(file);
                var config = parser.ParseObject();

                
                var sb = new StringBuilder();
                var s = Serializer.ToStringBuilder(sb);
                s.Serialize(config);

                System.Console.WriteLine(sb);
            }
            catch (Exception e) {
                System.Console.Error.WriteLine($"Failed to process file: {e.Message}\n{e}");
            }
        }

        public static void RunTests() {
            // correct tests
            var testsPath = @"examples\tests\correct";

            foreach (var path in Directory.EnumerateFiles(testsPath)) {
                try {
                    var parser = Parser.FromFile(path);
                    parser.ParseObject();
                }
                catch (Exception e) {
                    Console.Error.WriteLine($"Test failed: {path}: {e.Message}");
                }
            }

            // incorrect tests
            foreach (var path in Directory.EnumerateFiles( @"examples\tests\incorrect")) {
                var filename = Path.GetFileNameWithoutExtension(path);
                var parts = filename.Split('_');
                var line = int.Parse(parts[1]);
                var column = int.Parse(parts[2]);
                try {
                    var parser = Parser.FromFile(path);
                    parser.ParseItem();

                    Console.Error.WriteLine($"Test failed: {path}: Expected error at ({line}:{column})");
                }
                catch (OMCLParserError e) {
                    if (e.Location.Line != line || e.Location.Column != column)
                        Console.Error.WriteLine($"Test failed: {path}: Expected error at ({line}:{column}), got error at ({e.Location})\n{e.Message}");
                }
                catch (Exception e) {
                    Console.Error.WriteLine($"Test failed: {path}: {e.Message}");
                }
            }
        }
    }
}
