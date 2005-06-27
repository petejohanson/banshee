// Copyright 2005 Rapha�l Slinckx <raphael@slinckx.net> 
//
// (see http://entagged.sourceforge.net)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See
// the License for the specific language governing permissions and
// limitations under the License.

/*
 * $Log$
 * Revision 1.1  2005/06/27 00:54:43  abock
 * entagged id3frames
 *
 * Revision 1.3  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

using System;
using Entagged.Audioformats.Generic;
using Entagged.Audioformats.Mp3;

namespace Entagged.Audioformats.Mp3.Util.Id3Frames {
	public class TextId3Frame : Id3Frame, TagTextField {
		
		protected string content;
		protected byte encoding;
		protected string id;
		protected bool common;
		
		/*
		 * 0,1| frame flags
		 * 2| encoding
		 * 3,..,(0x00(0x00))| text content
		 */
		
		public TextId3Frame(string id, string content) {
			this.id = id;
			CheckCommon();
			this.content = content;
			Encoding = Id3v2Tag.DEFAULT_ENCODING;
		}
		
		public TextId3Frame(string id, byte[] rawContent, byte version) : base(rawContent, version) {
			this.id = id;
			CheckCommon();
		}
		
		private void CheckCommon() {
			this.common = id == "TIT2" ||
			  id == "TALB" ||
			  id == "TPE1" ||
			  id == "TCON" ||
			  id == "TRCK" ||
			  id == "TYER" ||
			  id == "COMM";
		}
		
		public string Encoding {
			get {
			    if(encoding == 0)
			        return "ISO-8859-1";
			    else if(encoding == 1)
			        return "UTF-16";
			    
			    return "ISO-8859-1";
			}
			set {
				if(value == "ISO-8859-1")
		        	encoding = 0;
			    else if(value == "UTF-16")
			        encoding = 1;
			    else
			        encoding = 0;
			}
		}
		
		public string Content {
			get { return content; }
			set { this.content = value; }
		}
		
		public override bool IsBinary {
			get { return false; }
			set { /* Not allowed */ }
		}
		
		public override string Id {
			get { return this.id; }
		}
		
		public override bool IsCommon {
			get { return this.common; }
		}
				
		public override bool IsEmpty {
		    get { return content == ""; }
		}
		
		public override void CopyContent(TagField field) {
		    if(field is TextId3Frame) {
		        this.content = (field as TextId3Frame).Content;
		        Encoding = (field as TextId3Frame).Encoding;
		        this.common = (field as TextId3Frame).IsCommon;
		    }
		}
		
		protected override void Populate(byte[] raw) {
			this.encoding = raw[flags.Length];
			if(this.encoding != 0 && this.encoding != 1)
			    this.encoding = 0;
			
			this.content = GetString(raw, flags.Length+1, raw.Length-flags.Length-1, Encoding);
			
			this.content = this.content.Split('\0')[0];
		}
		
		protected override byte[] Build() {
			byte[] data = GetBytes(this.content, Encoding);
			//the return byte[]
			byte[] b = new byte[4 + 4 + flags.Length + 1 + data.Length];
			
			int offset = 0;
			Copy(IdBytes, b, offset);        offset += 4;
			Copy(GetSize(b.Length-10), b, offset); offset += 4;
			Copy(flags, b, offset);               offset += flags.Length;
			
			b[offset] = this.encoding;	offset += 1;
			
			Copy(data, b, offset);
			
			return b;
		}
		
		public override string ToString() {
			return Content;
		}
	}
}
