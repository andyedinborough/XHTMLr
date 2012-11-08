using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should.Fluent;
using System;
using System.Linq;
using System.Xml.Linq;

namespace XHTMLr.Tests {
	[TestClass]
	public class UnitTest1 {

		public TestContext TestContext { get; set; }

		[TestMethod]
		public void TestPage() {
			using (var web = new System.Net.WebClient()) {
				var html = web.DownloadString("http://ch.tbe.taleo.net/CH11/ats/careers/requisition.jsp?org=INTERTEK&cws=4&rid=3291&utm_source=linkup&utm_medium=referrer");

				var xml = XHTML.ToXml(html, XHTML.Options.Default);
				var xdoc = XDocument.Parse(xml);
				xdoc.Descendants()
					.FirstOrDefault(x => (string)x.Attribute("id") == "taleoContent")
					.Should().Not.Be.Null();
			}
		}

		[TestMethod]
		public void TestSpeed() {
			var html = Html;
			var times = 1000;

			var htmlAgilityPack = Time(times, () => {
				var doc = new HtmlAgilityPack.HtmlDocument();
				doc.LoadHtml(html);
				var input = doc.DocumentNode.Descendants("input").FirstOrDefault();
				var value = input.GetAttributeValue("value", string.Empty);
				value.Should().Equal("test@test.com");
			});

			var xhtmlr = Time(times, () => {
				var doc = System.Xml.Linq.XDocument.Parse(XHTML.ToXml(html));
				var input = doc.Descendants("input").FirstOrDefault();
				var value = (string)input.Attribute("value");
				value.Should().Equal("test@test.com");
			});

			Console.WriteLine("HTML Agility Pack: {0}ms", htmlAgilityPack);
			Console.WriteLine("           XHTMLr: {0}ms", xhtmlr);
		}

		private long Time(int times, Action action) {
			var stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
			while (times-- > 0) {
				action();
			}
			stopwatch.Stop();
			return stopwatch.ElapsedMilliseconds;
		}

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
