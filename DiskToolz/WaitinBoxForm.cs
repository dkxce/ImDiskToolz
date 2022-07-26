//
// C# (.Net Framework) Windows-Only
// MSol.XMLSaved
// v 0.5, 21.06.2022
// artem.karimov@weadmire.io
// en,ru,1251,utf-8
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace System.Windows.Forms
{
    public class ProgressBoxForm : WaitingBoxForm { }
    public class WaitingBoxForm
    {
        private class WaitingForm : Form
        {
            private const int CP_NOCLOSE_BUTTON = 0x200;
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams myCp = base.CreateParams;
                    myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                    return myCp;
                }
            }
            private int progress = -1;

            public WaitingForm()
            {
                this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
                this.label1 = new System.Windows.Forms.Label();
                this.progressBar1 = new System.Windows.Forms.ProgressBar();
                this.cancelBtn = new System.Windows.Forms.Button();
                this.tableLayoutPanel1.SuspendLayout();
                this.SuspendLayout();
                // 
                // tableLayoutPanel1
                // 
                this.tableLayoutPanel1.ColumnCount = 1;
                this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.tableLayoutPanel1.Controls.Add(this.progressBar1, 0, 0);
                this.tableLayoutPanel1.Controls.Add(this.label1, 0, 2);
                this.tableLayoutPanel1.Controls.Add(this.cancelBtn, 0, 3);
                this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
                this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
                this.tableLayoutPanel1.Name = "tableLayoutPanel1";
                //this.tableLayoutPanel1.RowCount = 3;
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 5F));
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 0F));
                this.tableLayoutPanel1.Size = new System.Drawing.Size(492, 145);
                this.tableLayoutPanel1.TabIndex = 0;
                // 
                // label1
                // 
                this.label1.Anchor = System.Windows.Forms.AnchorStyles.Top;
                this.label1.AutoSize = true;
                this.label1.Location = new System.Drawing.Point(209, 0);
                this.label1.Name = "label1";
                this.label1.Size = new System.Drawing.Size(73, 13);
                this.label1.TabIndex = 3;
                this.label1.Text = "Please Wait...";
                // 
                // progressBar1
                // 
                this.progressBar1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
                this.progressBar1.Location = new System.Drawing.Point(2, 2);
                this.progressBar1.Name = "progressBar1";
                this.progressBar1.Size = new System.Drawing.Size(490, 23);
                this.progressBar1.TabIndex = 2;
                this.progressBar1.Style = ProgressBarStyle.Marquee;
                //
                // cancelBtn
                //                
                this.cancelBtn.Anchor = AnchorStyles.Top;
                this.cancelBtn.FlatStyle = FlatStyle.Popup;
                this.cancelBtn.Location = new System.Drawing.Point(196, 2);
                this.cancelBtn.Name = "cancelBtn";
                this.cancelBtn.Text = "Cancel";
                this.cancelBtn.Size = new System.Drawing.Size(100, 23);
                this.cancelBtn.TabIndex = 5;
                this.cancelBtn.Enabled = false;
                // 
                // WaitingForm
                // 
                this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
                this.StartPosition = FormStartPosition.CenterParent;
                this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
                this.ClientSize = new System.Drawing.Size(492, 55); // 55
                this.Controls.Add(this.tableLayoutPanel1);
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
                this.Name = "WaitingForm";
                this.Text = "Working in the background";
                this.Load += new System.EventHandler(this.WaitingForm_Load);
                this.tableLayoutPanel1.ResumeLayout(false);
                this.tableLayoutPanel1.PerformLayout();
                this.ShowInTaskbar = false;
                this.ResumeLayout(false);
            }

            private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
            private System.Windows.Forms.ProgressBar progressBar1;
            private System.Windows.Forms.Label label1;
            private System.Windows.Forms.Button cancelBtn;

            private void WaitingForm_Load(object sender, EventArgs e)
            {
                try
                {
                    this.BringToFront();
                    if (this.StartPosition != FormStartPosition.Manual)
                        this.CenterToScreen();
                }
                catch { };
            }

            internal string Label
            {
                get
                {
                    return label1.Text;
                }
                set
                {
                    label1.Text = value;
                }
            }

            internal int Progress
            {
                get
                {
                    return progress;
                }
                set
                {
                    if ((value >= 0) && (value <= 100))
                    {
                        this.progress = value;
                        this.progressBar1.Value = value;
                        if (this.progressBar1.Style != ProgressBarStyle.Continuous)
                            this.progressBar1.Style = ProgressBarStyle.Continuous;
                    }
                    else
                    {
                        this.progress = -1;
                        if (this.progressBar1.Style != ProgressBarStyle.Marquee)
                            this.progressBar1.Style = ProgressBarStyle.Marquee;
                    };
                }
            }

            internal string CancelText
            {
                get
                {
                    return cancelBtn.Text;
                }
                set
                {
                    cancelBtn.Text = value;
                }
            }

            internal EventHandler onCancel
            {
                set
                {
                    this.cancelBtn.Click += value;
                }
            }

            internal bool Cancelable
            {
                set
                {
                    if (value)
                    {
                        if ((!this.Visible) || (this.Visible && (this.tableLayoutPanel1.RowStyles[3].Height == 0)))
                        {
                            this.tableLayoutPanel1.RowStyles[3].Height = 50F;
                            this.ClientSize = new System.Drawing.Size(492, 95);
                        };
                    }
                    else
                    {
                        if (!this.Visible)
                        {
                            this.tableLayoutPanel1.RowStyles[3].Height = 0F;
                            this.ClientSize = new System.Drawing.Size(492, 55);
                        };
                    };
                    this.cancelBtn.Enabled = value;
                }
                get
                {
                    return this.cancelBtn.Enabled;
                }
            }
        }

        private int progress = -1;
        private Thread showThread;
        private bool showForm = false;
        private string formCaption = "Working..";
        private string formText = "Please wait...";
        private string cancelText = "Cancel";
        private Point parentCenter;
        private Form parent;
        private bool parentEnabled;
        private bool isCanceled = false;
        private bool isModal = true;
        private bool isCanCancel = false;

        public EventHandler onCancel;

        public WaitingBoxForm() { }

        public WaitingBoxForm(Form parent)
        {
            if (parent != null)
            {
                this.parent = parent;
                this.parentCenter = new Point(parent.DesktopLocation.X + (int)parent.Width / 2, parent.DesktopLocation.Y + (int)parent.Height / 2);
            };
        }

        public WaitingBoxForm(string Caption, string Text)
        {
            this.formCaption = Caption;
            this.formText = Text;
        }

        public WaitingBoxForm(string Caption, string Text, Form parent)
        {
            this.formCaption = Caption;
            this.formText = Text;
            if (parent != null)
            {
                this.parent = parent;
                this.parentCenter = new Point(parent.DesktopLocation.X + (int)parent.Width / 2, parent.DesktopLocation.Y + (int)parent.Height / 2);
            };
        }

        public string CancelText
        {
            get
            {
                return this.cancelText;
            }
            set
            {
                this.cancelText = value;
            }
        }

        public string Caption
        {
            get
            {
                return this.formCaption;
            }
            set
            {
                this.formCaption = value;
            }
        }

        public string Text
        {
            get
            {
                return this.formText;
            }
            set
            {
                this.formText = value;
            }
        }

        public bool Modal
        {
            get
            {
                return isModal;
            }
            set
            {
                isModal = value;
            }
        }

        public bool Cancelable
        {
            set
            {
                isCanCancel = value;
            }
            get
            {
                return isCanCancel;
            }
        }

        public bool Cancelled
        {
            get
            {
                return this.isCanceled;
            }
        }

        public bool Activated
        {
            get { return showForm; }
        }

        public int Progress
        {
            get
            {
                return progress;
            }
            set
            {
                if ((value < 0) || (value > 100))
                    progress = -1;
                else
                    progress = value;
            }
        }

        private bool ApplicationIsActive(out IntPtr foregroundWindow)
        {
            foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return false;

            int foregroundWindowProcessID;
            GetWindowThreadProcessId(foregroundWindow, out foregroundWindowProcessID);

            return foregroundWindowProcessID == System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private void OnCancel(object sender, EventArgs e)
        {
            this.isCanceled = true;
            if (onCancel != null) onCancel(this, new EventArgs());
        }

        private void ShowThread()
        {
            WaitingForm waitingform = new WaitingForm();
            waitingform.Text = this.formCaption;
            waitingform.Label = this.formText;
            waitingform.CancelText = this.cancelText;
            waitingform.onCancel = new EventHandler(OnCancel);
            waitingform.Cancelable = this.isCanCancel;
            waitingform.Progress = this.Progress;

            if (this.parentCenter != null)
            {
                waitingform.StartPosition = FormStartPosition.Manual;
                waitingform.Location = new Point(parentCenter.X - waitingform.Width / 2, parentCenter.Y - waitingform.Height / 2);
            };

            waitingform.Show();
            waitingform.Refresh();

            int pCtr = 0;
            while (showForm)
            {
                pCtr++;

                if (waitingform.Text != this.formCaption) waitingform.Text = this.formCaption;
                if (waitingform.Label != this.formText) waitingform.Label = this.formText;
                if (waitingform.CancelText != this.cancelText) waitingform.CancelText = this.cancelText;
                if (waitingform.Cancelable != this.Cancelable) waitingform.Cancelable = this.Cancelable;
                if (waitingform.Progress != this.Progress) waitingform.Progress = this.Progress;

                IntPtr fgWn;
                if (isModal && ApplicationIsActive(out fgWn))
                {
                    if (fgWn != waitingform.Handle)
                    {
                        int length = GetWindowTextLength(fgWn);
                        StringBuilder sb = new StringBuilder(length + 1);
                        GetWindowText(fgWn, sb, sb.Capacity);
                        string wTxt = sb.ToString();

                        if ((string.IsNullOrEmpty(wTxt)) || (!wTxt.StartsWith("JavaScript")))
                        {
                            waitingform.BringToFront();
                            waitingform.Activate();
                            waitingform.Focus();
                        };
                    };
                };

                if (pCtr == 20)
                {
                    waitingform.Refresh();
                    waitingform.Update();
                    pCtr = 0;
                };

                Application.DoEvents();
                System.Threading.Thread.Sleep(10);
            };
            waitingform.Close();
            waitingform.Dispose();
            waitingform = null;
        }

        public void Show()
        {
            if (this.showThread != null) return;

            this.isCanceled = false;
            if (this.parent == null)
                this.parent = System.Windows.Forms.Form.ActiveForm;
            if (this.parent != null)
                this.parentCenter = new Point(parent.DesktopLocation.X + (int)parent.Width / 2, parent.DesktopLocation.Y + (int)parent.Height / 2);
            this.showThread = new Thread(new ThreadStart(ShowThread));
            showForm = true;
            if (this.parent != null)
            {
                this.parentEnabled = this.parent.Enabled;
                this.parent.Enabled = false;
            };
            showThread.Start();
        }

        public void Show(int progress)
        {
            this.Progress = progress;
            if (this.showThread != null) return;

            isCanceled = false;
            if (this.parent == null)
                this.parent = System.Windows.Forms.Form.ActiveForm;
            if (this.parent != null)
                this.parentCenter = new Point(parent.DesktopLocation.X + (int)parent.Width / 2, parent.DesktopLocation.Y + (int)parent.Height / 2);
            this.showThread = new Thread(new ThreadStart(ShowThread));
            showForm = true;
            if (this.parent != null)
            {
                this.parentEnabled = this.parent.Enabled;
                this.parent.Enabled = false;
            };
            showThread.Start();
        }

        public void Show(Form parent)
        {
            this.parent = parent;
            if (this.parent == null)
                this.parent = System.Windows.Forms.Form.ActiveForm;
            if (parent != null)
                this.parentCenter = new Point(parent.DesktopLocation.X + (int)parent.Width / 2, parent.DesktopLocation.Y + (int)parent.Height / 2);
            else
                this.parentCenter = Point.Empty;
            this.Show();
        }

        public void Show(Form parent, int progress)
        {
            this.parent = parent;
            if (this.parent == null)
                this.parent = System.Windows.Forms.Form.ActiveForm;
            if (parent != null)
                this.parentCenter = new Point(parent.DesktopLocation.X + (int)parent.Width / 2, parent.DesktopLocation.Y + (int)parent.Height / 2);
            else
                this.parentCenter = Point.Empty;
            this.Show(progress);
        }

        public void Show(string Caption, string Text)
        {
            this.formCaption = Caption;
            this.formText = Text;
            this.Show();
        }

        public void Show(string Caption, string Text, int progress)
        {
            this.formCaption = Caption;
            this.formText = Text;
            this.Show(progress);
        }

        public void Show(string Caption, string Text, Form parent)
        {
            this.formCaption = Caption;
            this.formText = Text;
            this.parent = parent;
            if (this.parent == null)
                this.parent = System.Windows.Forms.Form.ActiveForm;
            if (parent != null)
                this.parentCenter = new Point(parent.DesktopLocation.X + (int)parent.Width / 2, parent.DesktopLocation.Y + (int)parent.Height / 2);
            else
                this.parentCenter = Point.Empty;
            this.Show();
        }

        public void Show(string Caption, string Text, Form parent, int progress)
        {
            this.formCaption = Caption;
            this.formText = Text;
            this.parent = parent;
            if (this.parent == null)
                this.parent = System.Windows.Forms.Form.ActiveForm;
            if (parent != null)
                this.parentCenter = new Point(parent.DesktopLocation.X + (int)parent.Width / 2, parent.DesktopLocation.Y + (int)parent.Height / 2);
            else
                this.parentCenter = Point.Empty;
            this.Show(progress);
        }

        public void Close()
        {
            this.showForm = false;
            if (this.showThread != null) this.showThread.Join();
            this.showThread = null;
        }

        public void Hide()
        {
            this.showForm = false;
            if (this.parent != null)
            {                
                try
                {
                    IntPtr fgWn;
                    bool isact = ApplicationIsActive(out fgWn);
                    if (this.parentEnabled)
                        this.parent.BeginInvoke(new ThreadStart(delegate
                        {
                            this.parent.Enabled = true;
                            if (isact)
                            {
                                this.parent.BringToFront();
                                this.parent.Activate();
                            };
                            this.parent.Focus();
                            this.parent.Refresh();
                        }));                   
                }
                catch { };
            };
            
            if (this.showThread != null) this.showThread.Join();
            this.showThread = null;
        }

        public void HideAndShowParent()
        {
            this.showForm = false;
            if (this.showThread != null) this.showThread.Join();
            this.showThread = null;
            if (this.parent != null)
            {
                if (this.parentEnabled)
                    this.parent.Enabled = true;
                this.parent.BringToFront();
                this.parent.Activate();
                this.parent.Focus();
                this.parent.Refresh();
            };
        }

        public void ShowNoProgress()
        {
            Show(-1);
        }

        public void ShowNoProgress(string Caption, string Text)
        {
            Show(Caption, Text, -1);
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct SafePtr
        {
            private class ReferenceType
            {
                public object Reference;
            }

            private class IntPtrWrapper
            {
                public IntPtr IntPtr;
            }

            [FieldOffset(0)]
            private ReferenceType Obj;

            [FieldOffset(0)]
            private IntPtrWrapper Pointer;

            public static SafePtr Create(object obj)
            {
                ReferenceType rp = new ReferenceType();
                rp.Reference = obj;
                SafePtr sp = new SafePtr();
                sp.Obj = rp;
                return sp;
            }

            public static SafePtr Create(IntPtr rIntPtr)
            {
                IntPtrWrapper ipw = new IntPtrWrapper();
                ipw.IntPtr = rIntPtr;
                SafePtr sp = new SafePtr();
                sp.Pointer = ipw;
                return sp;
            }

            public IntPtr IntPtr
            {
                get { return Pointer.IntPtr; }
                set { Pointer.IntPtr = value; }
            }

            public Object Object
            {
                get { return Obj.Reference; }
                set { Obj.Reference = value; }
            }

            public void SetPointer(SafePtr another)
            {
                Pointer.IntPtr = another.Pointer.IntPtr;
            }
        }
    }
}