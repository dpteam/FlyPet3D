using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace System.Text.Json;

public static class JsonSerializer
{
	private sealed class Parser
	{
		private readonly string _s;

		private int _i;

		public bool IsEnd => _i >= _s.Length;

		public Parser(string s)
		{
			_s = s;
			_i = 0;
		}

		public void SkipWhitespace()
		{
			while (_i < _s.Length && char.IsWhiteSpace(_s[_i]))
			{
				_i++;
			}
		}

		private char Peek()
		{
			if (_i >= _s.Length)
			{
				throw new FormatException("Неожиданный конец JSON.");
			}
			return _s[_i];
		}

		private char Next()
		{
			if (_i >= _s.Length)
			{
				throw new FormatException("Неожиданный конец JSON.");
			}
			return _s[_i++];
		}

		private void Expect(char c)
		{
			if (Next() != c)
			{
				throw new FormatException($"Ожидался '{c}'.");
			}
		}

		public object ParseValue(Type type)
		{
			SkipWhitespace();
			char c = Peek();
			if (c == 'n')
			{
				ExpectLiteral("null");
				return null;
			}
			if (type == typeof(object) || type == null)
			{
				switch (c)
				{
				case '{':
					return ParseObject(typeof(Dictionary<string, object>));
				case '[':
					return ParseArray(typeof(List<object>));
				case '"':
					return ParseString();
				default:
					if (c != 'f')
					{
						return ParseNumber(typeof(double));
					}
					goto case 't';
				case 't':
					return ParseBool();
				}
			}
			Type underlying = Nullable.GetUnderlyingType(type) ?? type;
			if (underlying == typeof(string))
			{
				return ParseString();
			}
			if (underlying == typeof(bool))
			{
				return ParseBool();
			}
			if (underlying == typeof(Guid))
			{
				return Guid.Parse(ParseString());
			}
			if (underlying == typeof(DateTime))
			{
				return DateTime.Parse(ParseString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
			}
			if (underlying.IsEnum)
			{
				return Enum.Parse(underlying, ParseString(), ignoreCase: true);
			}
			if (IsNumeric(underlying))
			{
				return ParseNumber(underlying);
			}
			return c switch
			{
				'{' => ParseObject(underlying), 
				'[' => ParseArray(underlying), 
				_ => throw new FormatException($"Не поддержано для типа {type}."), 
			};
		}

		private void ExpectLiteral(string literal)
		{
			for (int k = 0; k < literal.Length; k++)
			{
				if (Next() != literal[k])
				{
					throw new FormatException("Ожидался литерал '" + literal + "'.");
				}
			}
		}

		public string ParseString()
		{
			SkipWhitespace();
			Expect('"');
			StringBuilder sb = new StringBuilder();
			while (true)
			{
				char c = Next();
				switch (c)
				{
				case '\\':
					switch (Next())
					{
					case '"':
						sb.Append('"');
						break;
					case '\\':
						sb.Append('\\');
						break;
					case '/':
						sb.Append('/');
						break;
					case 'b':
						sb.Append('\b');
						break;
					case 'f':
						sb.Append('\f');
						break;
					case 'n':
						sb.Append('\n');
						break;
					case 'r':
						sb.Append('\r');
						break;
					case 't':
						sb.Append('\t');
						break;
					case 'u':
					{
						char[] hex = new char[4];
						for (int k = 0; k < 4; k++)
						{
							hex[k] = Next();
						}
						sb.Append((char)Convert.ToInt32(new string(hex), 16));
						break;
					}
					default:
						throw new FormatException("Некорректный escape.");
					}
					break;
				default:
					sb.Append(c);
					break;
				case '"':
					return sb.ToString();
				}
			}
		}

		public bool ParseBool()
		{
			SkipWhitespace();
			if (Peek() == 't')
			{
				ExpectLiteral("true");
				return true;
			}
			ExpectLiteral("false");
			return false;
		}

		public object ParseNumber(Type type)
		{
			SkipWhitespace();
			int start = _i;
			if (Peek() == '-')
			{
				_i++;
			}
			while (_i < _s.Length)
			{
				char c = _s[_i];
				if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
				{
					_i++;
					continue;
				}
				break;
			}
			string num = _s.Substring(start, _i - start);
			return Convert.ChangeType(num, type, CultureInfo.InvariantCulture);
		}

		public object ParseArray(Type targetType)
		{
			SkipWhitespace();
			Expect('[');
			SkipWhitespace();
			List<object> list = new List<object>();
			Type elemType = typeof(object);
			if (targetType.IsArray)
			{
				elemType = targetType.GetElementType();
			}
			else if (targetType.IsGenericType)
			{
				Type[] args = targetType.GetGenericArguments();
				if (args.Length == 1)
				{
					elemType = args[0];
				}
			}
			if (Peek() == ']')
			{
				_i++;
				return BuildArray(list, targetType, elemType);
			}
			while (true)
			{
				list.Add(ParseValue(elemType));
				SkipWhitespace();
				switch (Next())
				{
				default:
					throw new FormatException("Ожидался ',' или ']'.");
				case ',':
					break;
				case ']':
					return BuildArray(list, targetType, elemType);
				}
			}
		}

		private object BuildArray(List<object> list, Type targetType, Type elemType)
		{
			if (targetType.IsArray)
			{
				Array arr = Array.CreateInstance(elemType, list.Count);
				for (int k = 0; k < list.Count; k++)
				{
					arr.SetValue(list[k], k);
				}
				return arr;
			}
			if (targetType.IsGenericType)
			{
				Type def = targetType.GetGenericTypeDefinition();
				if (def == typeof(List<>))
				{
					return list;
				}
				if (def == typeof(IList<>) || def == typeof(IEnumerable<>))
				{
					Type concrete = typeof(List<>).MakeGenericType(elemType);
					object result;
					if (!(Activator.CreateInstance(concrete) is IList il))
					{
						result = list;
					}
					else
					{
						result = Fill(il, list);
					}
					return result;
				}
			}
			return list;
		}

		private static IList Fill(IList il, List<object> items)
		{
			foreach (object it in items)
			{
				il.Add(it);
			}
			return il;
		}

		public object ParseObject(Type targetType)
		{
			SkipWhitespace();
			Expect('{');
			SkipWhitespace();
			if (targetType == typeof(Dictionary<string, object>) || targetType == typeof(object))
			{
				Dictionary<string, object> dict = new Dictionary<string, object>();
				if (Peek() == '}')
				{
					_i++;
					return dict;
				}
				while (true)
				{
					SkipWhitespace();
					string key = ParseString();
					SkipWhitespace();
					Expect(':');
					dict[key] = ParseValue(typeof(object));
					SkipWhitespace();
					switch (Next())
					{
					default:
						throw new FormatException("Ожидался ',' или '}'.");
					case ',':
						break;
					case '}':
						return dict;
					}
				}
			}
			object obj;
			try
			{
				obj = Activator.CreateInstance(targetType);
			}
			catch
			{
				return ParseObject(typeof(Dictionary<string, object>));
			}
			PropertyInfo[] props = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			FieldInfo[] fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public);
			if (Peek() == '}')
			{
				_i++;
				return obj;
			}
			while (true)
			{
				SkipWhitespace();
				string key2 = ParseString();
				SkipWhitespace();
				Expect(':');
				PropertyInfo prop = null;
				FieldInfo field = null;
				for (int k = 0; k < props.Length; k++)
				{
					if (string.Equals(props[k].Name, key2, StringComparison.OrdinalIgnoreCase) && props[k].CanWrite)
					{
						prop = props[k];
						break;
					}
				}
				if (prop == null)
				{
					for (int i = 0; i < fields.Length; i++)
					{
						if (string.Equals(fields[i].Name, key2, StringComparison.OrdinalIgnoreCase))
						{
							field = fields[i];
							break;
						}
					}
				}
				if (prop != null)
				{
					object v = ParseValue(prop.PropertyType);
					if (v != null || !prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null)
					{
						prop.SetValue(obj, v, null);
					}
				}
				else if (field != null)
				{
					object v2 = ParseValue(field.FieldType);
					if (v2 != null || !field.FieldType.IsValueType || Nullable.GetUnderlyingType(field.FieldType) != null)
					{
						field.SetValue(obj, v2);
					}
				}
				else
				{
					ParseValue(typeof(object));
				}
				SkipWhitespace();
				switch (Next())
				{
				default:
					throw new FormatException("Ожидался ',' или '}'.");
				case ',':
					break;
				case '}':
					return obj;
				}
			}
		}
	}

	public static byte[] SerializeToUtf8Bytes(object value)
	{
		return Encoding.UTF8.GetBytes(Serialize(value));
	}

	public static string Serialize(object value)
	{
		StringBuilder sb = new StringBuilder(256);
		WriteValue(sb, value);
		return sb.ToString();
	}

	private static void WriteValue(StringBuilder sb, object value)
	{
		if (value == null)
		{
			sb.Append("null");
			return;
		}
		Type type = value.GetType();
		if (value is bool b)
		{
			sb.Append(b ? "true" : "false");
		}
		else if (value is string s)
		{
			WriteString(sb, s);
		}
		else if (value is char c)
		{
			WriteString(sb, c.ToString());
		}
		else if (value is Enum)
		{
			WriteString(sb, value.ToString());
		}
		else if (IsNumeric(type))
		{
			sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
		}
		else if (value is DateTime dt)
		{
			WriteString(sb, dt.ToString("o", CultureInfo.InvariantCulture));
		}
		else if (value is DateTimeOffset dto)
		{
			WriteString(sb, dto.ToString("o", CultureInfo.InvariantCulture));
		}
		else if (value is Guid guid)
		{
			WriteString(sb, guid.ToString("D"));
		}
		else if (value is IDictionary dict)
		{
			WriteDictionary(sb, dict);
		}
		else if (value is IEnumerable enumerable)
		{
			WriteArray(sb, enumerable);
		}
		else
		{
			WriteObject(sb, value, type);
		}
	}

	private static void WriteDictionary(StringBuilder sb, IDictionary dict)
	{
		sb.Append('{');
		bool first = true;
		foreach (DictionaryEntry entry in dict)
		{
			if (!first)
			{
				sb.Append(',');
			}
			first = false;
			WriteString(sb, Convert.ToString(entry.Key, CultureInfo.InvariantCulture));
			sb.Append(':');
			WriteValue(sb, entry.Value);
		}
		sb.Append('}');
	}

	private static void WriteArray(StringBuilder sb, IEnumerable enumerable)
	{
		sb.Append('[');
		bool first = true;
		foreach (object item in enumerable)
		{
			if (!first)
			{
				sb.Append(',');
			}
			first = false;
			WriteValue(sb, item);
		}
		sb.Append(']');
	}

	private static void WriteObject(StringBuilder sb, object value, Type type)
	{
		sb.Append('{');
		bool first = true;
		PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		PropertyInfo[] array = props;
		foreach (PropertyInfo prop in array)
		{
			if (prop.CanRead && prop.GetIndexParameters().Length == 0)
			{
				object propValue;
				try
				{
					propValue = prop.GetValue(value, null);
				}
				catch
				{
					continue;
				}
				if (!first)
				{
					sb.Append(',');
				}
				first = false;
				WriteString(sb, prop.Name);
				sb.Append(':');
				WriteValue(sb, propValue);
			}
		}
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
		FieldInfo[] array2 = fields;
		foreach (FieldInfo field in array2)
		{
			if (!field.IsStatic)
			{
				object fieldValue;
				try
				{
					fieldValue = field.GetValue(value);
				}
				catch
				{
					continue;
				}
				if (!first)
				{
					sb.Append(',');
				}
				first = false;
				WriteString(sb, field.Name);
				sb.Append(':');
				WriteValue(sb, fieldValue);
			}
		}
		sb.Append('}');
	}

	private static void WriteString(StringBuilder sb, string s)
	{
		sb.Append('"');
		foreach (char c in s)
		{
			switch (c)
			{
			case '"':
				sb.Append("\\\"");
				continue;
			case '\\':
				sb.Append("\\\\");
				continue;
			case '\b':
				sb.Append("\\b");
				continue;
			case '\f':
				sb.Append("\\f");
				continue;
			case '\n':
				sb.Append("\\n");
				continue;
			case '\r':
				sb.Append("\\r");
				continue;
			case '\t':
				sb.Append("\\t");
				continue;
			}
			if (c < ' ')
			{
				StringBuilder stringBuilder = sb.Append("\\u");
				int num = c;
				stringBuilder.Append(num.ToString("x4"));
			}
			else
			{
				sb.Append(c);
			}
		}
		sb.Append('"');
	}

	private static bool IsNumeric(Type type)
	{
		TypeCode typeCode = Type.GetTypeCode(type);
		TypeCode typeCode2 = typeCode;
		if ((uint)(typeCode2 - 5) <= 10u)
		{
			return true;
		}
		return false;
	}

	public static T Deserialize<T>(string json)
	{
		return (T)Deserialize(json, typeof(T));
	}

	public static T Deserialize<T>(byte[] utf8)
	{
		return Deserialize<T>(Encoding.UTF8.GetString(utf8));
	}

	public static object Deserialize(string json, Type type)
	{
		if (json == null)
		{
			throw new ArgumentNullException("json");
		}
		Parser parser = new Parser(json);
		object result = parser.ParseValue(type);
		parser.SkipWhitespace();
		if (!parser.IsEnd)
		{
			throw new FormatException("Лишние данные после JSON.");
		}
		return result;
	}
}
