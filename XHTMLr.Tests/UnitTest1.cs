using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should.Fluent;

namespace XHTMLr.Tests {
    [TestClass]
    public class UnitTest1 {

        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        private string _Html;
        public string Html {
            get {
                return _Html ?? (_Html = System.IO.File.ReadAllText(
                    System.IO.Path.GetFullPath(System.IO.Path.Combine(TestContext.TestRunDirectory, @"..\..\XHTMLr.Tests\Bad.html"))
                    ));
            }
        }

        [TestMethod]
        public void NoNestedPs() {
            var ugly = Html;

            var doc = ParseHtml(ref ugly);

            var ps = doc.Root.Descendants("p");
            ps.All(p => p.Descendants("p").Count() == 0).Should().Be.True();

            ps.Count().Should().Equal(2);
        }

        [TestMethod]
        public void AddsHtmlBodyTags() {
            var html = Html;

            var doc = ParseHtml(ref html);

            doc.Root.Name.LocalName.Should().Equal("html");
            doc.Root.Elements().First().Name.LocalName.Should().Equal("body");

            doc.Descendants("html").Count().Should().Equal(1);
            doc.Descendants("body").Count().Should().Equal(1);
            doc.Descendants("head").Count().Should().Be.InRange(0, 1);
        }

        [TestMethod]
        public void NormalizesAttributes() {
            var html = Html;

            var doc = ParseHtml(ref html);

            html.Should().Contain("style=\"COLOR:RED;\"");
        }

        [TestMethod]
        public void FixesComments() {
            var html = Html;

            var doc = ParseHtml(ref html);

            html.Should().Contain("-->");
            var ps = doc.Root.Descendants("p");
            ps.Count().Should().Equal(2);
        }

        [TestMethod]
        public void ParseForm() {
            var form = Form.GetForms(Html).FirstOrDefault();

            form["email"].Should().Equal("test@test.com");
            form.Action.Should().Equal("submit.cgi");
            form.Method.ToUpper().Should().Equal("GET");
        }


        private XDocument ParseHtml(ref string html, XHTML.Options options = XHTML.Options.Default) {
            //      html = XHTML.ToXml(html, options);
            html = XHTML.ToXml(html, XHTML.Options.Default | XHTML.Options.Pretty);

            return XDocument.Parse(html);
        }
    }
}
