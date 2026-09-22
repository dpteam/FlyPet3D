using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public static class TextBoxExtensions
{
	private const int EM_SETCUEBANNER = 5377;

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

	public static void SetPlaceholderText(this TextBox textBox, string text)
	{
		if (textBox == null)
		{
			throw new ArgumentNullException("textBox");
		}
		SendMessage(textBox.Handle, 5377, (IntPtr)1, text);
	}
}
