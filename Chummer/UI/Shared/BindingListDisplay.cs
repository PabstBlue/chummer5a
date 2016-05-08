﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Chummer.UI.Shared
{
	public partial class BindingListDisplay<TType> : UserControl
	{
		public PropertyChangedEventHandler ChildPropertyChanged;

		private readonly BindingList<TType> _contents;
		private readonly Func<TType, Control> _createFunc;
		private readonly List<Control> _controls = new List<Control>();
		private readonly Dictionary<TType, Control> _map = new Dictionary<TType, Control>();

		private int _progress = 0;

		public BindingListDisplay(BindingList<TType> list, Func<TType, Control> createFunc)
		{
			_contents = list;
			_createFunc = createFunc;
			//DoubleBuffered = true;
			InitializeComponent();
		}

		private void SkillsDisplay_Load(object sender, EventArgs e)
		{
			InitialFill();
		}

		private void ApplicationOnIdle(object sender, EventArgs eventArgs)
		{
			if (AddMore()) return;

			SkillsDisplay_Resize(null, null); //Updates size to show new item.
			_contents.RaiseListChangedEvents = true;
			_contents.ListChanged += ContentsOnListChanged;

			Log.Info($"Finished {_progress}(ALL) items this round");
			Log.Info($"Final height {ClientSize.Height}");
			tblContents.ResumeLayout();
			Application.Idle -= ApplicationOnIdle;
		}

		private bool AddMore()
		{
			int round = 0;
			Stopwatch sw = Stopwatch.StartNew();
			

			while (_progress < _contents.Count)
			{
				AddItem(_contents[_progress]);

				_progress++;
				round++;

				long time = sw.ElapsedMilliseconds;
				time /= round;
				time *= (round + 1);
				if (time > 100)
				{
					Log.Info($"Finished {_progress} items, {round} this round in {sw.ElapsedMilliseconds} ms");
					
					
					Log.Info($"ResumeLayout took finished at {sw.ElapsedMilliseconds}");
					return true;
				}
			}

			return false;
		}
		
		private void InitialFill()
		{
			Application.Idle += ApplicationOnIdle;
			Log.Info($"Initial height suspected {ClientSize.Height}");
			//tblContents.SuspendLayout();

			tblContents.SuspendLayout();
			foreach (TType content in _contents)
			{
				AddItem(content);
				_progress++;
				if (tblContents.Controls.Count > 0 && tblContents.Controls.Count * tblContents.Controls[0].Height > ClientSize.Height)
				{
					break;
				}
			}

			SkillsDisplay_Resize(null, null);

			tblContents.ResumeLayout();
			tblContents.SuspendLayout();
		}

		private void ContentsOnListChanged(object sender, ListChangedEventArgs listChangedEventArgs)
		{
			ListChangedType type = listChangedEventArgs.ListChangedType;
			if (type == ListChangedType.ItemAdded)
			{
				AddItem(_contents[listChangedEventArgs.NewIndex]);
			}
			else if (type == ListChangedType.ItemDeleted)
			{
				tblContents.Controls.RemoveAt(listChangedEventArgs.NewIndex);
				//tblContents.Controls.Remove(_map[_contents[listChangedEventArgs.NewIndex]]);
			}
			SkillsDisplay_Resize(null, null);
		}

		private void AddItem(TType content)
		{
			Control c = _createFunc?.Invoke(content);

			if (c != null)
			{
				_map.Add(content, c);
				_controls.Add(c);
				c.Location = new Point(999,2999); //Moves it out of visible arear. Otherwise first control will flicker
				tblContents.Controls.Add(c, 0, _controls.Count - 1);
				
			}
			else if (Debugger.IsAttached) Debugger.Break();

			INotifyPropertyChanged possibleInterface = (INotifyPropertyChanged) content;
			if (possibleInterface != null)
			{
				possibleInterface.PropertyChanged += OnChildChanged;
			}
			
		}

		private void OnChildChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			ChildPropertyChanged?.Invoke(sender, propertyChangedEventArgs);
		}

		public void Filter(Func<TType, bool> predicate)
		{
			tblContents.SuspendLayout();
			foreach (KeyValuePair<TType, Control> keyValuePair in _map)
			{
				keyValuePair.Value.Visible = predicate?.Invoke(keyValuePair.Key) ?? true;

			}
			tblContents.ResumeLayout();
		}

		public void Sort(IComparer<TType> comparison)
		{
			tblContents.SuspendLayout();
			int count = 0;
			foreach (KeyValuePair<TType, Control> keyValuePair in _map.OrderBy(x => x.Key, comparison))
			{
				tblContents.SetRow(keyValuePair.Value, count++);
			}
			tblContents.ResumeLayout();
		}

		private void SkillsDisplay_Resize(object sender, EventArgs e)
		{
			//This makes scrollbar look right, but it also allows the user to scroll longer down before all is rendered
			//AutoScrollMinSize = new Size(Width - SystemInformation.VerticalScrollBarWidth, tblContents.Controls[0].Height * _contents.Count);
			tblContents.Size = new Size(Width - SystemInformation.VerticalScrollBarWidth, tblContents.PreferredSize.Height);
		}
	}
}