using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName = ".NET Framework 4.8")]
namespace MaaChildHost
{
    internal static class NativeMethods
    {
        public const uint NoChildSessionId = uint.MaxValue;
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSEnableChildSessions([MarshalAs(UnmanagedType.Bool)] bool enable);
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSIsChildSessionsEnabled([MarshalAs(UnmanagedType.Bool)] out bool enabled);
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSGetChildSessionId(out uint sessionId);
        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSLogoffSession(IntPtr serverHandle, uint sessionId, [MarshalAs(UnmanagedType.Bool)] bool wait);
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentProcessId();
        [DllImport("kernel32.dll")]
        public static extern bool ProcessIdToSessionId(uint processId, out uint sessionId);
        public static uint GetCurrentSessionId()
        {
            uint sid;
            if (!ProcessIdToSessionId(GetCurrentProcessId(), out sid)) return 0;
            return sid;
        }
        public static uint? TryGetChildSessionId()
        {
            uint sid;
            if (!WTSGetChildSessionId(out sid)) return null;
            if (sid == NoChildSessionId || sid == GetCurrentSessionId()) return null;
            return sid;
        }
        [DllImport("kernel32.dll")]
        public static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();
        public const int ATTACH_PARENT_PROCESS = -1;
    }
    internal static class CredentialStore
    {
        private static string GetCredFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "session_cred.dat");
        }
        public static void SavePassword(string password)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(password);
            byte[] cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(GetCredFilePath(), cipherBytes);
        }
        public static string LoadPassword()
        {
            string filePath = GetCredFilePath();
            if (!File.Exists(filePath)) return null;
            try
            {
                byte[] cipherBytes = File.ReadAllBytes(filePath);
                byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }
        public static void ClearPassword()
        {
            try
            {
                string path = GetCredFilePath();
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }
    }
    [ComImport]
    [Guid("302D8188-0052-4807-806A-362B628F9AC5")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMsRdpExtendedSettings
    {
        void set_Property(
            [In, MarshalAs(UnmanagedType.BStr)] string propertyName,
            [In, MarshalAs(UnmanagedType.Struct)] ref object value);
        [return: MarshalAs(UnmanagedType.Struct)]
        object get_Property([In, MarshalAs(UnmanagedType.BStr)] string propertyName);
    }
    [ComImport]
    [Guid("2F079C4C-87B2-4AFD-97AB-20CDB43038AE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMsRdpClientNonScriptable
    {
        void put_ClearTextPassword([In, MarshalAs(UnmanagedType.BStr)] string value);
        void put_PortablePassword([In, MarshalAs(UnmanagedType.BStr)] string value);
        [return: MarshalAs(UnmanagedType.BStr)] string get_PortablePassword();
        void put_PortableSalt([In, MarshalAs(UnmanagedType.BStr)] string value);
        [return: MarshalAs(UnmanagedType.BStr)] string get_PortableSalt();
        void put_BinaryPassword([In, MarshalAs(UnmanagedType.BStr)] string value);
        [return: MarshalAs(UnmanagedType.BStr)] string get_BinaryPassword();
        void put_BinarySalt([In, MarshalAs(UnmanagedType.BStr)] string value);
        [return: MarshalAs(UnmanagedType.BStr)] string get_BinarySalt();
        void ResetPassword();
    }
    [ComImport]
    [Guid("336D5562-EFA8-482E-8CB3-C5C0FC7A7DB6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    [TypeLibType(TypeLibTypeFlags.FDispatchable)]
    internal interface IMsTscAxEvents
    {
        [DispId(1)] void OnConnecting();
        [DispId(2)] void OnConnected();
        [DispId(3)] void OnLoginComplete();
        [DispId(4)] void OnDisconnected([In] int disconnectReason);
        [DispId(5)] void OnEnterFullScreenMode();
        [DispId(6)] void OnLeaveFullScreenMode();
        [DispId(7)] void OnChannelReceivedData([In, MarshalAs(UnmanagedType.BStr)] string channelName, [In, MarshalAs(UnmanagedType.BStr)] string data);
        [DispId(8)] void OnRequestGoFullScreen();
        [DispId(9)] void OnRequestLeaveFullScreen();
        [DispId(10)] void OnFatalError([In] int errorCode);
        [DispId(11)] void OnWarning([In] int warningCode);
        [DispId(12)] void OnRemoteDesktopSizeChange([In] int width, [In] int height);
        [DispId(13)] void OnIdleTimeoutNotification();
        [DispId(14)] void OnRequestContainerMinimize();
        [DispId(15)] void OnConfirmClose([Out] out bool allowClose);
        [DispId(16)] void OnReceivedTSPublicKey([In, MarshalAs(UnmanagedType.BStr)] string publicKey, [Out] out bool continueLogon);
        [DispId(17)] void OnAutoReconnecting([In] int disconnectReason, [In] int attemptCount, [Out] out int continueStatus);
        [DispId(18)] void OnAuthenticationWarningDisplayed();
        [DispId(19)] void OnAuthenticationWarningDismissed();
        [DispId(20)] void OnRemoteProgramResult([In, MarshalAs(UnmanagedType.BStr)] string remoteProgram, [In] int error, [In] bool isExecutable);
        [DispId(21)] void OnRemoteProgramDisplayed([In] bool displayed, [In] uint displayInformation);
        [DispId(29)] void OnRemoteWindowDisplayed([In] bool displayed, [In] ref IntPtr windowHandle, [In] int windowAttribute);
        [DispId(22)] void OnLogonError([In] int errorCode);
        [DispId(23)] void OnFocusReleased([In] int direction);
        [DispId(24)] void OnUserNameAcquired([In, MarshalAs(UnmanagedType.BStr)] string userName);
        [DispId(26)] void OnMouseInputModeChanged([In] bool isRelativeMouseMode);
        [DispId(28)] void OnServiceMessageReceived([In, MarshalAs(UnmanagedType.BStr)] string serviceMessage);
        [DispId(30)] void OnConnectionBarPullDown();
        [DispId(32)] void OnNetworkStatusChanged([In] uint qualityLevel, [In] int bandwidth, [In] int roundTripTime);
        [DispId(35)] void OnDevicesButtonPressed();
        [DispId(33)] void OnAutoReconnected();
        [DispId(34)] void OnAutoReconnecting2([In] int disconnectReason, [In] bool networkAvailable, [In] int attemptCount, [In] int maxAttemptCount);
    }
    internal sealed class RdpActiveXControl : AxHost
    {
        private const string RdpClientClsid = "A0C63C30-F08D-4AB4-907C-34905D770C7D";
        private ConnectionPointCookie _eventCookie;
        private RdpEventSink _eventSink;
        public event EventHandler Connected;
        public event EventHandler LoginComplete;
        public event EventHandler<int> Disconnected;
        public event EventHandler<int> FatalError;
        public RdpActiveXControl() : base(RdpClientClsid)
        {
            Dock = DockStyle.Fill;
        }
        protected override void CreateSink()
        {
            base.CreateSink();
            _eventSink = new RdpEventSink(this);
            _eventCookie = new ConnectionPointCookie(GetOcx(), _eventSink, typeof(IMsTscAxEvents));
        }
        protected override void DetachSink()
        {
            try
            {
                if (_eventCookie != null)
                {
                    _eventCookie.Disconnect();
                    _eventCookie = null;
                }
                _eventSink = null;
            }
            finally
            {
                base.DetachSink();
            }
        }
        public void ConnectChild(int width, int height, string password)
        {
            if (!IsHandleCreated) { IntPtr h = Handle; }
            object ocx = GetOcx();
            if (ocx == null) throw new InvalidOperationException("RDP OCX not ready");
            SetComProp(ocx, "Server", "127.0.0.2");
            SetComProp(ocx, "UserName", Environment.UserName);
            SetComProp(ocx, "Domain", ".");
            SetComProp(ocx, "DesktopWidth", width);
            SetComProp(ocx, "DesktopHeight", height);
            SetComProp(ocx, "ColorDepth", 32);
            object adv = GetComProp(ocx, "AdvancedSettings7");
            SetComProp(adv, "RDPPort", 3389);
            SetComProp(adv, "EnableCredSspSupport", true);
            SetComProp(adv, "SmartSizing", false);
            SetComProp(adv, "EnableWindowsKey", 1);
            SetComProp(adv, "RedirectClipboard", false);
            SetComProp(adv, "RedirectDrives", false);
            SetComProp(adv, "RedirectPrinters", false);
            SetComProp(adv, "RedirectPorts", false);
            SetComProp(adv, "RedirectSmartCards", false);
            object secured = GetComProp(ocx, "SecuredSettings2");
            if (secured != null)
            {
                SetComProp(secured, "KeyboardHookMode", 1);
                SetComProp(secured, "AudioRedirectionMode", 2);
            }
            // 原生密码注入：直接将本地密码送入非脚本化客户端接口，彻底免除外部安全中心弹窗
            if (!string.IsNullOrEmpty(password))
            {
                IMsRdpClientNonScriptable nonScriptable = ocx as IMsRdpClientNonScriptable;
                if (nonScriptable != null)
                {
                    nonScriptable.put_ClearTextPassword(password);
                }
            }
            IMsRdpExtendedSettings extendedSettings = (IMsRdpExtendedSettings)ocx;
            object val = true;
            extendedSettings.set_Property("ConnectToChildSession", ref val);
            ocx.GetType().InvokeMember("Connect", BindingFlags.InvokeMethod, null, ocx, null, CultureInfo.InvariantCulture);
        }
        private static void SetComProp(object target, string name, object val)
        {
            target.GetType().InvokeMember(name, BindingFlags.SetProperty, null, target, new object[] { val }, CultureInfo.InvariantCulture);
        }
        private static object GetComProp(object target, string name)
        {
            return target.GetType().InvokeMember(name, BindingFlags.GetProperty, null, target, null, CultureInfo.InvariantCulture);
        }
        [ComVisible(true)]
        [ClassInterface(ClassInterfaceType.None)]
        private sealed class RdpEventSink : IMsTscAxEvents
        {
            private readonly RdpActiveXControl _owner;
            public RdpEventSink(RdpActiveXControl owner) { _owner = owner; }
            public void OnConnecting() {}
            public void OnConnected() { if (_owner.Connected != null) _owner.Connected(_owner, EventArgs.Empty); }
            public void OnLoginComplete() { if (_owner.LoginComplete != null) _owner.LoginComplete(_owner, EventArgs.Empty); }
            public void OnDisconnected(int reason) { if (_owner.Disconnected != null) _owner.Disconnected(_owner, reason); }
            public void OnEnterFullScreenMode() {}
            public void OnLeaveFullScreenMode() {}
            public void OnChannelReceivedData(string c, string d) {}
            public void OnRequestGoFullScreen() {}
            public void OnRequestLeaveFullScreen() {}
            public void OnFatalError(int err) { if (_owner.FatalError != null) _owner.FatalError(_owner, err); }
            public void OnWarning(int w) {}
            public void OnRemoteDesktopSizeChange(int w, int h) {}
            public void OnIdleTimeoutNotification() {}
            public void OnRequestContainerMinimize() {}
            public void OnConfirmClose(out bool allowClose) { allowClose = true; }
            public void OnReceivedTSPublicKey(string k, out bool c) { c = true; }
            public void OnAutoReconnecting(int r, int a, out int s) { s = 0; }
            public void OnAuthenticationWarningDisplayed() {}
            public void OnAuthenticationWarningDismissed() {}
            public void OnRemoteProgramResult(string p, int e, bool x) {}
            public void OnRemoteProgramDisplayed(bool d, uint i) {}
            public void OnRemoteWindowDisplayed(bool d, ref IntPtr h, int a) {}
            public void OnLogonError(int err) {}
            public void OnFocusReleased(int d) {}
            public void OnUserNameAcquired(string u) {}
            public void OnMouseInputModeChanged(bool m) {}
            public void OnServiceMessageReceived(string m) {}
            public void OnConnectionBarPullDown() {}
            public void OnNetworkStatusChanged(uint q, int b, int r) {}
            public void OnDevicesButtonPressed() {}
            public void OnAutoReconnected() { if (_owner.LoginComplete != null) _owner.LoginComplete(_owner, EventArgs.Empty); }
            public void OnAutoReconnecting2(int r, bool n, int a, int m) {}
        }
    }
    internal static class ChildLauncher
    {
        public static void LaunchBackend(uint parentPid)
        {
            uint? sessionId = NativeMethods.TryGetChildSessionId();
            if (!sessionId.HasValue) throw new InvalidOperationException("Child session is not ready.");
            string exe = MainForm.ResolveMaaEndPath(null);
            if (!File.Exists(exe)) throw new FileNotFoundException("MaaEnd backend executable not found: " + exe, exe);
            Launch(sessionId.Value, exe, "--child-backend " + parentPid.ToString(CultureInfo.InvariantCulture), Path.GetDirectoryName(exe));
        }
        public static void Launch(uint sessionId, string exePath, string args, string workingDir)
        {
            Type schedulerType = Type.GetTypeFromProgID("Schedule.Service");
            if (schedulerType == null) throw new InvalidOperationException("未找到任务计划程序 COM 服务");
            object schedulerObj = Activator.CreateInstance(schedulerType);
            dynamic scheduler = schedulerObj;
            scheduler.Connect();
            dynamic rootFolder = scheduler.GetFolder("\\");
            dynamic taskDef = scheduler.NewTask(0);
            string taskName = "MaaEnd-ChildLaunch-" + Guid.NewGuid().ToString("N");
            string accountName = WindowsIdentity.GetCurrent().Name;
            taskDef.RegistrationInfo.Author = "MaaEnd";
            taskDef.Settings.Enabled = true;
            taskDef.Settings.Hidden = true;
            taskDef.Settings.AllowDemandStart = true;
            taskDef.Settings.ExecutionTimeLimit = "PT0S";
            taskDef.Settings.DisallowStartIfOnBatteries = false;
            taskDef.Settings.StopIfGoingOnBatteries = false;
            taskDef.Principal.UserId = accountName;
            taskDef.Principal.LogonType = 3;
            taskDef.Principal.RunLevel = 1;
            dynamic action = taskDef.Actions.Create(0);
            action.Path = exePath;
            action.Arguments = args;
            action.WorkingDirectory = string.IsNullOrEmpty(workingDir) ? Path.GetDirectoryName(exePath) : workingDir;
            dynamic registeredTask = rootFolder.RegisterTaskDefinition(
                taskName, taskDef, 2, accountName, null, 3, null);
            try
            {
                registeredTask.RunEx(null, 0x4, (int)sessionId, null);
            }
            finally
            {
                try { rootFolder.DeleteTask(taskName, 0); } catch { }
                Marshal.ReleaseComObject(registeredTask);
                Marshal.ReleaseComObject(action);
                Marshal.ReleaseComObject(taskDef);
                Marshal.ReleaseComObject(rootFolder);
                Marshal.ReleaseComObject(schedulerObj);
            }
        }
    }
    public class PasswordDialog : Form
    {
        public string EnteredPassword { get; private set; }
        public bool RememberPassword { get; private set; }
        private TextBox _txtPassword;
        private CheckBox _chkRemember;
        public PasswordDialog()
        {
            Text = "桌面分身登录凭据设置";
            ClientSize = new Size(380, 190);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            Label lblInfo = new Label {
                Text = "请输入当前账户 (" + Environment.UserName + ") 的本地登录密码：\n(该密码仅用于本地 Child Session 原生注入认证)",
                Location = new Point(20, 15),
                Size = new Size(340, 36)
            };
            _txtPassword = new TextBox {
                Location = new Point(20, 60),
                Width = 340,
                UseSystemPasswordChar = true
            };
            _chkRemember = new CheckBox {
                Text = "记住密码 (Windows DPAPI 原生安全加密保存)",
                Location = new Point(20, 95),
                Width = 340,
                Checked = true
            };
            Button btnOk = new Button { Text = "确定连接", Location = new Point(190, 140), Width = 80, Height = 28, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "取消", Location = new Point(280, 140), Width = 80, Height = 28, DialogResult = DialogResult.Cancel };
            btnOk.Click += (s, e) => {
                EnteredPassword = _txtPassword.Text;
                RememberPassword = _chkRemember.Checked;
                Close();
            };
            Controls.Add(lblInfo);
            Controls.Add(_txtPassword);
            Controls.Add(_chkRemember);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
    public class MainForm : Form
    {
        private readonly RdpActiveXControl _rdpControl;
        private readonly NotifyIcon _trayIcon;
        private readonly Label _lblStatus;
        private readonly Button _btnLaunchMaaEnd;
        private readonly Button _btnLaunchGame;
        private readonly Button _btnChangeCred;
        private readonly Button _btnHide;
        private readonly Button _btnLogoff;
        private uint? _childSessionId;
        private uint? _managedSessionId;
        private bool _explicitExit = false;
        private EventWaitHandle _showWaitHandle;
        private Thread _showListenerThread;
        private bool _startHidden = false;
        private bool _launchMaaEndOnConnect = false;
        private uint? _backendParentPid;

        private string _customMaaEndPath = null;

        public MainForm(bool startHidden = false, bool launchMaaEndOnConnect = false, uint? backendParentPid = null, string customMaaEndPath = null)
        {
            _startHidden = startHidden;
            _launchMaaEndOnConnect = launchMaaEndOnConnect;
            _backendParentPid = backendParentPid;
            _customMaaEndPath = customMaaEndPath;
            if (backendParentPid > 0)
            {
                Opacity = 0;
                ShowInTaskbar = false;
            }
        
            Text = "MaaEnd 桌面分身 (Windows 原生 Child Session)";
            ClientSize = new Size(1600, 940);
            StartPosition = FormStartPosition.CenterScreen;
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(245, 245, 247) };
            _lblStatus = new Label { Text = "正在初始化桌面分身...", AutoSize = true, Location = new Point(12, 12), Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) };
            _btnLaunchMaaEnd = new Button { Text = "打开分身UI", Location = new Point(340, 7), Width = 85, Height = 26, Enabled = false };
            _btnLaunchGame = new Button { Text = "启动游戏", Location = new Point(430, 7), Width = 75, Height = 26, Enabled = false };
            _btnChangeCred = new Button { Text = "重设密码", Location = new Point(510, 7), Width = 75, Height = 26 };
            _btnHide = new Button { Text = "隐藏至托盘", Location = new Point(590, 7), Width = 85, Height = 26 };
            _btnLogoff = new Button { Text = "注销分身", Location = new Point(680, 7), Width = 75, Height = 26 };
            topPanel.Controls.Add(_lblStatus);
            topPanel.Controls.Add(_btnLaunchMaaEnd);
            topPanel.Controls.Add(_btnLaunchGame);
            topPanel.Controls.Add(_btnChangeCred);
            topPanel.Controls.Add(_btnHide);
            topPanel.Controls.Add(_btnLogoff);
            Controls.Add(topPanel);
            Panel rdpContainer = new Panel { Dock = DockStyle.Fill };
            _rdpControl = new RdpActiveXControl();
            rdpContainer.Controls.Add(_rdpControl);
            Controls.Add(rdpContainer);
            rdpContainer.BringToFront();
            AllowDrop = true;
            topPanel.AllowDrop = true;
            rdpContainer.AllowDrop = true;
            DragEventHandler onDragEnter = delegate(object s, DragEventArgs e) {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            DragEventHandler onDragDrop = delegate(object s, DragEventArgs e) {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string resolved = ResolveMaaEndPath(files[0]);
                    if (File.Exists(resolved))
                    {
                        _customMaaEndPath = resolved;
                        LaunchMaaEnd();
                    }
                }
            };
            DragEnter += onDragEnter;
            DragDrop += onDragDrop;
            topPanel.DragEnter += onDragEnter;
            topPanel.DragDrop += onDragDrop;
            rdpContainer.DragEnter += onDragEnter;
            rdpContainer.DragDrop += onDragDrop;
            _trayIcon = new NotifyIcon
            {
                Text = "MaaEnd 桌面分身",
                Icon = SystemIcons.Application,
                Visible = true
            };
            ContextMenu trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("显示/隐藏 桌面分身", delegate(object s, EventArgs e) { ToggleVisibility(); });
            trayMenu.MenuItems.Add("注销分身并退出", delegate(object s, EventArgs e) { DoLogoffAndExit(); });
            _trayIcon.ContextMenu = trayMenu;
            _trayIcon.DoubleClick += delegate(object s, EventArgs e) { ToggleVisibility(); };
            _btnHide.Click += delegate(object s, EventArgs e) { HideToTray(); };
            _btnLaunchMaaEnd.Click += delegate(object s, EventArgs e) { LaunchMaaEnd(); };
            _btnLaunchGame.Click += delegate(object s, EventArgs e) { LaunchGame(); };
            _btnChangeCred.Click += delegate(object s, EventArgs e) { ChangePassword(); };
            _btnLogoff.Click += delegate(object s, EventArgs e) { DoLogoffAndExit(); };
            _rdpControl.Connected += delegate(object s, EventArgs e) { UpdateStatus("物理通道已连通，正在进入分身桌面..."); };
            _rdpControl.LoginComplete += delegate(object s, EventArgs e) { OnLoginComplete(); };
            _rdpControl.Disconnected += delegate(object s, int reason) { OnConnectionLost("连接已断开 (代码: " + reason + ")"); };
            _rdpControl.FatalError += delegate(object s, int err) { OnConnectionLost("发生致命错误: " + err); };
            _showWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "MaaEndChildSessionHost_ShowEvent");
            _showListenerThread = new Thread(delegate()
            {
                while (!_explicitExit)
                {
                    try
                    {
                        if (_showWaitHandle.WaitOne())
                        {
                            if (_explicitExit) break;
                            Invoke(new Action(RestoreAndShow));
                        }
                    }
                    catch { break; }
                }
            }) { IsBackground = true };
            _showListenerThread.Start();
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            
            StartConnection();
        }
        private void StartConnection()
        {
            NativeMethods.WTSEnableChildSessions(true);
            string password = CredentialStore.LoadPassword();
            UpdateStatus("正在连接原生 Child Session (1600x900)...");
            try
            {
                _rdpControl.ConnectChild(1600, 900, password);
            }
            catch (Exception ex)
            {
                UpdateStatus("连接失败: " + ex.Message);
            }
        }
        private void ChangePassword()
        {
            using (var dlg = new PasswordDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dlg.EnteredPassword))
                {
                    if (dlg.RememberPassword) CredentialStore.SavePassword(dlg.EnteredPassword);
                    else CredentialStore.ClearPassword();
                    if (!_childSessionId.HasValue)
                    {
                        StartConnection();
                    }
                    else
                    {
                        MessageBox.Show("密码已更新！下次启动或重连时将生效。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        private void OnLoginComplete()
        {
            if (InvokeRequired) { Invoke(new Action(OnLoginComplete)); return; }
            ThreadPool.QueueUserWorkItem(delegate
            {
                uint? sid = null;
                for (int i = 0; i < 10; i++)
                {
                    sid = NativeMethods.TryGetChildSessionId();
                    if (sid.HasValue) break;
                    Thread.Sleep(200);
                }
                Invoke(new Action(delegate
                {
                    if (sid.HasValue)
                    {
                        _childSessionId = sid.Value;
                        _managedSessionId = sid.Value;
                        UpdateStatus("桌面分身运行中 (Child Session ID: " + sid.Value + ")");
                        _btnLaunchMaaEnd.Enabled = true;
                        _btnLaunchGame.Enabled = true;
                        if (_backendParentPid.HasValue)
                        {
                            uint parentPid = _backendParentPid.Value;
                            _backendParentPid = null;
                            ChildLauncher.LaunchBackend(parentPid);
                            if (_startHidden) Hide();
                        }
                        else if (_launchMaaEndOnConnect)
                        {
                            _launchMaaEndOnConnect = false;
                            LaunchMaaEnd();
                        }
                    }
                    else
                    {
                        if (_backendParentPid.HasValue) throw new InvalidOperationException("Child session is not ready after login.");
                        UpdateStatus("已完成登录，等待受管会话同步...");
                    }
                }));
            });
        }
        private void OnConnectionLost(string reasonMsg)
        {
            if (InvokeRequired) { Invoke(new Action<string>(OnConnectionLost), reasonMsg); return; }
            _childSessionId = null;
            _btnLaunchMaaEnd.Enabled = false;
            _btnLaunchGame.Enabled = false;
            UpdateStatus(reasonMsg);
        }
        private void UpdateStatus(string msg)
        {
            if (InvokeRequired) { Invoke(new Action<string>(UpdateStatus), msg); return; }
            _lblStatus.Text = msg;
            Console.WriteLine("[ChildSessionHost] " + msg);
        }
        private uint? VerifyTargetSession()
        {
            if (!_childSessionId.HasValue) return null;
            uint? actualSid = NativeMethods.TryGetChildSessionId();
            if (!actualSid.HasValue || actualSid.Value != _childSessionId.Value)
            {
                _childSessionId = null;
                _btnLaunchMaaEnd.Enabled = false;
                _btnLaunchGame.Enabled = false;
                UpdateStatus("目标 Child Session 已失效或发生变更，操作已取消");
                return null;
            }
            return actualSid.Value;
        }
        public static string ResolveMaaEndPath(string customPath)
        {
            if (!string.IsNullOrEmpty(customPath))
            {
                customPath = customPath.Trim('"');
                if (customPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && File.Exists(customPath))
                {
                    try
                    {
                        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                        if (shellType != null)
                        {
                            dynamic shell = Activator.CreateInstance(shellType);
                            dynamic sc = shell.CreateShortcut(customPath);
                            string targetPath = sc.TargetPath;
                            if (File.Exists(targetPath)) return targetPath;
                        }
                    }
                    catch { }
                }
                if (File.Exists(customPath)) return Path.GetFullPath(customPath);
                if (Directory.Exists(customPath))
                {
                    string cand = Path.Combine(customPath, "MaaEnd.exe");
                    if (File.Exists(cand)) return cand;
                }
            }

            string[] candidates = new string[]
            {
                @"D:\Program Files\MaaEnd\MaaEnd.exe",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MaaEnd.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"MaaEnd\MaaEnd.exe"),
                @"C:\Program Files\MaaEnd\MaaEnd.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\MaaEnd\MaaEnd.exe")
            };

            foreach (string p in candidates)
            {
                if (File.Exists(p)) return p;
            }
            return candidates[0];
        }

        private void LaunchMaaEnd()
        {
            uint? targetSid = VerifyTargetSession();
            if (!targetSid.HasValue)
            {
                MessageBox.Show("桌面分身会话未就绪或已失效，操作已终止。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string exe = ResolveMaaEndPath(_customMaaEndPath);
            if (!File.Exists(exe))
            {
                MessageBox.Show("未找到 MaaEnd: " + exe + "\r\n\r\n提示：您可以直接将 MaaEnd.exe 或其快捷方式拖拽到启动脚本上运行。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                ChildLauncher.Launch(targetSid.Value, exe, "", Path.GetDirectoryName(exe));
                UpdateStatus("已向 Child Session " + targetSid.Value + " 提交 MaaEnd 启动请求");
            }
            catch (Exception ex)
            {
                MessageBox.Show("提交启动失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static string ResolveGamePath()
        {
            string[] candidates = new string[]
            {
                @"D:\Program Files (x86)\Hypergryph Launcher\Launcher.exe",
                @"C:\Program Files (x86)\Hypergryph Launcher\Launcher.exe",
                @"D:\Program Files\Hypergryph Launcher\Launcher.exe",
                @"C:\Program Files\Hypergryph Launcher\Launcher.exe",
                @"D:\Program Files (x86)\Hypergryph Launcher\games\Endfield Game\Endfield.exe",
                @"C:\Program Files (x86)\Hypergryph Launcher\games\Endfield Game\Endfield.exe",
                @"D:\Program Files\Hypergryph Launcher\games\Endfield Game\Endfield.exe",
                @"C:\Program Files\Hypergryph Launcher\games\Endfield Game\Endfield.exe"
            };
            foreach (string p in candidates)
            {
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private void LaunchGame()
        {
            uint? targetSid = VerifyTargetSession();
            if (!targetSid.HasValue)
            {
                MessageBox.Show("桌面分身会话未就绪或已失效，操作已终止。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string target = ResolveGamePath();
            if (target == null)
            {
                MessageBox.Show("未找到游戏或启动器", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            bool isLauncher = target.EndsWith("Launcher.exe", StringComparison.OrdinalIgnoreCase);
            try
            {
                ChildLauncher.Launch(targetSid.Value, target, "", Path.GetDirectoryName(target));
                string desc = isLauncher ? "鹰角启动器" : "终末地客户端";
                UpdateStatus("已向 Child Session " + targetSid.Value + " 提交" + desc + "启动任务");
            }
            catch (Exception ex)
            {
                MessageBox.Show("提交任务失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void HideToTray()
        {
            Hide();
            _trayIcon.ShowBalloonTip(2000, "MaaEnd 桌面分身", "窗口已隐藏到系统托盘，子会话与游戏后台挂机保持运行中。\n双击托盘图标可重新显示。", ToolTipIcon.Info);
        }
        public void RestoreAndShow()
        {
            Opacity = 1;
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
            TopMost = true;
            TopMost = false;
        }
        private void ToggleVisibility()
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                RestoreAndShow();
            }
        }
        private void DoLogoffAndExit()
        {
            _explicitExit = true;
            uint? activeChildId = NativeMethods.TryGetChildSessionId();
            uint currentHostSessionId = NativeMethods.GetCurrentSessionId();
            uint? targetSid = _childSessionId.HasValue ? _childSessionId : _managedSessionId;
            if (activeChildId.HasValue && targetSid.HasValue && activeChildId.Value == targetSid.Value)
            {
                if (activeChildId.Value != currentHostSessionId && activeChildId.Value != NativeMethods.NoChildSessionId)
                {
                    bool logoffOk = NativeMethods.WTSLogoffSession(IntPtr.Zero, activeChildId.Value, false);
                    Console.WriteLine("[ChildSessionHost] Logoff child session " + activeChildId.Value + " result: " + logoffOk);
                }
            }
            _childSessionId = null;
            _managedSessionId = null;
            _trayIcon.Visible = false;
            try { if (_showWaitHandle != null) _showWaitHandle.Set(); } catch { }
            Close();
            Application.Exit();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !_explicitExit)
            {
                e.Cancel = true;
                HideToTray();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
    }
    internal static class Program
    {


        private static void RunEnvironmentDiagnostics()
        {
            bool attached = NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
            if (!attached && !Console.IsInputRedirected)
            {
                NativeMethods.AllocConsole();
            }
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }
            try { Console.Title = "MaaEnd 桌面分身 - 系统环境与兼容性检测"; } catch { }

            Console.WriteLine();
            Console.WriteLine("======================================================");
            Console.WriteLine("       MaaEnd 桌面分身 - 系统环境与兼容性检测        ");
            Console.WriteLine("======================================================");
            Console.WriteLine();

            int passCount = 0;
            int warnCount = 0;
            int failCount = 0;

            // 1. 操作系统版本与 Edition
            string prodName = "Windows";
            string edition = "Unknown";
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        prodName = Convert.ToString(key.GetValue("ProductName"));
                        edition = Convert.ToString(key.GetValue("EditionID"));
                    }
                }
            }
            catch { }

            Console.Write("[1/6] 操作系统版本: " + prodName + " (" + edition + ") ... ");
            bool isHome = edition.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          prodName.IndexOf("Home", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isHome)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL (家庭版)");
                Console.ResetColor();
                Console.WriteLine("      原因：Windows 家庭版默认缺失远程桌面服务端组件，无法原生开启 Child Session。");
                Console.WriteLine("      解决：运行【修复家庭版RDP.bat】即可一键切换至专业版。");
                failCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK (支持)");
                Console.ResetColor();
                passCount++;
            }

            // 2. 管理员权限检查
            bool isAdmin = false;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            Console.Write("[2/6] 管理员提权状态 ... ");
            if (isAdmin)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK (已提权)");
                Console.ResetColor();
                passCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARN (未提权)");
                Console.ResetColor();
                Console.WriteLine("      提示：启动分身或修改策略需要管理员权限，运行 bat 时会自动触发 UAC 提权。");
                warnCount++;
            }

            // 3. 远程桌面核心服务配置 (TermService)
            Console.Write("[3/6] 远程桌面核心服务 (TermService) ... ");
            int serviceStart = -1;
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\TermService"))
                {
                    if (key != null)
                    {
                        serviceStart = Convert.ToInt32(key.GetValue("Start", -1));
                    }
                }
            }
            catch { }
            if (serviceStart == 4)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL (服务已被禁用)");
                Console.ResetColor();
                Console.WriteLine("      解决：请在 services.msc 中将 Remote Desktop Services 启动类型改为 手动 或 自动。");
                failCount++;
            }
            else if (serviceStart == 2 || serviceStart == 3)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK (配置正常)");
                Console.ResetColor();
                passCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARN (状态未知: " + serviceStart + ")");
                Console.ResetColor();
                warnCount++;
            }

            // 4. 系统远程桌面连接策略 (fDenyTSConnections)
            Console.Write("[4/6] 本地 RDP 监听策略 (fDenyTSConnections) ... ");
            int denyTS = -1;
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server"))
                {
                    if (key != null)
                    {
                        denyTS = Convert.ToInt32(key.GetValue("fDenyTSConnections", 1));
                    }
                }
            }
            catch { }
            if (denyTS == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK (已开启)");
                Console.ResetColor();
                passCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARN (未开启: " + denyTS + ")");
                Console.ResetColor();
                Console.WriteLine("      解决：运行【一键免密.bat】即可自动开启该策略。");
                warnCount++;
            }

            // 5. Windows 原生 Child Session 状态探测 (只读)
            Console.Write("[5/6] 原生 Child Session 状态 (WTSIsChildSessionsEnabled) ... ");
            bool childApiOk = false;
            bool isEnabled = false;
            int lastErr = 0;
            try
            {
                childApiOk = NativeMethods.WTSIsChildSessionsEnabled(out isEnabled);
                if (!childApiOk) lastErr = Marshal.GetLastWin32Error();
            }
            catch (Exception ex)
            {
                lastErr = Marshal.GetHRForException(ex);
            }
            if (childApiOk)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK (" + (isEnabled ? "已启用" : "支持但未启用") + ")");
                Console.ResetColor();
                passCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAIL (Win32错误码: " + lastErr + ")");
                Console.ResetColor();
                if (lastErr == 5)
                {
                    Console.WriteLine("      原因：拒绝访问 (Access Denied)，请以管理员身份运行。");
                }
                else if (lastErr == 120)
                {
                    Console.WriteLine("      原因：系统未实现该 API (ERROR_CALL_NOT_IMPLEMENTED)，可能是精简版 Windows。");
                }
                failCount++;
            }

            // 6. 本地免密凭据检查 (session_cred.dat)
            Console.Write("[6/6] 本地免密凭据 (session_cred.dat) ... ");
            string pwd = CredentialStore.LoadPassword();
            if (!string.IsNullOrEmpty(pwd))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK (已配置，DPAPI 解密通过)");
                Console.ResetColor();
                passCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("INFO (未配置)");
                Console.ResetColor();
                Console.WriteLine("      说明：尚未保存凭据。若当前账户有登录密码，首次建议运行【一键免密.bat】。");
            }

            Console.WriteLine();
            Console.WriteLine("======================================================");
            if (failCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("【检测结果】环境完全满足要求！可以正常使用桌面分身。");
                Console.ResetColor();
                Console.WriteLine("使用方式：直接将 MaaEnd.exe 或快捷方式拖到【启动桌面分身.bat】上。");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("【检测结果】发现 " + failCount + " 项阻碍条件，请参照上方提示修复后再试。");
                Console.ResetColor();
            }
            Console.WriteLine("======================================================");
            Console.WriteLine();

            if (!attached)
            {
                Console.WriteLine("按任意键退出...");
                try { Console.ReadKey(); } catch { }
            }
        }
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)12288;
                AppContext.SetSwitch("Switch.System.Net.DontEnableSystemDefaultTlsVersions", false);
                AppContext.SetSwitch("Switch.System.Net.DontEnableSchUseStrongCrypto", false);
            }
            catch { }
            uint backendParentPid = 0;
            if (args.Length > 0 && (args[0].Equals("--ensure-backend", StringComparison.OrdinalIgnoreCase) || args[0].Equals("--launch-backend", StringComparison.OrdinalIgnoreCase)))
            {
                if (args.Length != 2 || !uint.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out backendParentPid) || backendParentPid == 0)
                {
                    NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                    Console.Error.WriteLine("Usage: ChildSessionHost.exe --ensure-backend <parentPid> (positive decimal PID only)");
                    Environment.ExitCode = 1;
                    return;
                }
            }
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    StringBuilder sbArgs = new StringBuilder();
                    if (args != null && args.Length > 0)
                    {
                        foreach (string a in args)
                        {
                            if (sbArgs.Length > 0) sbArgs.Append(" ");
                            if (a.Contains(" ") || a.Contains("\""))
                                sbArgs.Append("\"").Append(a.Replace("\"", "\\\"")).Append("\"");
                            else
                                sbArgs.Append(a);
                        }
                    }

                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(Application.ExecutablePath)
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        Arguments = sbArgs.ToString()
                    };
                    try
                    {
                        var proc = System.Diagnostics.Process.Start(psi);
                        if (proc != null && args != null && args.Length > 0)
                        {
                            proc.WaitForExit();
                            Environment.Exit(proc.ExitCode);
                            return;
                        }
                    }
                    catch { }
                    return;
                }
            }
            if (args != null && args.Length > 0)
            {
                string cmd = args[0].ToLowerInvariant();
                if (cmd == "--check" || cmd == "-c")
                {
                    RunEnvironmentDiagnostics();
                    Environment.Exit(0);
                    return;
                }
                if (cmd == "--fix-rdp" || cmd == "-f")
                {
                    NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                    Console.WriteLine("======================================================");
                    Console.WriteLine("           正在一键配置并修复本地 RDP 环境");
                    Console.WriteLine("======================================================");
                    try
                    {
                        Console.Write("[1/3] 配置远程桌面核心服务 (TermService) ... ");
                        try
                        {
                            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\TermService", true))
                            {
                                if (key != null)
                                {
                                    int cur = Convert.ToInt32(key.GetValue("Start", 3));
                                    if (cur == 4) key.SetValue("Start", 3, Microsoft.Win32.RegistryValueKind.DWord);
                                }
                            }
                            var psi = new System.Diagnostics.ProcessStartInfo("net", "start TermService")
                            {
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };
                            using (var proc = System.Diagnostics.Process.Start(psi)) { if (proc != null) proc.WaitForExit(3000); }
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("OK");
                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("WARN (" + ex.Message + ")");
                            Console.ResetColor();
                        }

                        Console.Write("[2/3] 开启本地 RDP 监听策略 (fDenyTSConnections) ... ");
                        using (var rdpKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server"))
                        {
                            if (rdpKey != null) rdpKey.SetValue("fDenyTSConnections", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                        using (var tsKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"))
                        {
                            if (tsKey != null) tsKey.SetValue("fPromptForPassword", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("OK");
                        Console.ResetColor();

                        Console.Write("[3/3] 激活内核 Child Session API ... ");
                        bool apiOk = NativeMethods.WTSEnableChildSessions(true);
                        if (apiOk)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("OK");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("WARN (错误码: " + Marshal.GetLastWin32Error() + ")");
                            Console.ResetColor();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("配置异常: " + ex.Message);
                        Console.ResetColor();
                    }

                    Console.WriteLine();
                    Console.WriteLine("修复配置执行完成，正在运行环境检测...");
                    Console.WriteLine();
                    RunEnvironmentDiagnostics();
                    Environment.Exit(0);
                    return;
                }
                if (cmd == "--setup-cred" || cmd == "-s")
                {
                    NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                    string pwd = null;
                    if (Console.IsInputRedirected)
                    {
                        pwd = Console.ReadLine();
                    }
                    if (string.IsNullOrEmpty(pwd))
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        using (var dlg = new PasswordDialog())
                        {
                            if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dlg.EnteredPassword))
                            {
                                pwd = dlg.EnteredPassword;
                            }
                            else
                            {
                                Console.WriteLine("[ChildSessionHost] Cancelled by user.");
                                Environment.Exit(1);
                                return;
                            }
                        }
                    }
                    try
                    {
                        CredentialStore.SavePassword(pwd);
                        Console.WriteLine("[ChildSessionHost] SUCCESS: 测试凭据保存完成！");
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[ChildSessionHost] ERROR: " + ex.Message);
                        Environment.Exit(1);
                    }
                    return;
                }
                else if (cmd == "--status")
                {
                    NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                    NativeMethods.WTSEnableChildSessions(true);
                    uint? sid = NativeMethods.TryGetChildSessionId();
                    if (sid.HasValue)
                    {
                        Console.WriteLine("RUNNING:" + sid.Value);
                    }
                    else
                    {
                        Console.WriteLine("STOPPED");
                    }
                    Environment.Exit(0);
                    return;
                }
                else if (cmd == "--show")
                {
                    NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                    try
                    {
                        EventWaitHandle showEvent;
                        if (EventWaitHandle.TryOpenExisting("MaaEndChildSessionHost_ShowEvent", out showEvent))
                        {
                            showEvent.Set();
                            showEvent.Close();
                            Console.WriteLine("[ChildSessionHost] Show event signaled.");
                            Environment.Exit(0);
                            return;
                        }
                    }
                    catch { }
                }
                else if (cmd == "--logoff")
                {
                    NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                    NativeMethods.WTSEnableChildSessions(true);
                    uint? activeChildId = NativeMethods.TryGetChildSessionId();
                    uint currentHostSessionId = NativeMethods.GetCurrentSessionId();
                    if (activeChildId.HasValue && activeChildId.Value != currentHostSessionId && activeChildId.Value != NativeMethods.NoChildSessionId)
                    {
                        bool ok = NativeMethods.WTSLogoffSession(IntPtr.Zero, activeChildId.Value, false);
                        Console.WriteLine("[ChildSessionHost] Logoff session " + activeChildId.Value + ": " + ok);
                    }
                    else
                    {
                        Console.WriteLine("[ChildSessionHost] No child session to logoff.");
                    }
                    Environment.Exit(0);
                    return;
                }
                else if (cmd == "--ensure-backend" || cmd == "--launch-backend")
                {
                    NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                    bool createdBackendHost;
                    using (Mutex backendHostMutex = new Mutex(true, "MaaEndChildSessionHost_SingleInstanceMutex", out createdBackendHost))
                    {
                        if (!NativeMethods.WTSEnableChildSessions(true)) throw new Win32Exception(Marshal.GetLastWin32Error());
                        if (createdBackendHost)
                        {
                            Application.EnableVisualStyles();
                            Application.SetCompatibleTextRenderingDefault(false);
                            Application.Run(new MainForm(true, backendParentPid: backendParentPid));
                        }
                        else
                        {
                            uint? activeChildId = NativeMethods.TryGetChildSessionId();
                            if (!activeChildId.HasValue)
                            {
                                Console.Error.WriteLine("[ChildSessionHost] Host is already running; child session is not ready.");
                                Environment.ExitCode = 1;
                                return;
                            }
                            ChildLauncher.LaunchBackend(backendParentPid);
                            Console.WriteLine("[ChildSessionHost] Backend launch requested in child session " + activeChildId.Value);
                        }
                    }
                    return;
                }
                else if (cmd == "--launch-maaend")
                {
                    NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                    NativeMethods.WTSEnableChildSessions(true);
                    uint? activeChildId = NativeMethods.TryGetChildSessionId();
                    if (!activeChildId.HasValue)
                    {
                        Console.Error.WriteLine("[ChildSessionHost] Child session is not running.");
                        Environment.Exit(1);
                        return;
                    }
                    string customPath = args.Length > 1 ? args[1] : null;
                    string exe = MainForm.ResolveMaaEndPath(customPath);
                    if (!File.Exists(exe))
                    {
                        Console.Error.WriteLine("[ChildSessionHost] MaaEnd executable not found: " + exe);
                        Environment.Exit(1);
                        return;
                    }
                    ChildLauncher.Launch(activeChildId.Value, exe, "", Path.GetDirectoryName(exe));
                    Console.WriteLine("[ChildSessionHost] Launched MaaEnd into session " + activeChildId.Value + ": " + exe);
                    Environment.Exit(0);
                    return;
                }
                else if (cmd == "--launch-game")
                {
                    NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
                    NativeMethods.WTSEnableChildSessions(true);
                    uint? activeChildId = NativeMethods.TryGetChildSessionId();
                    if (!activeChildId.HasValue)
                    {
                        Console.Error.WriteLine("[ChildSessionHost] Child session is not running.");
                        Environment.Exit(1);
                        return;
                    }
                    string target = MainForm.ResolveGamePath();
                    if (target == null)
                    {
                        Console.Error.WriteLine("[ChildSessionHost] Game launcher executable not found.");
                        Environment.Exit(1);
                        return;
                    }
                    ChildLauncher.Launch(activeChildId.Value, target, "", Path.GetDirectoryName(target));
                    Console.WriteLine("[ChildSessionHost] Launched game into session " + activeChildId.Value + ": " + target);
                    Environment.Exit(0);
                    return;
                }
            }
            string customMaaEnd = null;
            bool autoLaunchMaaEnd = false;

            if (args != null && args.Length > 0)
            {
                string cmd = args[0].ToLowerInvariant();
                string first = args[0].Trim('"');

                if (cmd == "--start")
                {
                    autoLaunchMaaEnd = true;
                    if (args.Length > 1) customMaaEnd = args[1].Trim('"');
                }
                else if (cmd == "--no-auto-launch")
                {
                    autoLaunchMaaEnd = false;
                }
                else if (first.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || first.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || File.Exists(first) || Directory.Exists(first))
                {
                    customMaaEnd = first;
                    autoLaunchMaaEnd = true;
                }
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, "MaaEndChildSessionHost_SingleInstanceMutex", out createdNew))
            {
                if (!createdNew)
                {
                    NativeMethods.WTSEnableChildSessions(true);
                    uint? activeChildId = NativeMethods.TryGetChildSessionId();
                    if (activeChildId.HasValue && autoLaunchMaaEnd)
                    {
                        string exe = MainForm.ResolveMaaEndPath(customMaaEnd);
                        if (File.Exists(exe))
                        {
                            try { ChildLauncher.Launch(activeChildId.Value, exe, "", Path.GetDirectoryName(exe)); } catch { }
                        }
                    }

                    try
                    {
                        EventWaitHandle showEvent;
                        if (EventWaitHandle.TryOpenExisting("MaaEndChildSessionHost_ShowEvent", out showEvent))
                        {
                            showEvent.Set();
                            showEvent.Close();
                        }
                    }
                    catch { }
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(false, autoLaunchMaaEnd, null, customMaaEnd));
            }
        }
    }
}
