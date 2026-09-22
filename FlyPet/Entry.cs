using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace FlyPet;

internal static class Entry
{
	[STAThread]
	private static void Main(string[] args)
	{
		ApplicationConfiguration.Initialize();
		string text = Option("--verify-data");
		if (text != null)
		{
			using (Brain brain = new Brain(AppContext.BaseDirectory))
			{
				brain.Send(new Brain.Input(0f, 0f, 0f, Sleep: false, 1f));
				Stopwatch stopwatch = Stopwatch.StartNew();
				while (brain.Snapshot.BioMs < 64 && !brain.Completion.IsCompleted && stopwatch.Elapsed.TotalSeconds < 120.0)
				{
					Thread.Sleep(50);
				}
				Brain.Output snapshot = brain.Snapshot;
				bool flag = snapshot.Neurons == 139255 && snapshot.Edges == 3732460 && snapshot.LoomCount == 314 && snapshot.BioMs >= 64 && snapshot.Spikes > 0;
				File.WriteAllText(text, JsonSerializer.Serialize(new
				{
					ok = flag,
					status = brain.Status,
					snapshot = snapshot
				}));
				Environment.ExitCode = ((!flag) ? 1 : 0);
				return;
			}
		}
		string render = Option("--render");
		string text2 = Option("--profile") ?? "default";
		if (text2.Length > 32 || text2.Any((char c) => !char.IsLetterOrDigit(c) && c != '-'))
		{
			text2 = "default";
		}
		Habitat window = new Habitat(render != null || args.Contains("--preview"), text2);
		try
		{
			if (render != null)
			{
				window.Shown += delegate
				{
					window.PreparePreview(Option("--scene") ?? "table");
					using Bitmap bitmap = new Bitmap(window.Width, window.Height);
					window.DrawToBitmap(bitmap, new Rectangle(Point.Empty, window.Size));
					bitmap.Save(render);
					window.Close();
				};
			}
			Application.Run(window);
		}
		finally
		{
			if (window != null)
			{
				((IDisposable)window).Dispose();
			}
		}
		string? Option(string name)
		{
			int num = Array.IndexOf(args, name);
			if (num < 0 || num + 1 >= args.Length)
			{
				return null;
			}
			return args[num + 1];
		}
	}
}
