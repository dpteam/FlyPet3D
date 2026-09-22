using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FlyPet;

internal sealed class RoomDialog : Form
{
	private readonly Timer _timer = new Timer
	{
		Interval = 300
	};

	internal RoomDialog(PlayerProfile profile, Func<PeerSession?> getPeer, Action<PeerSession?> setPeer)
	{
		Text = "Один стол на двоих";
		base.ClientSize = new Size(650, 605);
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.StartPosition = FormStartPosition.CenterParent;
		BackColor = Art.Paper;
		Font = new Font("Segoe UI", 10f);
		LabelAt("Один стол на двоих", 24, 20, 500, 30, bold: true);
		LabelAt("Мозг и состояние каждой мухи остаются у её владельца.", 24, 58, 590, 24);
		LabelAt("Имя вашей мухи", 24, 95, 170, 25);
		TextBox name = Field(203, 92, 220, profile.Name);
		name.MaxLength = 18;
		name.TextChanged += delegate
		{
			profile.Name = PlayerProfile.CleanName(name.Text);
		};
		LabelAt("1. Создать стол", 24, 143, 450, 30, bold: true);
		LabelAt("Ваш внешний IP / адрес VPN", 24, 182, 270, 25);
		LabelAt("TCP-порт", 296, 182, 100, 25);
		string text = "127.0.0.1";
		try
		{
			text = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault((IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))?.ToString() ?? text;
		}
		catch (SocketException)
		{
		}
		TextBox host = Field(24, 210, 260, text);
		NumericUpDown port = new NumericUpDown
		{
			Location = new Point(296, 210),
			Width = 100,
			Minimum = 1024m,
			Maximum = 65535m,
			Value = 47831m
		};
		base.Controls.Add(port);
		TextBox invite = Field(24, 289, 593, "");
		invite.ReadOnly = true;
		LabelAt("Приглашение — передайте его другу", 24, 258, 500, 25);
		ButtonAt("Открыть стол", 412, 206, 205, delegate
		{
			UriHostNameType uriHostNameType = Uri.CheckHostName(host.Text.Trim());
			if (uriHostNameType == UriHostNameType.Unknown || uriHostNameType == UriHostNameType.IPv6)
			{
				throw new FormatException("Укажите внешний IPv4 или VPN-адрес без порта.");
			}
			setPeer(null);
			PeerSession peerSession2 = PeerSession.Host(profile.Id, (int)port.Value);
			setPeer(peerSession2);
			invite.Text = peerSession2.Invite(host.Text);
		});
		ButtonAt("Копировать", 480, 326, 137, delegate
		{
			if (invite.Text.Length > 0)
			{
				Clipboard.SetText(invite.Text);
			}
		});
		host.TextChanged += delegate
		{
			try
			{
				PeerSession peerSession2 = getPeer();
				if (peerSession2 != null && peerSession2.Port > 0)
				{
					invite.Text = peerSession2.Invite(host.Text);
				}
			}
			catch (FormatException)
			{
				invite.Text = "";
			}
		};
		LabelAt("2. Или присоединиться", 24, 371, 500, 30, bold: true);
		TextBox join = Field(24, 410, 402, "");
		join.SetPlaceholderText("Приглашение flypet://…");
		ButtonAt("Присоединиться", 441, 406, 176, delegate
		{
			PeerSession obj = PeerSession.Join(profile.Id, join.Text);
			setPeer(obj);
		});
		LabelAt("Через интернет: у создателя должен быть открыт выбранный TCP-порт на роутере и в брандмауэре. Альтернатива — общий VPN. Автоматического обхода NAT нет.", 24, 465, 594, 64);
		Label status = new Label
		{
			Location = new Point(24, 538),
			Size = new Size(420, 54),
			ForeColor = Art.Green
		};
		base.Controls.Add(status);
		ButtonAt("Отключиться", 480, 550, 137, delegate
		{
			setPeer(null);
			invite.Text = "";
		});
		_timer.Tick += delegate
		{
			status.Text = getPeer()?.Status ?? "Сейчас вы играете одни.";
		};
		_timer.Start();
		PeerSession peerSession = getPeer();
		if (peerSession != null && peerSession.Port > 0)
		{
			port.Value = peerSession.Port;
			if (peerSession.AdvertisedHost.Length > 0)
			{
				host.Text = peerSession.AdvertisedHost;
			}
			invite.Text = peerSession.Invite(host.Text);
		}
		base.FormClosed += delegate
		{
			_timer.Dispose();
		};
		void ButtonAt(string text2, int x, int y, int width, Action action)
		{
			Button button = new Button
			{
				Text = text2,
				Location = new Point(x, y),
				Size = new Size(width, 34),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.FromArgb(232, 235, 221)
			};
			button.FlatAppearance.BorderSize = 0;
			button.Click += delegate
			{
				try
				{
					action();
				}
				catch (Exception ex2) when (((ex2 is SocketException || ex2 is FormatException || ex2 is InvalidOperationException || ex2 is ExternalException) ? 1 : 0) != 0)
				{
					MessageBox.Show(this, ex2.Message, "Подключение", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
			};
			Controls.Add(button);
		}
		TextBox Field(int x, int y, int width, string text2)
		{
			TextBox textBox = new TextBox
			{
				Location = new Point(x, y),
				Width = width,
				Text = text2
			};
			Controls.Add(textBox);
			return textBox;
		}
		void LabelAt(string text2, int x, int y, int w, int h, bool bold = false)
		{
			Controls.Add(new Label
			{
				Text = text2,
				Location = new Point(x, y),
				Size = new Size(w, h),
				ForeColor = Art.Ink,
				Font = new Font("Segoe UI", bold ? 13 : 10, bold ? FontStyle.Bold : FontStyle.Regular)
			});
		}
	}
}
