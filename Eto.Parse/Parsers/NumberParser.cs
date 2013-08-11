using System;
using System.Collections.Generic;
using System.Globalization;

namespace Eto.Parse.Parsers
{
	public class NumberParser : Parser
	{
		public bool AllowSign { get; set; }

		public bool AllowDecimal { get; set; }

		public char DecimalSeparator { get; set; }

		public bool AllowExponent { get; set; }

		protected NumberParser(NumberParser other, ParserCloneArgs chain)
			: base(other, chain)
		{
			AllowSign = other.AllowSign;
			AllowExponent = other.AllowExponent;
			DecimalSeparator = other.DecimalSeparator;
		}

		public NumberParser()
		{
			DecimalSeparator = '.';
			AddError = true;
		}

		public decimal GetValue(NamedMatch match)
		{
			var style = NumberStyles.None;

			if (AllowSign)
				style |= NumberStyles.AllowLeadingSign;
			if (AllowDecimal)
				style |= NumberStyles.AllowDecimalPoint;
			if (AllowExponent)
				style |= NumberStyles.AllowExponent;

			return decimal.Parse(match.Value, style);
		}

		protected override ParseMatch InnerParse(ParseArgs args)
		{
			var scanner = args.Scanner;
			var len = 0;
			char ch;
			bool hasNext;
			var pos = scanner.Position;
			if (AllowSign)
			{
				hasNext = scanner.ReadChar(out ch);
				if (!hasNext)
				{
					scanner.SetPosition(pos);
					return args.NoMatch;
				}
				if (ch == '-' || ch == '+')
				{
					len++;
					hasNext = scanner.ReadChar(out ch);
				}
			}
			else
				hasNext = scanner.ReadChar(out ch);

			bool foundNumber = false;
			bool hasDecimal = false;
			bool hasExponent = false;
			while (hasNext)
			{
				if (char.IsDigit(ch))
				{
					foundNumber = true;
				}
				else if (AllowDecimal && !hasDecimal && ch == DecimalSeparator)
				{
					hasDecimal = true;
				}
				else if (hasExponent && (ch == '+' || ch == '-'))
				{
				}
				else if (AllowExponent && !hasExponent && (ch == 'E' || ch == 'e'))
				{
					hasExponent = true;
					hasDecimal = true; // no decimals after exponent
				}
				else if (!foundNumber)
				{
					scanner.SetPosition(pos);
					return args.NoMatch;
				}
				else
					break;
				len++;
				hasNext = scanner.ReadChar(out ch);
			}
			scanner.SetPosition(pos + len);
			return new ParseMatch(pos, len);
		}

		public override Parser Clone(ParserCloneArgs chain)
		{
			return new NumberParser(this, chain);
		}

		public override IEnumerable<Parser> Children(ParserChain args)
		{
			yield break;
		}
	}
}

