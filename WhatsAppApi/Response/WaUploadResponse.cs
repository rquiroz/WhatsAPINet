using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhatsAppApi.Helper;

namespace WhatsAppApi.Response
{
    public class WaUploadResponse
    {
        public string url { get; set; }
        public string mimetype { get; set; }
        public int size { get; set; }
        public string filehash { get; set; }
        public string type { get; set; }
        public int width { get; set; }
        public int height { get; set; }

        public int duration { get; set; }
        public string acodec { get; set; }
        public int asampfreq { get; set; }
        public string asampfmt { get; set; }
        public int abitrate { get; set; }

        public WaUploadResponse()
        { }

        public WaUploadResponse(ProtocolTreeNode node)
        {
            node = node.GetChild("duplicate");
            if (node != null)
            {
                int oSize, oWidth, oHeight, oDuration, oAsampfreq, oAbitrate;
                this.url = node.GetAttribute("url");
                this.mimetype = node.GetAttribute("mimetype");
                Int32.TryParse(node.GetAttribute("size"), out oSize);
                this.filehash = node.GetAttribute("filehash");
                this.type = node.GetAttribute("type");
                Int32.TryParse(node.GetAttribute("width"), out oWidth);
                Int32.TryParse(node.GetAttribute("height"), out oHeight);
                Int32.TryParse(node.GetAttribute("duration"), out oDuration);
                this.acodec = node.GetAttribute("acodec");
                Int32.TryParse(node.GetAttribute("asampfreq"), out oAsampfreq);
                this.asampfmt = node.GetAttribute("asampfmt");
                Int32.TryParse(node.GetAttribute("abitrate"), out oAbitrate);
                this.size = oSize;
                this.width = oWidth;
                this.height = oHeight;
                this.duration = oDuration;
                this.asampfreq = oAsampfreq;
                this.abitrate = oAbitrate;
            }
        }
    }
}
