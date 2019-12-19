using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Mq.Migration.Test
{
    static internal class TestHelper
    {
        static public XDocument LoadResourceDocument(string name)
        {
            if (name == null) throw new System.ArgumentNullException(nameof(name));

            using (StreamReader reader = new StreamReader(
                Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream($"Mq.Migration.Test.Assets.{name}"),
                Encoding.UTF8))
            {
                return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            }
        }
    }
}
