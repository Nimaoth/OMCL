using System;
using System.Text;
using OMCL.Data;
using OMCL.Serialization;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
                var file = args.Length > 0 ? args[0] : @"D:\Programming\C#\OMCL\examples\generated.omcl";

                var parser = Parser.FromFile(file);
                var configItem = parser.ParseObject();
                var config = configItem.AsObject();

                
                var sb = new StringBuilder();
                var s = Serializer.ToStringBuilder(sb);
                s.Serialize(config);

                System.Console.WriteLine(sb);
            }
            catch (Exception e) {
                System.Console.Error.WriteLine($"Failed to process file: {e.Message}\n{e}");
            }
        }
    }
}
