using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace XHTMLConvert {
   class Program {
      static void Main(string[] args) {
         string text = "<p>This is normal</p>" +
              "<P><BR/><Br><ul><li>Hello World<li>Goodbye</ul>This is <span class=\"underline\" disabled data-id=\"1\"><span class=\"strong\">bold <em>Italic</em></span> Underline</span></p>" +
              "<p><span class=\"underline\">Underline.</span></p>" +
              "<p><span class=\"strong\">Bold</span></p>" +
              "<p><span class=\"strong\"><em>Bold &amp; Italic<br>" +
              "<br>" +
              "<span class=\"underline\">Unde</span></em></span><span class=\"underline\">rli</span><span class=\"strong\"><span class=\"underline\"></span><em><span class=\"underline\">ne</span></em></span></p>";

         XHTMLConverter x = new XHTMLConverter();

         XmlDocument y = x.ConvertHTML(text, "root");

         Console.Out.WriteLine(y.DocumentElement.OuterXml);

         Console.In.ReadLine();
      }
   }
}
