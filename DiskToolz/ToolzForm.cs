// http://www.ltr-data.se/library/imdisknet/html/b33f1e89-3d92-fc08-248d-14c5c2efd549.htm

using System;
using System.Management;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiskToolz
{
    public partial class ToolzForm : Form
    {
        public ToolzForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text += String.Format(" v{0}", FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location).FileVersion);
            RefreshDeviceList();
            RefreshDevio();
            jView.Items.Clear();
            SearchJunctions(null);
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshDeviceList();
        }  
        
        private void RefreshDeviceList()
        {
            dView.Items.Clear();
            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();
            foreach (int d in devs)
            {
                try
                {
                    DListViewItem lvi = new DListViewItem(d);
                    LTR.IO.ImDisk.UnsafeNativeMethods.ImDiskCreateData dd = LTR.IO.ImDisk.ImDiskAPI.QueryDevice((uint)d);
                    lvi.SubItems.Add(char.IsLetter(dd.DriveLetter) ? dd.DriveLetter.ToString() : "");
                    lvi.SubItems.Add(BytesToString(dd.DiskSize));
                    string fn = "";
                    if (!string.IsNullOrEmpty(dd.Filename)) fn = dd.Filename;
                    else if ((((LTR.IO.ImDisk.ImDiskFlags)dd.Flags) & LTR.IO.ImDisk.ImDiskFlags.TypeVM) == LTR.IO.ImDisk.ImDiskFlags.TypeVM)
                        fn = "Virtual Memory";
                    lvi.SubItems.Add(fn);
                    lvi.SubItems.Add(((LTR.IO.ImDisk.ImDiskFlags)dd.Flags).ToString());
                    lvi.SubItems.Add(dd.MediaType.ToString());
                    dView.Items.Add(lvi);
                }
                catch { };
            };
        }
        public static string BytesToString(long size)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            };
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##} {1}", len, sizes[order]);
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            removeToolStripMenuItem.Enabled = dView.SelectedItems.Count > 0;
            forceRemoveToolStripMenuItem.Enabled = dView.SelectedItems.Count > 0;
            emergencyRemoveToolStripMenuItem.Enabled = dView.SelectedItems.Count > 0;
            saveToFileToolStripMenuItem.Enabled = dView.SelectedItems.Count == 1;
            mountInTrueCryptToolStripMenuItem.Enabled = dView.SelectedItems.Count == 1;
        }

        private void forceRemoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Remove(true);
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Remove(false);
        }

        private void Remove(bool force, bool em = false)
        {
            if (dView.SelectedItems.Count == 0) return;
            int dCount = dView.SelectedItems.Count;
            if (MessageBox.Show("Do you really want to remove " + dCount.ToString() + " items?", "Remove Device/Disk", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            for(int i = dView.Items.Count -1;i>=0;i--)
            {
                if (!dView.Items[i].Selected) continue;
                uint dNo = (uint)(((DListViewItem)dView.Items[i]).ID);
                string args = force ?  "-D" : "-d";
                if (em) args = "-R";
                args += " -u " + dNo.ToString();

                //LTR.IO.ImDisk.ImDiskAPI.RemoveDevice
                //LTR.IO.ImDisk.ImDiskAPI.ForceRemoveDevice

                ProcessStartInfo psi = new ProcessStartInfo("imdisk", args);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                proc.WaitForExit();
            };            
            RefreshDeviceList();
        }

        private void emergencyRemoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Remove(false, true);
            
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jView.Items.Clear();
            SearchJunctions(null);
        }
        private void SearchJunctions(string pathToSearch)
        {
            List<string> dirs = new List<string>();

            searchToolStripMenuItem.Enabled = false;
            searchAtToolStripMenuItem.Enabled = false;

            if (!string.IsNullOrEmpty(pathToSearch))
            {
                try { dirs.AddRange(Directory.GetDirectories(pathToSearch)); } catch { };
            }
            else
            {
                foreach (DriveInfo drive in System.IO.DriveInfo.GetDrives())
                {
                    try { dirs.AddRange(Directory.GetDirectories(drive.Name)); } catch { };
                };
            };

            while (dirs.Count > 0)
            {
                string path = dirs[0];
                dirs.RemoveAt(0);
                try
                {
                    if (JunctionPoint.Exists(path))
                    {
                        string target = JunctionPoint.GetTarget(path);
                        ListViewItem lvi = new ListViewItem(new string[] { path, target });
                        jView.Items.Add(lvi);
                    };
                }
                catch (Exception ex) { };
                Application.DoEvents();
            };

            searchToolStripMenuItem.Enabled = true;
            searchAtToolStripMenuItem.Enabled = true;
        }

        private void searchAtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string dir = @"C:\";
            if (InputBox.QueryDirectoryBox("Search Junctions at", "Select Folder to Search:", ref dir) != DialogResult.OK) return;
            SearchJunctions(dir);
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jView.Items.Clear();
        }

        private void removeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //LTR.IO.ImDisk.ImDiskAPI.RemoveMountPoint

            if (jView.SelectedItems.Count == 0) return;
            int dCount = jView.SelectedItems.Count;
            if (MessageBox.Show("Do you really want to remove " + dCount.ToString() + " items?", "Remove Junctions", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            for (int i = jView.Items.Count - 1; i >= 0; i--)
            {
                if (!jView.Items[i].Selected) continue;
                try
                {
                    JunctionPoint.Delete(jView.Items[i].Text);
                }
                catch { };
                jView.Items.RemoveAt(i);
            };            
        }

        private void contextMenuStrip2_Opening(object sender, CancelEventArgs e)
        {
            removeToolStripMenuItem1.Enabled = jView.SelectedItems.Count > 0;
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string real = @"C:\Real";
            if (InputBox.QueryDirectoryBox("Create Junction", "Select Real Path:", ref real) != DialogResult.OK) return;
            string virt = @"C:\Virt";
            if (InputBox.QueryDirectoryBox("Create Junction", "Select Virtual Path:", ref virt) != DialogResult.OK) return;
            if (!real.EndsWith(@"\")) real += @"\";

            try
            {
                JunctionPoint.Create(virt, real, true);
                ListViewItem lvi = new ListViewItem(new string[] { virt, real });
                jView.Items.Add(lvi);
            }
            catch (Exception ex) { };
        }

        private void addImDiskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //LTR.IO.ImDisk.ImDiskAPI.CreateMountPoint

            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();

            List<string> devices = new List<string>();
            foreach (int d in devs) devices.Add(@"\Device\ImDisk" + d.ToString());

            string real = devices.Count > 0 ? devices[0] : @"\Device\Unknown";
            if (devices.Count > 0)
            {
                if (InputBox.Show("Create Junction", "Select Real Device", devices.ToArray(), ref real, true) != DialogResult.OK) return;
            }
            else
            {
                if (InputBox.Show("Create Junction", "Enter Real Device", ref real) != DialogResult.OK) return;
            };
            string virt = @"C:\Virt";
            if (InputBox.QueryDirectoryBox("Create Junction", "Select Virtual Path:", ref virt) != DialogResult.OK) return;
            if (!real.EndsWith(@"\")) real += @"\";

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("junc.exe", "\""+virt+"\" "+real);
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                proc.WaitForExit();
                ListViewItem lvi = new ListViewItem(new string[] { virt, real });
                jView.Items.Add(lvi);
            }
            catch (Exception ex) { };
        }

        private void imDiskcplToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("imdisk.cpl");
        }

        private void addToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            int size = 10;
            string mPoint = GetNextDriveLetter();
            uint devNo = 0;
            if (InputBox.Show("Create ImDisk Device (Rewritable) in Memory", "Size of disk in MB:", ref size, 10, 64 * 1024) != DialogResult.OK) return;
            if (InputBox.Show("Create ImDisk Device (Rewritable) in Memory", "Mounting Point:", ref mPoint) != DialogResult.OK) return;
            size = size * 1024 * 1024;

            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();
            while (devs.Contains((int)devNo)) devNo++;
            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Create ImDisk Device (Rewritable) in Memory", "Wait, Creating Device...", this);
            try
            {
                LTR.IO.ImDisk.ImDiskAPI.CreateDevice(size, mPoint, ref devNo);                
                RefreshDeviceList();
            }
            catch (Exception ex) 
            {
                wbf.Hide();
                this.Activate();
                MessageBox.Show(ex.ToString(), "Create ImDisk Device (Rewritable) in Memory", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private string GetNextDriveLetter()
        {
            // LTR.IO.ImDisk.ImDiskAPI.FindFreeDriveLetter

            List<char> driveLetters = new List<char>();
            for (int i = 67; i < 91; i++) // increment from ASCII values for C-Z
            {
                driveLetters.Add(Convert.ToChar(i)); // Add uppercase letters to possible drive letters
            };

            foreach (string drive in Directory.GetLogicalDrives())
            {
                driveLetters.Remove(drive[0]); // removed used drive letters from possible drive letters
            };

            foreach (char drive in driveLetters)
                return drive.ToString() + @":\";

            return "";
        }

        private void addToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (*.img)|*.img|All types (*.*)|*.*";
            ofd.Title = "Select File";
            string fn = "";
            if (ofd.ShowDialog() == DialogResult.OK)
                fn = ofd.FileName;
            ofd.Dispose();
            if (string.IsNullOrEmpty(fn)) return;

            long size = (new FileInfo(fn)).Length;
            string mPoint = GetNextDriveLetter();
            uint devNo = 0;
            if (InputBox.Show("Create ImDisk Device (Rewritable)", "Mounting Point:", ref mPoint) != DialogResult.OK) return;
            
            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();
            while (devs.Contains((int)devNo)) devNo++;
            try
            {
                LTR.IO.ImDisk.ImDiskAPI.CreateDevice(size, fn, LTR.IO.ImDisk.ImDiskAPI.MemoryType.VirtualMemory, mPoint, ref devNo);
                RefreshDeviceList();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Create ImDisk Device (Rewritable)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
        }

        private void addVDDFromFileInPhysMemoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (*.img)|*.img|All types (*.*)|*.*";
            ofd.Title = "Select File";
            string fn = "";
            if (ofd.ShowDialog() == DialogResult.OK)
                fn = ofd.FileName;
            ofd.Dispose();
            if (string.IsNullOrEmpty(fn)) return;

            long size = (new FileInfo(fn)).Length;
            string mPoint = GetNextDriveLetter();
            uint devNo = 0;
            if (InputBox.Show("Create ImDisk Device (Rewritable)", "Mounting Point:", ref mPoint) != DialogResult.OK) return;

            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();
            while (devs.Contains((int)devNo)) devNo++;
            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Create ImDisk Device (Rewritable)", "Wait, Creating Device...", this);
            try
            {
                LTR.IO.ImDisk.ImDiskAPI.CreateDevice(size, fn, LTR.IO.ImDisk.ImDiskAPI.MemoryType.PhysicalMemory, mPoint, ref devNo);
                RefreshDeviceList();
            }
            catch (Exception ex)
            {
                wbf.Hide();
                this.Activate();
                MessageBox.Show(ex.ToString(), "Create ImDisk Device (Rewritable)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void addVDDFromFileWritableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (*.img)|*.img|All types (*.*)|*.*";
            ofd.Title = "Select File";
            string fn = "";
            if (ofd.ShowDialog() == DialogResult.OK)
                fn = ofd.FileName;
            ofd.Dispose();
            if (string.IsNullOrEmpty(fn)) return;

            long size = (new FileInfo(fn)).Length;
            string mPoint = GetNextDriveLetter();
            uint devNo = 0;
            if (InputBox.Show("Create ImDisk Device (Rewritable)", "Mounting Point:", ref mPoint) != DialogResult.OK) return;

            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();
            while (devs.Contains((int)devNo)) devNo++;
            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Create ImDisk Device (Rewritable)", "Wait, Creating Device...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("imdisk", "-a" + (string.IsNullOrEmpty(mPoint) ? "" : " -m " + mPoint) + " -n -o rw" + " -f " + "\"" + fn + "\" -u " + devNo.ToString());
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                proc.WaitForExit();
                RefreshDeviceList();
            }
            catch (Exception ex)
            {
                wbf.Hide();
                this.Activate();
                MessageBox.Show(ex.ToString(), "Create ImDisk Device", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void addVDDFromFileReadonlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (*.img)|*.img|All types (*.*)|*.*";
            ofd.Title = "Select File";
            string fn = "";
            if (ofd.ShowDialog() == DialogResult.OK)
                fn = ofd.FileName;
            ofd.Dispose();
            if (string.IsNullOrEmpty(fn)) return;

            long size = (new FileInfo(fn)).Length;
            string mPoint = GetNextDriveLetter();
            uint devNo = 0;
            if (InputBox.Show("Create ImDisk Device (Read Only)", "Mounting Point:", ref mPoint) != DialogResult.OK) return;

            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();
            while (devs.Contains((int)devNo)) devNo++;
            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Create ImDisk Device (Read Only)", "Wait, Creating Device...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("imdisk", "-a" + (string.IsNullOrEmpty(mPoint) ? "" : " -m " + mPoint) + " -n -o ro" + " -f " + "\"" + fn + "\" -u " + devNo.ToString());
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                proc.WaitForExit();
                RefreshDeviceList();
            }
            catch (Exception ex)
            {
                wbf.Hide();
                this.Activate();
                MessageBox.Show(ex.ToString(), "Create ImDisk Device", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void addRemoteVDDTCPIPDeviToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string mPoint = GetNextDriveLetter();
            uint devNo = 0;
            string query = "127.0.0.1:9000";
            if (InputBox.Show("Create ImDisk Device (Read Only)", "Mounting Point:", ref mPoint) != DialogResult.OK) return;
            if (InputBox.Show("Create ImDisk Device (Read Only)", "Remote Connection:", ref query) != DialogResult.OK) return;

            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();
            while (devs.Contains((int)devNo)) devNo++;
            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Create ImDisk Device (Read Only)", "Wait, Creating Device...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("imdisk", "-a" + (string.IsNullOrEmpty(mPoint) ? "" : " -m " + mPoint) + " -t proxy -n -o ro,ip -f " + query + " -u " + devNo.ToString());
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                proc.WaitForExit();
                RefreshDeviceList();
            }
            catch (Exception ex)
            {
                wbf.Hide();
                this.Activate();
                MessageBox.Show(ex.ToString(), "Create ImDisk Device", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void addRemoteVDDTCPIPDevioRWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string mPoint = GetNextDriveLetter();
            uint devNo = 0;
            string query = "127.0.0.1:9000";
            if (InputBox.Show("Create ImDisk Device (Rewritable)", "Mounting Point:", ref mPoint) != DialogResult.OK) return;
            if (InputBox.Show("Create ImDisk Device (Rewritable)", "Remote Connection:", ref query) != DialogResult.OK) return;

            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();
            while (devs.Contains((int)devNo)) devNo++;
            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Create ImDisk Device (Rewritable)", "Wait, Creating Device...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("imdisk", "-a" + (string.IsNullOrEmpty(mPoint) ? "" : " -m " + mPoint) + " -t proxy -n -o rw,ip -f " + query + " -u " + devNo.ToString());
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                proc.WaitForExit();
                RefreshDeviceList();
            }
            catch (Exception ex)
            {
                wbf.Hide();
                this.Activate();
                MessageBox.Show(ex.ToString(), "Create ImDisk Device", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void refreshToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            RefreshDevio();
        }

        private void RefreshDevio()
        {
            iView.Items.Clear();
            Process[] procs = Process.GetProcesses();
            List<ProcessPort> pps = ProcessPorts.ProcessPortMap;
            foreach (Process proc in procs)
            {
                if (proc.ProcessName.ToLower() != "devio") continue;
                int pNum = 0;
                foreach (ProcessPort pp in pps)
                    if (pp.ProcessId == proc.Id)
                        pNum = pp.PortNumber;

                string path = @"\\.\Unknown";
                string rwro = "rw";
                ManagementObjectSearcher pSearcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Caption, ExecutablePath, CommandLine FROM Win32_Process WHERE ProcessId = " + proc.Id.ToString());
                ManagementObjectCollection pObjects = pSearcher.Get();
                foreach (ManagementObject pObj in pObjects)
                {
                    try { 
                        path = pObj["CommandLine"].ToString();
                        if (path.IndexOf(" -r ") > 0) rwro = "ro";
                        int iof = path.IndexOf(@"\\.\");
                        if (iof > 0) path = path.Substring(iof + 4).Trim('\"');
                    } catch { };
                    break;
                };

                ListViewItem lvi = new ListViewItem(new string[] { path, rwro, String.Format("127.0.0.1:{0}", pNum), proc.Id.ToString() });
                iView.Items.Add(lvi);
            };
        }

        private void contextMenuStrip3_Opening(object sender, CancelEventArgs e)
        {
            killToolStripMenuItem.Enabled = iView.SelectedItems.Count > 0;
            copyConnectionStringToolStripMenuItem.Enabled = iView.SelectedItems.Count == 1;
        }

        private void killToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (iView.SelectedItems.Count == 0) return;
            int dCount = iView.SelectedItems.Count;
            if (MessageBox.Show("Do you really want to kill " + dCount.ToString() + " processes?", "Kill DevIO", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            for (int i = iView.Items.Count - 1; i >= 0; i--)
            {
                if (!iView.Items[i].Selected) continue;
                try
                {
                    string id = iView.Items[i].SubItems[3].Text;
                    Process p = Process.GetProcessById(int.Parse(id));
                    p.Kill();
                }
                catch { };
                iView.Items.RemoveAt(i);
            };
        }

        private void imDiskcplToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Process.Start("imdisk.cpl");
        }

        private void imDiskcplToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Process.Start("imdisk.cpl");
        }

        private void shareFileROToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (*.img)|*.img|All types (*.*)|*.*";
            ofd.Title = "Select File";
            string fn = "";
            if (ofd.ShowDialog() == DialogResult.OK)
                fn = ofd.FileName;
            ofd.Dispose();
            if (string.IsNullOrEmpty(fn)) return;

            int port = 9000;
            if (InputBox.Show("Share File (Read Only)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share File (Read Only)", "Wait, Sharing File...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", "-r "+port.ToString()+" \""+@"\\.\"+fn+"\"");
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);                            
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void shareFileRWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (*.img)|*.img|All types (*.*)|*.*";
            ofd.Title = "Select File";
            string fn = "";
            if (ofd.ShowDialog() == DialogResult.OK)
                fn = ofd.FileName;
            ofd.Dispose();
            if (string.IsNullOrEmpty(fn)) return;

            int port = 9000;
            if (InputBox.Show("Share File (Rewritable)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share File (Rewritable)", "Wait, Sharing File...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", port.ToString() + " \"" + @"\\.\" + fn + "\"");
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }


        private void shareDiskROToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            List<string> drives = new List<string>();
            foreach (DriveInfo drive in System.IO.DriveInfo.GetDrives()) drives.Add(drive.Name);
            string real = drives[0];
            if (InputBox.Show("Share Disk (Read Only)", "Select Disk", drives.ToArray(), ref real, true) != DialogResult.OK) return;
            real = real.Trim('\\');

            int port = 9000;
            if (InputBox.Show("Share Disk (Read Only)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share Disk (Read Only)", "Wait, Sharing Device...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", "-r " + port.ToString() + @" \\.\" + real);
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void shareDiskRWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> drives = new List<string>();
            foreach (DriveInfo drive in System.IO.DriveInfo.GetDrives()) drives.Add(drive.Name);
            string real = drives[0];
            if (InputBox.Show("Share Disk (Rewritable)", "Select Disk", drives.ToArray(), ref real, true) != DialogResult.OK) return;
            real = real.Trim('\\');

            int port = 9000;
            if (InputBox.Show("Share Disk (Rewritable)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share Disk Rewritable)", "Wait, Sharing Device...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", port.ToString() + @" \\.\" + real);
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void shareDriveROToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share Disk", "Wait, getting drives from system", this);
            List<string> drives = ListDrives();
            wbf.Hide();
            this.Activate();
            string real = drives[0];
            if (InputBox.Show("Share Disk (Read Only)", "Select Drive", drives.ToArray(), ref real, false) != DialogResult.OK) return;
            real = real.Trim('\\');

            int port = 9000;
            if (InputBox.Show("Share Disk (Read Only)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            wbf.Show("Share Disk (Read Only)", "Wait, Sharing Device ...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", "-r " + port.ToString() + @" \\.\" + real);
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private List<string> ListDrives()
        {
            List<string> drives = new List<string>();
            try
            {
                ManagementObjectSearcher pSearcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Index FROM Win32_DiskDrive");
                ManagementObjectCollection pObjects = pSearcher.Get();
                foreach (ManagementObject pObj in pObjects)
                {
                    try { drives.Add(String.Format(@"\\.\PhysicalDrive{0}", pObj["Index"].ToString())); } catch { };
                };
            }
            catch { };
            try
            {
                ManagementObjectSearcher pSearcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT DiskIndex, Index FROM Win32_DiskPartition");
                ManagementObjectCollection pObjects = pSearcher.Get();
                foreach (ManagementObject pObj in pObjects)
                {
                    try { drives.Add(String.Format(@"\\.\PhysicalDrive{0} {1}", pObj["DiskIndex"].ToString(), pObj["Index"].ToString())); } catch { };
                };
            }
            catch { };
            drives.Sort();
            return drives;
        }

        private void shareDriveRWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share Disk", "Wait, getting drives from system", this);
            List<string> drives = ListDrives();
            wbf.Hide();
            this.Activate();
            string real = drives[0];
            if (InputBox.Show("Share Disk (Rewritable)", "Select Drive", drives.ToArray(), ref real, false) != DialogResult.OK) return;
            real = real.Trim('\\');

            int port = 9000;
            if (InputBox.Show("Share Disk (Rewritable)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            wbf.Show("Share Disk (Rewritable)", "Wait, Sharing Device ...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", port.ToString() + @" \\.\" + real);
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void shareImDiskDeviceROToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();            

            List<string> devices = new List<string>();
            foreach (int d in devs) devices.Add(@"\Device\ImDisk" + d.ToString());

            string real = devices.Count > 0 ? devices[0] : @"\Device\Unknown";
            if (devices.Count > 0)
            {
                if (InputBox.Show("Share Device (Read Only)", "Select Real Device", devices.ToArray(), ref real, true) != DialogResult.OK) return;
            }
            else
            {
                if (InputBox.Show("Share Device (Read Only)", "Enter Real Device ", ref real) != DialogResult.OK) return;
            };
            
            int port = 9000;
            if (InputBox.Show("Share Device (Read Only)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            int iof = real.IndexOf("ImDisk");
            if (iof > 0) real = real.Remove(0, iof);

            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share Device (Read Only)", "Wait, Sharing Device...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", "-r " + port.ToString() + @" \\.\" + real);
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void shareImDiskDeviceRWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<int> devs = LTR.IO.ImDisk.ImDiskAPI.GetDeviceList();

            List<string> devices = new List<string>();
            foreach (int d in devs) devices.Add(@"\Device\ImDisk" + d.ToString());

            string real = devices.Count > 0 ? devices[0] : @"\Device\Unknown";
            if (devices.Count > 0)
            {
                if (InputBox.Show("Share Device (Rewritable)", "Select Real Device", devices.ToArray(), ref real, true) != DialogResult.OK) return;
            }
            else
            {
                if (InputBox.Show("Share Device (Rewritable)", "Enter Real Device", ref real) != DialogResult.OK) return;
            };

            int port = 9000;
            if (InputBox.Show("Share Device (Rewritable)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            int iof = real.IndexOf("ImDisk");
            if (iof > 0) real = real.Remove(0, iof);

            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share Device (Rewritable)", "Wait, Sharing Device...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", port.ToString() + @" \\.\" + real);
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void sharePhysicalMemoryRWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int size = 10;
            if (InputBox.Show("Share Physical Memory (Rewritable)", "Size of memory in MB:", ref size, 10, 64 * 1024) != DialogResult.OK) return;

            int port = 9000;
            if (InputBox.Show("Share Physical Memory (Rewritable)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share Physical Memory (Rewritable)", "Wait, Sharing Memory...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", port.ToString() + @" \\?\awealloc " + size.ToString() + "MB");
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void copyConnectionStringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (iView.SelectedItems.Count != 1) return;
            Clipboard.SetText(iView.SelectedItems[0].SubItems[2].Text);
        }

        private void loadFileInMemoryAndShareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (*.img)|*.img|All types (*.*)|*.*";
            ofd.Title = "Select File";
            string fn = "";
            if (ofd.ShowDialog() == DialogResult.OK)
                fn = ofd.FileName;
            ofd.Dispose();
            if (string.IsNullOrEmpty(fn)) return;

            int port = 9000;
            if (InputBox.Show("Share File in Memory (Rewritable)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share File in Memory (Rewritable)", "Wait, Sharing Memory...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", port.ToString() + " \"" + @"\\?\awealloc\??\" + fn + "\"");
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void shareDiskROToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            List<string> drives = new List<string>();
            foreach (DriveInfo drive in System.IO.DriveInfo.GetDrives()) drives.Add(drive.Name);
            string real = drives[0];
            if (InputBox.Show("Share Disk (Read Only)", "Select Disk", drives.ToArray(), ref real, true) != DialogResult.OK) return;
            real = real.Trim('\\');

            int port = 9000;
            if (InputBox.Show("Share Disk (Read Only)", "Select Port", ref port, 1000, 65000) != DialogResult.OK) return;

            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Share Disk (Read Only)", "Wait, Sharing Device...", this);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("devio.exe", "-r " + port.ToString() + @" \\.\" + real);
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Process proc = Process.Start(psi);
                System.Threading.Thread.Sleep(1000);
                RefreshDevio();
            }
            catch (Exception ex) { };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void saveToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dView.SelectedItems.Count != 1) return;
            DListViewItem di = (DListViewItem)dView.SelectedItems[0];
            LTR.IO.ImDisk.ImDiskDevice d = null;
            WaitingBoxForm wbf = new WaitingBoxForm();            
            try
            {
                d = new LTR.IO.ImDisk.ImDiskDevice((uint)di.ID, FileAccess.Read);
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.DefaultExt = ".img";
                sfd.Title = "Save Image to File";
                sfd.Filter = "Image Files (*.img)|*.img|All types (*.*)|*.*";
                string fn = "";
                if (sfd.ShowDialog() == DialogResult.OK)
                    fn = sfd.FileName;
                sfd.Dispose();
                if (!string.IsNullOrEmpty(fn))
                {
                    wbf.Show("Save Image to File", "Wait, saving...", this);
                    d.SaveImageFile(fn);
                };
                d.Close();
            }
            catch (Exception ex) 
            {
                wbf.Hide();
                this.Activate();
                MessageBox.Show(ex.ToString(), "Save Image to File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (d != null) d.Close();
            };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void mountInTrueCryptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dView.SelectedItems.Count != 1) return;            
            DListViewItem di = (DListViewItem)dView.SelectedItems[0];

            if (dView.SelectedItems[0].SubItems[4].Text.Contains("ReadOnly"))
            {
                MessageBox.Show("TrueCrypt can open only Rewritable Devices!", "Mounting in TrueCrypt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            };

            string mPoint = GetNextDriveLetter();
            string passw = "";
            uint devNo = (uint)di.ID;
            if (InputBox.Show("Mounting in TrueCrypt", "Choose Letter Drive:", ref mPoint) != DialogResult.OK) return;
            if (string.IsNullOrEmpty(mPoint)) return;
            mPoint = mPoint.Trim('\\');
            if (InputBox.QueryPass("Mounting in TrueCrypt", "Enter Password:", ref passw) != DialogResult.OK) return;            

            WaitingBoxForm wbf = new WaitingBoxForm();
            wbf.Show("Mounting in TrueCrypt", "Wait, Mounting Device ...");
            try
            {

                ProcessStartInfo psi = new ProcessStartInfo("TrueCrypt.exe", "/v \\Device\\ImDisk" + devNo.ToString() + " /l " + mPoint + (string.IsNullOrEmpty(passw) ? "" : " /p \"" + passw + "\"") + " /s /q");
                psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                psi.Verb = "runas";
                psi.UseShellExecute = false;
                Clipboard.SetText(psi.Arguments);
                Process proc = Process.Start(psi);
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                wbf.Hide();
                this.Activate();
                MessageBox.Show(ex.ToString(), "Mounting in TrueCrypt", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            wbf.Hide();
            wbf.Close();
            this.Activate();
        }

        private void trueCryptexeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("TrueCrypt.exe");
        }
    }

    internal class DListViewItem: ListViewItem
    {
        public int ID = -1;
        public DListViewItem(int ID): base("ImDisk" + ID.ToString())
        {
            this.ID = ID;
        }
    }
}
