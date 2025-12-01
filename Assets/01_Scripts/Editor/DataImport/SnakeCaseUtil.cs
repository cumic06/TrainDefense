#if UNITY_EDITOR
using System.Text;

namespace TrainDefense.Editor.DataImport
{
	public static class SnakeCaseUtil
	{
		public static string ToSnakeCase(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < name.Length; i++)
			{
				char c = name[i];
				if (char.IsUpper(c))
				{
					if (i > 0 && name[i - 1] != '_') sb.Append('_');
					sb.Append(char.ToLowerInvariant(c));
				}
				else
				{
					sb.Append(c);
				}
			}
			return sb.ToString();
		}
	}
}
#endif


