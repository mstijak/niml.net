using PetaTest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Niml.Tests
{
    [TestFixture]
    class DevTests
    {
//        [Test]
//        public void Test1()
//        {
//            var niml = @"
//document+
//content+
//p Paragraph text
//";
//            using (var sr = new StringReader(niml))
//            {
//                var n = NimlConvert.Convert(sr);
//                Assert.AreEqual(1, n.Count);
//                var document = n[0] as NElement;
//                Assert.IsNotNull(document);
//                Assert.AreEqual("document", document.Name);
//                Assert.AreEqual(1, document.Children.Count);
//                var content = (NElement)document.Children[0];
//                Assert.AreEqual("content", content.Name);
//                var p = (NElement)content.Children[0];
//                Assert.AreEqual("p", p.Name);
//                var ptext = (NText)p.Children[0];
//                Assert.AreEqual("Paragraph text", ptext.Value);
//            }
//        }

        [Test]
        public void FileTest()
        {
            var files = Directory.EnumerateFiles("Files", "*.niml");
            foreach (var file in files)
            {
                using (var fr = File.OpenText(file))
                using (var tw = File.CreateText(Path.ChangeExtension(file, ".out.html")))
                using (var html = new NimlHtmlWriter(tw))
                {
                    var data = NimlConvert.Convert(fr);
                    foreach (var o in data)
                        html.Write(o);
                }
            }
        }
    }
}
