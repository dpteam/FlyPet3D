using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using NeuroFlyEngine;

namespace FlyPet;

internal sealed class Habitat : Form
{
	private readonly RectangleF _table = new RectangleF(108f, 176f, 800f, 500f);

	private readonly RectangleF _roomButton = new RectangleF(934f, 38f, 216f, 42f);

	private readonly RectangleF _lamp = new RectangleF(817f, 104f, 86f, 68f);

	private readonly RectangleF _pauseButton = new RectangleF(859f, 36f, 44f, 42f);

	private readonly RectangleF _brainButton = new RectangleF(935f, 526f, 216f, 40f);

	private readonly Timer _timer = new Timer
	{
		Interval = 40
	};

	private readonly Stopwatch _clock = Stopwatch.StartNew();

	private readonly Brain? _brain;

	private readonly bool _preview;

	private readonly string _folder;

	private readonly World _world;

	private readonly Queue<float> _history = new Queue<float>();

	private readonly List<PointF> _dust = new List<PointF>();

	private readonly List<(TableAction Action, float Born, bool Remote)> _effects = new List<(TableAction, float, bool)>();

	private readonly TableScene _scene = new TableScene();

	private bool _threeD = true;

	private bool _orbiting;

	private PointF _orbitLast;

	private PeerSession? _peer;

	private FlyFrame? _remote;

	private float _remoteX;

	private float _remoteY;

	private float _remoteAngle;

	private TableTool _tool;

	private PointF _mouse = new PointF(-100f, -100f);

	private bool _mouseInside;

	private bool _held;

	private bool _hidden;

	private bool _telemetry;

	private float _visualTime;

	private float _lastSend;

	private float _lastSave;

	private float _lastBrush = -1f;

	private float _lastSwat = -1f;

	private float _sample;

	private double _last;

	private string _notice = "Выберите предмет на полке слева и используйте его на столе.";

	private Brain.Output Readout => _brain?.Snapshot ?? new Brain.Output(default(BrainSignals), 0, 0, 0L, 0, 0f, 0.0, 0, 0, 0);

	private float ScaleFactor => Math.Min((float)base.ClientSize.Width / 1180f, (float)base.ClientSize.Height / 800f);

	private PointF Offset => new PointF(((float)base.ClientSize.Width - 1180f * ScaleFactor) / 2f, ((float)base.ClientSize.Height - 800f * ScaleFactor) / 2f);

	private PointF? TablePoint
	{
		get
		{
			if (!_threeD)
			{
				return new PointF(_mouse.X - _table.X, _mouse.Y - _table.Y);
			}
			return _scene.Camera.PickTable(new PointF(_mouse.X - _table.X, _mouse.Y - _table.Y), 800, 500);
		}
	}

	public Habitat(bool preview, string profile = "default")
	{
		_preview = preview;
		_folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlyPet");
		if (profile != "default")
		{
			_folder = Path.Combine(_folder, "profiles", profile);
		}
		_world = new World(preview ? new PlayerProfile() : PlayerProfile.Load(_folder));
		SavePet();
		Text = "На краю стола · FlyPet 3D";
		base.ClientSize = new Size(1180, 800);
		MinimumSize = new Size(940, 690);
		base.StartPosition = FormStartPosition.CenterScreen;
		BackColor = Art.Paper;
		DoubleBuffered = true;
		base.KeyPreview = true;
		if (!preview)
		{
			_brain = new Brain(FindData());
		}
		Random random = new Random(71);
		for (int i = 0; i < 24; i++)
		{
			_dust.Add(new PointF(random.Next(45, 755), random.Next(45, 455)));
		}
		base.MouseMove += delegate(object? _, MouseEventArgs e)
		{
			_mouse = ToCanvas(e.Location);
			_mouseInside = true;
			if (_orbiting)
			{
				_scene.Camera.Orbit(_mouse.X - _orbitLast.X, _mouse.Y - _orbitLast.Y);
				_orbitLast = _mouse;
			}
			UpdateCursor();
			if (_held && _tool == TableTool.Brush)
			{
				UseTool();
			}
			Invalidate();
		};
		base.MouseWheel += delegate(object? _, MouseEventArgs e)
		{
			if (_threeD && _table.Contains(ToCanvas(e.Location)))
			{
				_scene.Camera.Zoom(e.Delta);
				Invalidate();
			}
		};
		base.MouseLeave += delegate
		{
			_mouseInside = false;
			_held = false;
			SetHidden(hide: false);
			Invalidate();
		};
		base.MouseUp += delegate
		{
			_held = false;
			_orbiting = false;
			base.Capture = false;
			UpdateCursor();
		};
		base.MouseDown += delegate(object? _, MouseEventArgs e)
		{
			_mouse = ToCanvas(e.Location);
			if (e.Button == MouseButtons.Middle && _threeD && _table.Contains(_mouse))
			{
				_orbiting = true;
				_orbitLast = _mouse;
				base.Capture = true;
				SetHidden(hide: false);
			}
			else if (e.Button == MouseButtons.Right)
			{
				Choose(TableTool.Observe);
			}
			else if (e.Button == MouseButtons.Left)
			{
				_held = true;
				if (_roomButton.Contains(_mouse))
				{
					OpenRoom();
				}
				else if (_pauseButton.Contains(_mouse))
				{
					_world.Paused = !_world.Paused;
				}
				else if (_lamp.Contains(_mouse))
				{
					_world.Sleeping = !_world.Sleeping;
					_notice = (_world.Sleeping ? "Лампа погасла. Ваша муха отдыхает." : "Снова светло.");
				}
				else if (_brainButton.Contains(_mouse))
				{
					_telemetry = !_telemetry;
				}
				else if (new RectangleF(772f, 36f, 74f, 42f).Contains(_mouse))
				{
					ToggleDimension();
				}
				else if (_table.Contains(_mouse))
				{
					UseTool();
				}
				else
				{
					for (int j = 0; j < 5; j++)
					{
						if (ToolRect(j).Contains(_mouse))
						{
							Choose((TableTool)j);
						}
					}
				}
				UpdateCursor();
				Invalidate();
			}
		};
		base.Deactivate += delegate
		{
			_held = false;
			_orbiting = false;
			base.Capture = false;
			SetHidden(hide: false);
		};
		base.Activated += delegate
		{
			UpdateCursor();
		};
		base.KeyDown += delegate(object? _, KeyEventArgs e)
		{
			Keys keyCode = e.KeyCode;
			if (keyCode >= Keys.D1 && keyCode <= Keys.D5)
			{
				Choose((TableTool)(e.KeyCode - 49));
			}
			if (e.KeyCode == Keys.Escape)
			{
				Choose(TableTool.Observe);
			}
			if (e.KeyCode == Keys.Space)
			{
				_world.Paused = !_world.Paused;
			}
			if (e.KeyCode == Keys.L)
			{
				_world.Sleeping = !_world.Sleeping;
			}
			if (e.KeyCode == Keys.R)
			{
				_scene.Camera.Reset();
			}
			if (e.KeyCode == Keys.F2)
			{
				ToggleDimension();
			}
		};
		_timer.Tick += delegate
		{
			TickGame();
		};
		_timer.Start();
		base.FormClosing += delegate
		{
			_timer.Stop();
			SetHidden(hide: false);
			_peer?.Dispose();
			_brain?.Dispose();
			SavePet();
		};
		base.FormClosed += delegate
		{
			_scene.Dispose();
		};
	}

	private static string FindData()
	{
		for (DirectoryInfo directoryInfo = new DirectoryInfo(AppContext.BaseDirectory); directoryInfo != null; directoryInfo = directoryInfo.Parent)
		{
			if (File.Exists(Path.Combine(directoryInfo.FullName, "classification.csv.gz")))
			{
				return directoryInfo.FullName;
			}
		}
		return Environment.CurrentDirectory;
	}

	private void SavePet()
	{
		if (_preview)
		{
			return;
		}
		try
		{
			_world.Profile.Save(_folder);
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			_notice = "Не удалось сохранить питомца. Проверьте доступ к папке профиля.";
		}
	}

	private PointF ToCanvas(Point p)
	{
		return new PointF(((float)p.X - Offset.X) / ScaleFactor, ((float)p.Y - Offset.Y) / ScaleFactor);
	}

	private static RectangleF ToolRect(int i)
	{
		return new RectangleF(24f, 203 + i * 81, 60f, 66f);
	}

	private void SetHidden(bool hide)
	{
		if (_hidden != hide)
		{
			_hidden = hide;
			if (hide)
			{
				Cursor.Hide();
			}
			else
			{
				Cursor.Show();
			}
		}
	}

	private void UpdateCursor()
	{
		SetHidden(!_orbiting && _mouseInside && _table.Contains(_mouse) && TablePoint.HasValue && _tool != TableTool.Observe && Form.ActiveForm == this);
	}

	private void ToggleDimension()
	{
		_threeD = !_threeD;
		_held = false;
		_orbiting = false;
		base.Capture = false;
		UpdateCursor();
		Invalidate();
	}

	private void Choose(TableTool tool)
	{
		_tool = tool;
		_held = false;
		UpdateCursor();
		Invalidate();
	}

	private void UseTool()
	{
		if (!_table.Contains(_mouse) || _world.Paused)
		{
			return;
		}
		PointF? tablePoint = TablePoint;
		if (tablePoint.HasValue)
		{
			PointF valueOrDefault = tablePoint.GetValueOrDefault();
			TableTool tool = _tool;
			if ((uint)(tool - 1) <= 1u)
			{
				_world.Place((_tool == TableTool.Fruit) ? "fruit" : "water", valueOrDefault);
				_notice = ((_tool == TableTool.Fruit) ? "Фрукт для вашей мухи. Теперь можно просто наблюдать." : "Капля на столе. Муха подойдёт, когда захочет пить.");
			}
			else if (_tool == TableTool.Swatter && _visualTime - _lastSwat > 0.38f)
			{
				_lastSwat = _visualTime;
				Act(new TableAction("swat", valueOrDefault.X, valueOrDefault.Y), remote: false);
			}
			else if (_tool == TableTool.Brush && _visualTime - _lastBrush > 0.08f)
			{
				_lastBrush = _visualTime;
				Act(new TableAction("brush", valueOrDefault.X, valueOrDefault.Y), remote: false);
			}
		}
	}

	private void Act(TableAction action, bool remote)
	{
		_world.Apply(action);
		if (action.Kind == "brush")
		{
			_dust.RemoveAll((PointF p) => World.Distance(p.X, p.Y, action.X, action.Y) < 52f);
		}
		if (_effects.Count > 80)
		{
			_effects.RemoveAt(0);
		}
		_effects.Add((action, _visualTime, remote));
		if (!remote)
		{
			_peer?.SendAction(action);
		}
	}

	private void TickGame()
	{
		double totalSeconds = _clock.Elapsed.TotalSeconds;
		float num = (float)Math.Min(0.1, totalSeconds - _last);
		_last = totalSeconds;
		_visualTime += num;
		_effects.RemoveAll(((TableAction Action, float Born, bool Remote) e) => _visualTime - e.Born > 0.7f);
		FlyFrame flyFrame = _peer?.Remote;
		if (flyFrame != null)
		{
			if (_remote == null)
			{
				_remoteX = flyFrame.X;
				_remoteY = flyFrame.Y;
				_remoteAngle = flyFrame.Angle;
			}
			_remote = flyFrame;
			float num2 = 1f - MathF.Exp((0f - num) * 14f);
			_remoteX += (flyFrame.X - _remoteX) * num2;
			_remoteY += (flyFrame.Y - _remoteY) * num2;
			_remoteAngle = World.Wrap(_remoteAngle + World.Wrap(flyFrame.Angle - _remoteAngle) * num2);
		}
		else if (!_preview)
		{
			_remote = null;
		}
		if (_peer != null)
		{
			TableAction action;
			while (_peer.TryAction(out action))
			{
				if (action != null)
				{
					Act(action, remote: true);
				}
			}
		}
		Brain.Output readout = Readout;
		if (readout.Neurons > 0 || _preview)
		{
			_world.Tick(num, readout.Signals);
		}
		(float, float, float, float, float) tuple = _world.Inputs();
		_brain?.Send(new Brain.Input(tuple.Item1, tuple.Item2, tuple.Item3, _world.Sleeping, tuple.Item4, _world.Paused, tuple.Item5));
		if (_visualTime - _lastSend >= 0.1f)
		{
			_lastSend = _visualTime;
			_peer?.SendFrame(_world.Frame());
		}
		if (_visualTime - _lastSave > 30f)
		{
			_lastSave = _visualTime;
			SavePet();
		}
		_sample += num;
		if (_sample >= 0.25f)
		{
			_sample = 0f;
			_history.Enqueue(readout.Hz);
			while (_history.Count > 55)
			{
				_history.Dequeue();
			}
		}
		if (_held && _tool == TableTool.Brush)
		{
			UseTool();
		}
		Invalidate();
	}

	private void OpenRoom()
	{
		_held = false;
		SetHidden(hide: false);
		using RoomDialog roomDialog = new RoomDialog(_world.Profile, () => _peer, delegate(PeerSession? p)
		{
			_peer?.Dispose();
			_peer = p;
		});
		roomDialog.ShowDialog(this);
		SavePet();
		UpdateCursor();
	}

	internal void PreparePreview(string scene)
	{
		_world.X = 420f;
		_world.Y = 255f;
		_world.Angle = -0.35f;
		_world.Place("fruit", new PointF(555f, 195f));
		_world.Place("water", new PointF(235f, 320f));
		if (scene == "swatter")
		{
			_tool = TableTool.Swatter;
			_mouse = new PointF(540f, 470f);
			_mouseInside = true;
			_visualTime = 1f;
			_lastSwat = _visualTime;
			Act(new TableAction("swat", 432f, 294f), remote: false);
			_visualTime += 0.12f;
		}
		if (scene == "peer")
		{
			_remote = new FlyFrame(Guid.NewGuid(), "Бусинка", 240f, 180f, 1f, Sleeping: false, "Исследует стол", 80f, 80f, 80f, 80f, new TableItem[1]
			{
				new TableItem("fruit", 175f, 120f, 40f)
			});
			_remoteX = 240f;
			_remoteY = 180f;
			_remoteAngle = 1f;
		}
		if (scene == "orbit")
		{
			_scene.Camera.Orbit(175f, 45f);
			_scene.Camera.Zoom(120);
		}
		if (scene == "2d")
		{
			_threeD = false;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		GraphicsState gstate = graphics.Save();
		graphics.TranslateTransform(Offset.X, Offset.Y);
		graphics.ScaleTransform(ScaleFactor, ScaleFactor);
		Art.Text(graphics, _threeD ? "FLYPET 3D  /  МАЛЕНЬКИЕ ЖИЗНИ" : "FLYPET  /  МАЛЕНЬКИЕ ЖИЗНИ", 26f, 28f, 9f, Art.Green, bold: true);
		Art.Text(graphics, "На краю стола", 23f, 52f, 28f, Art.Ink, bold: true);
		Art.Text(graphics, "У каждого — своя муха. Стол — общий.", 27f, 105f, 10f, Art.Muted);
		Art.Box(graphics, Color.FromArgb(233, 234, 224), _roomButton, 12f);
		PeerSession peer = _peer;
		Art.Oval(graphics, (peer != null && peer.Connected) ? Art.Green : Art.Coral, 948f, 55f, 7f, 7f);
		PeerSession peer2 = _peer;
		Art.Text(graphics, (peer2 != null && peer2.Connected) ? "Вы за одним столом" : "Пригласить друга", 965f, 48f, 10f, Art.Ink, bold: true);
		Art.Box(graphics, Color.FromArgb(233, 234, 224), _pauseButton, 12f);
		Art.Text(graphics, _world.Paused ? "▶" : "Ⅱ", 872f, 46f, 14f, Art.Green, bold: true);
		Art.Box(graphics, Color.FromArgb(233, 234, 224), new RectangleF(772f, 36f, 74f, 42f), 12f);
		Art.Text(graphics, _threeD ? "3D" : "2D", 793f, 47f, 12f, Art.Green, bold: true);
		DrawTools(graphics);
		DrawTable(graphics);
		DrawLamp(graphics);
		DrawSidebar(graphics);
		Art.Text(graphics, ToolTitle(), 109f, 706f, 13f, Art.Ink, bold: true);
		Art.Text(graphics, ToolHint(), 109f, 734f, 10f, Art.Muted);
		Art.Text(graphics, _world.Paused ? "На паузе · пробел, чтобы продолжить" : _notice, 28f, 774f, 9f, Art.Muted);
		if (!_threeD && _mouseInside && _table.Contains(_mouse) && _tool != TableTool.Observe)
		{
			if (_tool == TableTool.Swatter)
			{
				if (_visualTime - _lastSwat > 0.3f)
				{
					Art.Swatter(graphics, _mouse, 1f, -18f, shadow: true);
				}
			}
			else
			{
				Art.Icon(graphics, _tool, _mouse.X, _mouse.Y, (_tool == TableTool.Brush) ? 1.65f : 1.2f);
			}
		}
		graphics.Restore(gstate);
	}

	private string ToolTitle()
	{
		TableTool tool = _tool;
		if (1 == 0)
		{
		}
		string result = tool switch
		{
			TableTool.Fruit => "Долька фрукта", 
			TableTool.Water => "Пипетка с водой", 
			TableTool.Brush => "Мягкая кисть", 
			TableTool.Swatter => "Хлопушка", 
			_ => "Место для маленьких открытий", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private string ToolHint()
	{
		TableTool tool = _tool;
		if (1 == 0)
		{
		}
		string result = tool switch
		{
			TableTool.Fruit => "Нажмите на стол, чтобы оставить еду для своей мухи.", 
			TableTool.Water => "Оставьте каплю на столе — муха сама подойдёт к ней.", 
			TableTool.Brush => "Зажмите мышь и смахните крошки. Рядом с мухой — почистите её.", 
			TableTool.Swatter => "Нажмите, чтобы хлопнуть по столу. Чем ближе удар, тем сильнее испуг.", 
			_ => _threeD ? "Средняя кнопка — вращать · колесо — масштаб · R — сброс · F2 — 2D" : "1–5 — инструменты · правая кнопка — убрать предмет · L — лампа", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private void DrawTools(Graphics g)
	{
		Art.Text(g, "ПОЛКА", 31f, 177f, 8f, Art.Muted, bold: true);
		for (int i = 0; i < 5; i++)
		{
			RectangleF r = ToolRect(i);
			bool flag = i == (int)_tool;
			Art.Box(g, flag ? Art.Green : Color.FromArgb(234, 234, 224), r);
			Art.Icon(g, (TableTool)i, r.X + 30f, r.Y + 26f, 1f, flag);
			Art.Text(g, (i + 1).ToString(), r.X + 27f, r.Y + 47f, 7f, flag ? Color.FromArgb(211, 223, 209) : Art.Muted);
		}
	}

	private void DrawTable(Graphics g)
	{
		if (_threeD)
		{
			Draw3D(g);
			return;
		}
		Art.Text(g, _world.Sleeping ? "ТИХИЙ ВЕЧЕР" : "СОЛНЕЧНЫЙ УГОЛОК", 110f, 148f, 8f, Art.Muted, bold: true);
		Art.Box(g, Color.FromArgb(25, 101, 81, 54), new RectangleF(108f, 185f, 800f, 503f), 24f);
		using GraphicsPath graphicsPath = Art.Round(_table, 22f);
		using (LinearGradientBrush brush = new LinearGradientBrush(_table, Color.FromArgb(226, 209, 177), Color.FromArgb(204, 183, 147), 80f))
		{
			g.FillPath(brush, graphicsPath);
		}
		GraphicsState gstate = g.Save();
		g.SetClip(graphicsPath);
		Random random = new Random(41);
		for (int i = 0; i < 40; i++)
		{
			float num = 180 + i * 14;
			using Pen pen = new Pen(Color.FromArgb(13 + random.Next(8), 130, 102, 64), (i % 5 == 0) ? 1.3f : 0.7f);
			g.DrawBezier(pen, 105f, num, 310f, num - (float)random.Next(14), 645f, num + (float)random.Next(12), 915f, num - 4f);
		}
		using (Pen pen2 = new Pen(Color.FromArgb(28, 119, 93, 58), 1f))
		{
			g.DrawLine(pen2, 108, 344, 908, 350);
			g.DrawLine(pen2, 108, 510, 908, 505);
		}
		using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(26, 255, 253, 229)))
		{
			g.FillPolygon(brush2, new PointF[4]
			{
				new PointF(490f, 176f),
				new PointF(775f, 176f),
				new PointF(506f, 676f),
				new PointF(220f, 676f)
			});
		}
		foreach (PointF item2 in _dust)
		{
			Art.Oval(g, Color.FromArgb(110, 151, 126, 85), _table.X + item2.X, _table.Y + item2.Y, 2f, 2f);
			Art.Oval(g, Color.FromArgb(80, 161, 128, 77), _table.X + item2.X + 4f, _table.Y + item2.Y + 4f, 3f, 2f);
		}
		foreach (TableItem item3 in _world.Items)
		{
			DrawItem(g, item3, remote: false);
		}
		if (_remote != null)
		{
			TableItem[] items = _remote.Items;
			TableItem[] array = items;
			foreach (TableItem item4 in array)
			{
				DrawItem(g, item4, remote: true);
			}
		}
		if (_world.Sleeping)
		{
			Art.Box(g, Color.FromArgb(95, 45, 57, 61), _table, 22f);
		}
		foreach (var effect in _effects)
		{
			DrawEffect(g, effect.Action, _visualTime - effect.Born, effect.Remote);
		}
		if (_remote != null)
		{
			DrawFly(g, _remoteX, _remoteY, _remoteAngle, _remote.Sleeping, _remote.Name, local: false);
		}
		DrawFly(g, _world.X, _world.Y, _world.Angle, _world.Sleeping, _world.Profile.Name, local: true);
		g.Restore(gstate);
		if (Readout.Neurons == 0 && !_preview)
		{
			Art.Box(g, Color.FromArgb(245, 250, 249, 241), new RectangleF(255f, 370f, 505f, 86f));
			Art.Text(g, "Знакомимся с вашей мухой…", 276f, 384f, 16f, Art.Ink, bold: true);
			string text = _brain?.Status ?? "";
			Art.Text(g, (text.Length > 64) ? (text.Substring(0, 64) + "…") : text, 276f, 420f, 9f, Art.Green);
		}
	}

	private void Draw3D(Graphics g)
	{
		Art.Text(g, _world.Sleeping ? "ТИХИЙ ВЕЧЕР / 3D" : "СОЛНЕЧНЫЙ УГОЛОК / 3D", 110f, 148f, 8f, Art.Muted, bold: true);
		FlyFrame flyFrame = ((_remote == null) ? null : _remote with
		{
			X = _remoteX,
			Y = _remoteY,
			Angle = _remoteAngle
		});
		SceneEffect[] effects = _effects.Select(((TableAction Action, float Born, bool Remote) e) => new SceneEffect(e.Action, _visualTime - e.Born, e.Remote)).ToArray();
		Bitmap image = _scene.Render(_world, flyFrame, _visualTime, _dust, effects, _tool, (_mouseInside && _table.Contains(_mouse) && !_orbiting) ? TablePoint : ((PointF?)null), _visualTime - _lastSwat);
		GraphicsState gstate = g.Save();
		using (GraphicsPath clip = Art.Round(_table, 22f))
		{
			g.SetClip(clip);
			g.DrawImage(image, _table);
			Caption(_world.Frame(), local: true);
			if (flyFrame != null)
			{
				Caption(flyFrame, local: false);
			}
			g.Restore(gstate);
			if (Readout.Neurons == 0 && !_preview)
			{
				Art.Box(g, Color.FromArgb(245, 250, 249, 241), new RectangleF(255f, 370f, 505f, 86f));
				Art.Text(g, "Знакомимся с вашей мухой…", 276f, 384f, 16f, Art.Ink, bold: true);
				string text = _brain?.Status ?? "";
				Art.Text(g, (text.Length > 64) ? (text.Substring(0, 64) + "…") : text, 276f, 420f, 9f, Art.Green);
			}
		}
		void Caption(FlyFrame frame, bool local)
		{
			PointF pointF = _scene.Camera.Project(new Vector3(frame.X - 400f, 48f, frame.Y - 250f), 800, 500);
			if (pointF.X < 0f || pointF.X > 800f || pointF.Y < 0f || pointF.Y > 480f)
			{
				return;
			}
			string text2 = frame.Name + (local ? " · вы" : "");
			using Font font = new Font("Segoe UI", 8f);
			SizeF sizeF = g.MeasureString(text2, font);
			float num = ZMath.Clamp(_table.X + pointF.X - sizeF.Width / 2f, _table.X + 8f, _table.Right - sizeF.Width - 8f);
			float num2 = _table.Y + pointF.Y - 21f;
			Art.Box(g, Color.FromArgb(220, 248, 246, 233), new RectangleF(num - 5f, num2, sizeF.Width + 10f, 20f), 7f);
			Art.Text(g, text2, num, num2 + 1f, 8f, local ? Art.Green : Art.Coral, bold: true);
		}
	}

	private void DrawItem(Graphics g, TableItem item, bool remote)
	{
		float num = _table.X + item.X;
		float num2 = _table.Y + item.Y;
		float num3 = 0.7f + item.Amount / 100f;
		Art.Oval(g, Color.FromArgb(22, 91, 65, 32), num - 17f, num2 + 8f, 35f, 10f);
		if (item.Kind == "fruit")
		{
			Art.Icon(g, TableTool.Fruit, num, num2, num3 * 1.3f);
		}
		else
		{
			Art.Oval(g, Color.FromArgb(92, 123, 172, 183), num - 17f * num3, num2 - 9f * num3, 34f * num3, 21f * num3);
			Art.Oval(g, Color.FromArgb(135, 244, 249, 236), num - 10f, num2 - 6f, 10f, 4f);
		}
		Art.Oval(g, remote ? Art.Coral : Art.Green, num - 2f, num2 + 26f, 4f, 4f);
	}

	private void DrawFly(Graphics g, float x, float y, float angle, bool sleeping, string name, bool local)
	{
		x += _table.X;
		y += _table.Y;
		Art.Fly(g, new PointF(x, y), angle, sleeping, (sleeping || (_world.Paused & local)) ? 0f : (MathF.Sin(_visualTime * 18f) * 3f), local);
		string text = name + (local ? " · вы" : "");
		using Font font = new Font("Segoe UI", 8f);
		SizeF sizeF = g.MeasureString(text, font);
		float num = ZMath.Clamp(x - sizeF.Width / 2f, _table.Left + 8f, _table.Right - sizeF.Width - 12f);
		float num2 = Math.Min(y + 30f, _table.Bottom - 28f);
		Art.Box(g, Color.FromArgb(213, 248, 245, 232), new RectangleF(num - 5f, num2 - 1f, sizeF.Width + 10f, 21f), 9f);
		Art.Text(g, text, num, num2 + 1f, 8f, local ? Art.Green : Art.Coral, bold: true);
	}

	private void DrawLamp(Graphics g)
	{
		Art.Oval(g, Color.FromArgb(30, 70, 63, 46), 842f, 159f, 45f, 9f);
		Art.Box(g, Color.FromArgb(172, 174, 154), new RectangleF(844f, 157f, 39f, 7f), 3f);
		Art.Line(g, Art.Green, 3f, new PointF(864f, 157f), new PointF(864f, 125f));
		using SolidBrush brush = new SolidBrush(_world.Sleeping ? Color.FromArgb(125, 143, 127) : Color.FromArgb(106, 135, 108));
		g.FillPolygon(brush, new PointF[4]
		{
			new PointF(850f, 112f),
			new PointF(876f, 112f),
			new PointF(889f, 137f),
			new PointF(838f, 137f)
		});
		Art.Oval(g, _world.Sleeping ? Art.Muted : Color.FromArgb(241, 218, 157), 839f, 133f, 49f, 6f);
		Art.Text(g, "L", 896f, 144f, 7f, Art.Muted);
	}

	private void DrawEffect(Graphics g, TableAction action, float age, bool remote)
	{
		float num = _table.X + action.X;
		float num2 = _table.Y + action.Y;
		float num3 = ZMath.Clamp(1f - age / 0.7f, 0f, 1f);
		if (action.Kind == "swat")
		{
			float num4 = 20f + age * 145f;
			using Pen pen = new Pen(Color.FromArgb((int)(100f * num3), 109, 88, 59), 2f);
			g.DrawEllipse(pen, num - num4, num2 - num4 * 0.55f, num4 * 2f, num4 * 1.1f);
			for (int i = 0; i < 7; i++)
			{
				float x = (float)i * ((float)Math.PI * 2f) / 7f;
				Art.Oval(g, Color.FromArgb((int)(90f * num3), 129, 102, 62), num + MathF.Cos(x) * num4 * 0.6f, num2 + MathF.Sin(x) * num4 * 0.4f, 3f, 2f);
			}
			if (remote || age < 0.3f)
			{
				Art.Swatter(g, new PointF(num, num2 - Math.Max(0f, age - 0.12f) * 70f), 1f - age * 0.3f, -15f + MathF.Sin(age * 14f) * 10f, shadow: true);
			}
			return;
		}
		using Pen pen2 = new Pen(Color.FromArgb((int)(80f * num3), 249, 247, 227), 6f);
		g.DrawArc(pen2, num - 23f, num2 - 12f, 46f, 24f, 10f, 150f);
		if (remote)
		{
			Art.Icon(g, TableTool.Brush, num, num2 - 12f, 1.4f);
		}
	}

	private void DrawSidebar(Graphics g)
	{
		Art.Text(g, "ВАША МУХА", 938f, 151f, 8f, Art.Muted, bold: true);
		Art.ClippedText(g, _world.Profile.Name, new RectangleF(934f, 176f, 220f, 43f), 23f, Art.Ink, bold: true);
		Art.Text(g, _world.Paused ? "На паузе" : _world.Activity, 939f, 218f, 10f, Art.Green);
		Meter(g, "Сытость", _world.Pet.Satiety, 262f, Color.FromArgb(193, 156, 95));
		Meter(g, "Вода", _world.Pet.Water, 316f, Color.FromArgb(121, 157, 168));
		Meter(g, "Энергия", _world.Pet.Energy, 370f, Color.FromArgb(147, 167, 110));
		Meter(g, "Чистота", _world.Pet.Clean, 424f, Color.FromArgb(159, 166, 143));
		Art.Text(g, $"Вместе {TimeSpan.FromSeconds(_world.Pet.AgeSeconds):hh\\:mm\\:ss}", 939f, 483f, 9f, Art.Muted);
		Art.Box(g, Color.FromArgb(232, 234, 223), _brainButton, 11f);
		Art.Oval(g, (Readout.Neurons > 0) ? Art.Green : Art.Coral, 948f, 542f, 6f, 6f);
		Art.Text(g, "Нейросеть", 965f, 536f, 10f, Art.Green, bold: true);
		Art.Text(g, _telemetry ? "−" : "+", 1127f, 533f, 14f, Art.Green);
		if (_telemetry)
		{
			Art.Text(g, $"{Readout.Hz:F1} Гц / нейрон", 939f, 581f, 16f, Art.Green, bold: true);
			Art.Text(g, $"{Readout.Neurons:N0} нейронов", 939f, 617f, 9f, Art.Muted);
			Art.Text(g, $"{(double)Readout.Edges / 1000000.0:F2} млн связей · {(float)Readout.BioMs / 1000f:F1} с", 939f, 640f, 9f, Art.Muted);
			float[] array = _history.ToArray();
			if (array.Length > 1)
			{
				float max = Math.Max(10f, array.Max() * 1.2f);
				Art.Line(g, Art.Green, 1.5f, array.Select((float v, int i) => new PointF(940f + (float)i * 3.8f, 713f - v / max * 35f)).ToArray());
			}
			return;
		}
		Art.Text(g, "ЗА ЭТИМ СТОЛОМ", 939f, 594f, 8f, Art.Muted, bold: true);
		Art.Oval(g, Art.Green, 941f, 628f, 7f, 7f);
		Art.Text(g, _world.Profile.Name + " · вы", 958f, 620f, 10f);
		Art.Oval(g, (_remote != null) ? Art.Coral : Color.FromArgb(197, 198, 185), 941f, 661f, 7f, 7f);
		Art.Text(g, _remote?.Name ?? "Место для друга", 958f, 653f, 10f, (_remote != null) ? Art.Ink : Art.Muted);
		using Font font = new Font("Segoe UI", 8f);
		using SolidBrush brush = new SolidBrush(Art.Muted);
		g.DrawString(_peer?.Status ?? "Прямое соединение · 2 игрока", font, brush, new RectangleF(939f, 693f, 210f, 58f));
	}

	private static void Meter(Graphics g, string title, float value, float y, Color color)
	{
		Art.Text(g, title, 939f, y, 10f);
		Art.Text(g, $"{value:F0}", 1123f, y, 9f, Art.Muted);
		Art.Box(g, Color.FromArgb(229, 231, 220), new RectangleF(941f, y + 27f, 206f, 5f), 2f);
		if (value > 0f)
		{
			Art.Box(g, color, new RectangleF(941f, y + 27f, Math.Max(1f, 206f * value / 100f), 5f), 2f);
		}
	}
}
