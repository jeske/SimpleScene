using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading.Tasks;

namespace Example2DTileGame
{

    partial class Example2DTileGame : OpenTK.GameWindow
    {
        
        string xmlData = "<item productID='124390'>" +
                        "<price>5.95</price>" +
                        "</item>";

        StringBuilder stringBuild = new StringBuilder();

        /// <summary>
        /// Read the XML file of the map and output data
        /// </summary>
        public void readMap()
        {
            // Begin
            Console.WriteLine("- READING MAP -");
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlData)))
            {
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.Indent = true;

                using (XmlWriter writer = XmlWriter.Create(stringBuild, ws))
                {
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element :
                                writer.WriteStartElement(reader.Name);
                                break;
                            case XmlNodeType.Whitespace :
                                writer.WriteWhitespace(" ");
                                break;
                            case XmlNodeType.Text :
                                writer.WriteString(reader.Value);
                                break;                               

                        }
                    }
                }

                // End
                Console.WriteLine("- FINISHED MAP READ -");
            }

            Console.WriteLine(stringBuild.ToString());
        }
    }
}