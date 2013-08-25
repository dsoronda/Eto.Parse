using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Eto.Parse.Parsers
{
	public class StringParser : Parser
	{
		string quoteCharString;
		char[] quoteCharacters;
		string endQuoteCharString;
		char[] endQuoteCharacters;

		public char[] QuoteCharacters
		{
			get { return BeginQuoteCharacters; }
			set
			{
				BeginQuoteCharacters = EndQuoteCharacters = value;
			}
		}

		public char[] BeginQuoteCharacters
		{
			get { return quoteCharacters; }
			set
			{
				quoteCharacters = value;
				quoteCharString = value != null ? new string(value) : null;
			}
		}

		public char[] EndQuoteCharacters
		{
			get { return endQuoteCharacters; }
			set
			{
				endQuoteCharacters = value;
				endQuoteCharString = value != null ? new string(value) : null;
			}
		}

		public bool AllowEscapeCharacters { get; set; }

		public bool AllowDoubleQuote { get; set; }

		public bool AllowNonQuoted { get; set; }

		public Parser NonQuotedLetter { get; set; }

		public bool AllowQuoted
		{
			get { return quoteCharString != null; }
		}

		public override string DescriptiveName
		{
			get { return AllowQuoted ? "Quoted String" : "String"; }
		}

		public override object GetValue(Match match)
		{
			var val = match.Text;
			if (val.Length > 0)
			{
				// process escapes using string format with no parameters
				if (AllowEscapeCharacters)
				{
					val = GetEscapedString(val);
				}
				else if (AllowQuoted)
				{
					var quoteIndex = quoteCharString.IndexOf(val[0]);
					if (quoteIndex >= 0)
					{
						var quoteChar = endQuoteCharString[quoteIndex];
						if (val.Length >= 2 && val[val.Length - 1] == quoteChar)
						{
							val = val.Substring(1, val.Length - 2);
						}
						if (AllowDoubleQuote)
						{
							val = val.Replace(quoteChar.ToString() + quoteChar, quoteChar.ToString());
						}
					}
				}
			}
			return val;
		}

		string GetEscapedString(string source)
		{
			int pos = 0;
			var length = source.Length;
			var parseDoubleQuote = false;
			char quoteChar = default(char);
			if (AllowQuoted && pos == 0 && pos < source.Length)
			{
				var quoteIndex = quoteCharString.IndexOf(source[pos]);
				if (quoteIndex >= 0)
				{
					quoteChar = endQuoteCharString[quoteIndex];
					if (source.Length >= 2 && source[source.Length - 1] == quoteChar)
					{
						pos++;
						length--;
						parseDoubleQuote = AllowDoubleQuote;
					}
				}
			}
			var sb = new StringBuilder(length);
			while (pos < length)
			{
				char c = source[pos];
				if (parseDoubleQuote && pos < source.Length - 1 && endQuoteCharString.IndexOf(c) >= 0)
				{
					// assume that the parse match ensured that we have a duplicate if we're not at the end of the string
					pos++;
					sb.Append(c);
					continue;
				}
				if (c == '\\')
				{
					pos++;
					if (pos >= length)
						throw new ArgumentException("Missing escape sequence");
					switch (source[pos])
					{
						case '\'':
							c = '\'';
							break;
						case '\"':
							c = '\"';
							break;
						case '\\':
							c = '\\';
							break;
						case '0':
							c = '\0';
							break;
						case 'a':
							c = '\a';
							break;
						case 'b':
							c = '\b';
							break;
						case 'f':
							c = '\f';
							break;
						case 'n':
							c = '\n';
							break;
						case 'r':
							c = '\r';
							break;
						case 't':
							c = '\t';
							break;
						case 'v':
							c = '\v';
							break;
						case 'x':
							var hex = new StringBuilder(4);
							pos++;
							if (pos >= length)
								throw new ArgumentException("Missing escape sequence");
							for (int i = 0; i < 4; i++)
							{
								c = source[pos];
								if (!(char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
									break;
								hex.Append(c);
								pos++;
								if (pos > length)
									break;
							}
							if (hex.Length == 0)
								throw new ArgumentException("Unrecognized escape sequence");
							c = (char)Int32.Parse(hex.ToString(), NumberStyles.HexNumber);
							pos--;
							break;
						case 'u':
							pos++;
							if (pos + 3 >= length)
								throw new ArgumentException("Unrecognized escape sequence");
							try
							{
								uint charValue = UInt32.Parse(source.Substring(pos, 4), NumberStyles.HexNumber);
								c = (char)charValue;
								pos += 3;
							}
							catch (SystemException)
							{
								throw new ArgumentException("Unrecognized escape sequence");
							}
							break;
						case 'U':
							pos++;
							if (pos + 7 >= length)
								throw new ArgumentException("Unrecognized escape sequence");
							try
							{
								uint charValue = UInt32.Parse(source.Substring(pos, 8), NumberStyles.HexNumber);
								if (charValue > 0xffff)
									throw new ArgumentException("Unrecognized escape sequence");
								c = (char)charValue;
								pos += 7;
							}
							catch (SystemException)
							{
								throw new ArgumentException("Unrecognized escape sequence");
							}
							break;
						default:
							throw new ArgumentException("Unrecognized escape sequence");
					}
				}
				pos++;
				sb.Append(c);
			}

			return sb.ToString();
		}

		protected StringParser(StringParser other, ParserCloneArgs args)
			: base(other, args)
		{
			this.BeginQuoteCharacters = other.BeginQuoteCharacters != null ? (char[])other.BeginQuoteCharacters.Clone() : null;
			this.EndQuoteCharacters = other.EndQuoteCharacters != null ? (char[])other.EndQuoteCharacters.Clone() : null;
			this.AllowDoubleQuote = other.AllowDoubleQuote;
			this.AllowEscapeCharacters = other.AllowEscapeCharacters;
			this.AllowNonQuoted = other.AllowNonQuoted;
			this.NonQuotedLetter = args.Clone(other.NonQuotedLetter);
		}

		public StringParser()
		{
			NonQuotedLetter = Terminals.LetterOrDigit;
			QuoteCharacters = "\"\'".ToCharArray();
		}

		protected override ParseMatch InnerParse(ParseArgs args)
		{
			int length = 1;
			var scanner = args.Scanner;
			var pos = scanner.Position;
			char ch;

			if (AllowQuoted)
			{
				if (!scanner.ReadChar(out ch))
					return ParseMatch.None;

				var quoteIndex = quoteCharString.IndexOf(ch);
				if (quoteIndex >= 0)
				{
					char quote = endQuoteCharString[quoteIndex];
					bool isEscape = false;
					for (;;)
					{
						if (!scanner.ReadChar(out ch))
							break;

						length++;
						if (AllowEscapeCharacters && ch == '\\')
							isEscape = true;
						else if (!isEscape)
						{
							if (ch == quote)
							{
								if (!AllowDoubleQuote || scanner.IsEof || scanner.Current != quote)
									return new ParseMatch(pos, length);
								else
									isEscape = true;
							}
						}
						else
							isEscape = false;
					}
				}

				length = 0;
				scanner.SetPosition(pos);
			}

			if (AllowNonQuoted && NonQuotedLetter != null)
			{
				for (;;)
				{
					var m = NonQuotedLetter.Parse(args);
					if (!m.Success || m.Length == 0)
						break;
					length += m.Length;
				}
				if (length > 0)
					return new ParseMatch(pos, length);
			}

			return ParseMatch.None;
		}

		public override Parser Clone(ParserCloneArgs chain)
		{
			return new StringParser(this, chain);
		}
	}
}