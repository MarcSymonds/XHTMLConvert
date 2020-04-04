using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;

namespace XHTMLConvert {
   public class XHTMLConverter {
      public XHTMLConverter() { }

      private static readonly string[] COMMON_ATTRIBUTES = {
               "cellpadding",
               "cellspacing",
               "class",
               "colspan",
               "href",
               "id",
               "method",
               "onclick",
               "onfocus",
               "onblur",
               "post",
               "src",
               "style",
               "title",
               "type",
               "border",
               "valign",
               "value",
               "width",
               "alt" };

      private static readonly string[] EMPTY_ELEMENTS = {
               "br",
               "hr",
               "img",
               "input",
               "link" };

      private static Regex TAG_MATCH = new Regex(
         "<" +
            "(?<endTag>/)?" +
            "((?<tagNS>\\w+):)?" +
            "(?<tagName>[^ />]+)" +
            "(\\s*" +
               "(?<attr>" +
                  "(?<attrName>[^ =/>]+)" +
                  "(\\s*=\\s*" +
                     "(?<attrValue>(\"[^\"]*\"|[^ />]*))" +
                  ")?" +
               ")" +
            ")*" +
            "\\s*" +
            "(?<singleTag>/)?" +
         ">", RegexOptions.Compiled);

      /// <summary>
      /// Converts HTML in to XHTML by closing elements that are not closed; i.e. &lt;br&gt; becomes &lt;br /&gt;
      /// </summary>
      /// <param name="sourceHTML"></param>
      /// <param name="rootNode">The name for a node to wrap the XHTML within. This is required if the source HTML contains multiple elements at the root.</param>
      /// <returns>XmlDocument containing the converted HTML.</returns>
      public XmlDocument ConvertHTML(string sourceHTML, string rootNode = null) {
         char chrChar;
         StringBuilder tagBuilder = new StringBuilder();
         string tag;
         int intPos;
         int intStart;
         List<string> notePath = new List<string>();
         XmlDocument objDoc = null;
         char[] chrLastFew = new char[3];
         int intIdx;

         using (StringReader htmlReader = new StringReader(sourceHTML)) {
            using (MemoryStream xhtmlBuffer = new MemoryStream(sourceHTML.Length)) {
               using (XmlTextWriter xhtmlWriter = new XmlTextWriter(xhtmlBuffer, System.Text.Encoding.UTF8)) {
                  if (rootNode != null) {
                     xhtmlWriter.WriteStartElement(rootNode);
                  }

                  char[] readBuffer = new char[512];
                  int inReadBuffer = htmlReader.Read(readBuffer, 0, 512);
                  intStart = 0;
                  while (inReadBuffer > 0) {
                     intPos = Array.IndexOf(readBuffer, '<', intStart);

                     if (intPos < 0) {
                        xhtmlWriter.WriteRaw(readBuffer, intStart, inReadBuffer - intStart);
                        inReadBuffer = htmlReader.Read(readBuffer, 0, 512);
                        intStart = 0;
                     }
                     else {
                        if (intPos > intStart) {
                           xhtmlWriter.WriteRaw(readBuffer, intStart, intPos - intStart);
                        };

                        bool inQuote = false;
                        bool isCDATA = false;
                        bool isComment = false;
                        chrLastFew[0] = ' ';
                        chrLastFew[1] = ' ';
                        chrLastFew[2] = ' ';

                        tagBuilder.Length = 0;

                        // Read the whole tag by reading up to the closing >
                        //
                        while (true) {
                           chrChar = readBuffer[intPos];
                           ++intPos;

                           tagBuilder.Append(chrChar);

                           if (intPos >= inReadBuffer) {
                              inReadBuffer = htmlReader.Read(readBuffer, 0, 512);

                              if (inReadBuffer == 0) {
                                 break;
                              }
                              else {
                                 intPos = 0;
                              }
                           }

                           if (isCDATA || isComment) {
                              chrLastFew[0] = chrLastFew[1];
                              chrLastFew[1] = chrLastFew[2];
                              chrLastFew[2] = chrChar;

                              if (chrChar == '>') {
                                 if (isCDATA == true) {
                                    if (chrLastFew[0] == ']' && chrLastFew[1] == ']') {
                                       break;
                                    }
                                 }
                                 else if (isComment) {
                                    if (chrLastFew[0] == '-' && chrLastFew[1] == '-') {
                                       break;
                                    }
                                 }
                              }
                           }
                           else if (tagBuilder.Length == 9 && tagBuilder.ToString().Equals("<![cdata[", StringComparison.CurrentCultureIgnoreCase)) {
                              isCDATA = true;
                           }
                           else if (tagBuilder.Length == 4 && tagBuilder.ToString().Equals("<!--")) {
                              isComment = true;
                           }
                           else if (!inQuote && chrChar == '>') {
                              break;
                           }
                           else if (chrChar == '"') {
                              inQuote = !inQuote;
                           }
                        }

                        intStart = intPos;
                        tag = tagBuilder.ToString();

                        if (isCDATA == true) {
                           xhtmlWriter.WriteCData(tag.Substring(9, tag.Length - 12));
                        }
                        else if (isComment == true) {
                           xhtmlWriter.WriteComment(tag.Substring(4, tag.Length - 7));
                        }
                        else {
                           // Use regex to parse the tag.
                           //
                           Match tagMatch = TAG_MATCH.Match(tag);

                           if (!tagMatch.Success) {
                              // If couldn't parse the tag, then just write it out.
                              //
                              xhtmlWriter.WriteComment(tag);
                           }
                           else {
                              string tagNameSpace = string.Empty;
                              string tagName = string.Empty;
                              bool isEndTag = tagMatch.Groups["endTag"].Success;
                              bool isSingleTag = tagMatch.Groups["singleTag"].Success;

                              if (tagMatch.Groups["tagNS"].Success) {
                                 tagNameSpace = tagMatch.Groups["tagNS"].Value;
                              }

                              tagName = tagMatch.Groups["tagName"].Value;

                              if (tagName.In(EMPTY_ELEMENTS, true)) {
                                 // If it's an empty element end tag, then ignore it as we've already created it.
                                 //
                                 if (isEndTag == false) {
                                    xhtmlWriter.WriteStartElement(tagName.ToLower());

                                    WriteAttributes(xhtmlWriter, tagMatch);

                                    xhtmlWriter.WriteEndElement();
                                 }
                              }
                              else if (isEndTag) {
                                 intIdx = notePath.Count - 1;

                                 while (intIdx >= 0 && !notePath[intIdx].Equals(tagName, StringComparison.CurrentCultureIgnoreCase)) {
                                    xhtmlWriter.WriteEndElement();
                                    notePath.RemoveAt(intIdx);
                                    --intIdx;
                                 }

                                 xhtmlWriter.WriteEndElement();

                                 if (intIdx >= 0) { // If this is false then it's all gone wrong
                                    notePath.RemoveAt(intIdx);
                                 }
                              }
                              else {
                                 // TODO: Could do some checking for TR TD, LI elements.

                                 intIdx = notePath.Count - 1;

                                 if (intIdx >= 0 && notePath[intIdx].Equals("script", StringComparison.CurrentCultureIgnoreCase)) {
                                    xhtmlWriter.WriteEndElement();
                                    notePath.RemoveAt(intIdx);
                                    --intIdx;
                                 }

                                 if (intIdx >= 0 && tagName.Equals("li", StringComparison.CurrentCultureIgnoreCase)) {
                                    if (notePath[intIdx].Equals(tagName, StringComparison.CurrentCultureIgnoreCase)) {// No </li>
                                       xhtmlWriter.WriteEndElement();
                                       notePath.RemoveAt(intIdx);
                                       //--intIdx;
                                    }
                                 }

                                 xhtmlWriter.WriteStartElement(tagName);
                                 WriteAttributes(xhtmlWriter, tagMatch);

                                 if (isSingleTag) {
                                    xhtmlWriter.WriteEndElement();
                                 }
                                 else {
                                    notePath.Add(tagName);
                                 }
                              }
                           }
                        }
                     }
                  }

                  // Write out any remaining unclosed elements.
                  //
                  while (notePath.Count > 0) {
                     xhtmlWriter.WriteEndElement();
                     notePath.RemoveAt(0);
                  }

                  if (rootNode != null) {
                     xhtmlWriter.WriteEndElement();
                  }

                  xhtmlWriter.Flush();

                  objDoc = new XmlDocument();

                  using (StreamReader bufferReader = new StreamReader(xhtmlBuffer)) {
                     bufferReader.BaseStream.Seek(0, SeekOrigin.Begin);

                     objDoc.Load(bufferReader);
                  }
               }
            }
         }

         return objDoc;
      }

      private void WriteAttributes(XmlTextWriter xhtmlWriter, Match tagMatch) {
         if (tagMatch.Groups["attr"].Success) {
            CaptureCollection atts = tagMatch.Groups["attr"].Captures;
            CaptureCollection attrNames = tagMatch.Groups["attrName"].Captures;
            CaptureCollection attrValues = tagMatch.Groups["attrValue"].Captures;

            string attrName;
            string attrValue;

            foreach (Capture attrCapture in tagMatch.Groups["attr"].Captures) {
               string attr = attrCapture.Value;
               int eq = attr.IndexOf('=');

               if (eq > 0) {
                  attrName = attr.Substring(0, eq).Trim();
                  attrValue = attr.Substring(eq + 1).Trim(' ', '"');
               }
               else {
                  attrName = attr.Trim();
                  attrValue = string.Empty;
               }

               if (attrName.In(COMMON_ATTRIBUTES, true)) {
                  xhtmlWriter.WriteAttributeString(attrName.ToLower(), attrValue);
               }
               else {
                  xhtmlWriter.WriteAttributeString(attrName, attrValue);
               }
            }
         }
      }
   }
}