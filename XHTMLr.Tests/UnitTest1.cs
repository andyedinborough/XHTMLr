using Microsoft.VisualStudio.TestTools.UnitTesting;
using Should.Fluent;
using System;
using System.Linq;
using System.Xml.Linq;

namespace XHTMLr.Tests
{
	[TestClass]
	public class UnitTest1
	{
		#region Fields

		private string _Html;

		#endregion

		#region Properties

		public string Html
		{
			get
			{
				return _Html ?? (_Html = System.IO.File.ReadAllText(
						System.IO.Path.GetFullPath(System.IO.Path.Combine(TestContext.TestRunDirectory, @"..\..\XHTMLr.Tests\Bad.html"))
						));
			}
		}

		public TestContext TestContext { get; set; }

		#endregion

		#region Methods

		[TestMethod]
		public void AddsHtmlBodyTags()
		{
			var html = Html;

			var doc = ParseHtml(ref html);

			doc.Root.Name.LocalName.Should().Equal("html");
			doc.Root.Elements().First().Name.LocalName.Should().Equal("body");

			doc.Descendants("html").Count().Should().Equal(1);
			doc.Descendants("body").Count().Should().Equal(1);
			doc.Descendants("head").Count().Should().Be.InRange(0, 1);
		}

		[TestMethod]
		public void FixesComments()
		{
			var html = Html;
			var doc = ParseHtml(ref html);
			html.Should().Contain("-->");
			var ps = doc.Root.Descendants("p");
			ps.Count().Should().Equal(2);
		}

		[TestMethod]
		public void FontFontFont()
		{
			var html = @"<SPAN lang=EN>
<P dir=ltr align=left>Thank you for choosing <FONT color=#ff0000><FONT color=#ff0000><FONT color=#ff0000>asdf</FONT></FONT></FONT> asdf. </P>
<P dir=ltr align=left>&nbsp;</P><B>
<P dir=ltr align=left></P></B>
<P dir=ltr align=left>Sincerely,</P>
<P dir=ltr align=left></P>
<P dir=ltr align=left>Customer Service </P>
<P dir=ltr align=left>Phone: 1-888-333-4444 </P>
<P dir=ltr align=left>Fax: 1-888-333-5555</P>
<P dir=ltr align=left>Email: </SPAN><A href=""mailto:asdf@asdf.com""><FONT color=#0000ff><FONT color=#0000ff><FONT color=#0000ff><SPAN lang=EN>asdf@asdf.com</FONT></FONT></FONT></SPAN></A></P>
<P dir=ltr align=left>&nbsp;</P><U><FONT face=Arial><FONT face=Arial><SPAN lang=EN>
<P dir=ltr align=left><FONT face=""Times New Roman""></FONT></P>
<P dir=ltr align=left><FONT face=""Times New Roman"">Customer Service Hours</FONT></P></U>
<P dir=ltr align=left><FONT face=""Times New Roman"">Mon-Fri: 7:00am to 10:00pm CST</FONT></P>
<P dir=ltr align=left><FONT face=""Times New Roman"">Sat-Sun: 9:00am to 6:00pm CST</FONT></P></FONT></FONT></SPAN>";

			var xhtml = XHTML.ToXml(html);
			Console.WriteLine(xhtml);
		}

		[TestMethod]
		public void NoNestedPs()
		{
			var ugly = Html;

			var doc = ParseHtml(ref ugly);

			var ps = doc.Root.Descendants("p");
			ps.All(p => p.Descendants("p").Count() == 0).Should().Be.True();

			ps.Count().Should().Equal(2);
		}

		[TestMethod]
		public void NormalizesAttributes()
		{
			var html = Html;

			var doc = ParseHtml(ref html);

			html.Should().Contain("style=\"COLOR:RED;\"");
		}

		[TestMethod]
		public void ParseForm()
		{
			var form = Form.GetForms(Html).FirstOrDefault();
			form["email"].Should().Equal("test@test.com");
			form.Action.Should().Equal("submit.cgi");
			form.Method.ToUpper().Should().Equal("GET");
		}

		[TestMethod]
		public void TestForm()
		{
			var html = "<form method=\"post\"><input type=text name=tested /></form>";
			var forms = Form.GetForms(html).First();
			var keys = forms.Keys.ToArray();
			keys[0].Should().Equal("tested");
		}

		[TestMethod]
		public void TestSpeed()
		{
			var html = Html;
			var times = 1000;

			var htmlAgilityPack = Time(times, () =>
			{
				var doc = new HtmlAgilityPack.HtmlDocument();
				doc.LoadHtml(html);
				var input = doc.DocumentNode.Descendants("input").FirstOrDefault();
				var value = input.GetAttributeValue("value", string.Empty);
				value.Should().Equal("test@test.com");
			});

			var xhtmlr = Time(times, () =>
			{
				var doc = System.Xml.Linq.XDocument.Parse(XHTML.ToXml(html));
				var input = doc.Descendants("input").FirstOrDefault();
				var value = (string)input.Attribute("value");
				value.Should().Equal("test@test.com");
			});

			Console.WriteLine("HTML Agility Pack: {0}ms", htmlAgilityPack);
			Console.WriteLine("           XHTMLr: {0}ms", xhtmlr);
		}

		private XDocument ParseHtml(ref string html, XHTML.Options options = XHTML.Options.Default)
		{
			//      html = XHTML.ToXml(html, options);
			html = XHTML.ToXml(html, XHTML.Options.Default | XHTML.Options.Pretty);

			return XDocument.Parse(html);
		}

		private long Time(int times, Action action)
		{
			var stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
			while (times-- > 0)
			{
				action();
			}
			stopwatch.Stop();
			return stopwatch.ElapsedMilliseconds;
		}

		#endregion
	}
}