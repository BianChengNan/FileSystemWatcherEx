using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// a simple enhanced file watcher class based on official FileSystemWatcher.
/// </summary>
public class FileSystemWatcherEx
{
    #region options，ref https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-5.0
    /// <summary>
    /// folder to watch, must exist.
    /// </summary>
    public string Path
    {
        get { return _path; }
        set { _path = value; }
    }
    private string _path = "";

    /// <summary>
    /// FilterList contains all filters
    /// *.*    : watch for all files.
    /// *.docx : only watch .docx files.
    /// a.*    : watch file a, all extensions match.
    /// </summary>
    public string Filters
    {
        get { return _filters; }
        set { _filters = value; }
    }
    private string _filters;

    /// <summary>
    /// recursive watch.
    /// </summary>
    public bool Recursive
    {
        get { return _recursive; }
        set { _recursive = value; }
    }
    private bool _recursive = true;

    public NotifyFilters NotifyFilters
    {
        get { return _notifyFilters; }
        set { _notifyFilters = value; }
    }

    private NotifyFilters _notifyFilters = NotifyFilters.DirectoryName | NotifyFilters.FileName
        | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.LastAccess
        | NotifyFilters.Attributes | NotifyFilters.Size | NotifyFilters.Security
        ;

    #endregion

    #region optionex (to avoid same event trigger multiple times and wait for file ready to access)

    /// <summary>
    /// sometimes, same event may be triggered multiple times. 
    /// set this flag true to try merge same event.
    /// </summary>
    public bool TryMergeSameEvent
    {
        get { return _tryMergeSameEvent; }
        set { _tryMergeSameEvent = value; }
    }
    private bool _tryMergeSameEvent = true;

    /// <summary>
    /// max wait time to handle same event
    /// </summary>
    public int DelayTriggerMs
    {
        get { return _delayTriggerMs; }
        set { _delayTriggerMs = value; }
    }
    private int _delayTriggerMs = 10;

    /// <summary>
    /// when copying a huge file to monitor folder, we receive file create event at the beginning,
    /// but this file can not be access immediately.
    /// </summary>
    public bool WaitForFileReadyToAccess
    {
        get { return _waitForFileReadyToAccess; }
        set { _waitForFileReadyToAccess = value; }
    }
    private bool _waitForFileReadyToAccess = true;

    /// <summary>
    /// max wait time for a file until it can be accessed.
    /// </summary>
    public int MaxWaitMs
    {
        get { return _maxWaitMs; }
        set { _maxWaitMs = value; }
    }
    private int _maxWaitMs = int.MaxValue;

    /// <summary>
    /// when file is not ready to access immediately, how frequent to check again.
    /// </summary>
    public int FileAccessCheckIntervalMs
    {
        get { return _fileAccessCheckIntervalMs; }
        set { _fileAccessCheckIntervalMs = value; }
    }
    private int _fileAccessCheckIntervalMs = 100;

    #endregion

    #region event callback，ref https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-5.0

    public event FileSystemEventHandler OnCreated;

    public event FileSystemEventHandler OnDeleted;

    public event FileSystemEventHandler OnChanged;

    public event RenamedEventHandler OnRenamed;

    public event ErrorEventHandler OnError;

    internal void OnFileSystemEventHandler(object sender, FileSystemEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(string.Format("[inner] path: {0}, event type: {1}", e.FullPath, e.ChangeType.ToString()));
        AddEventData(new FileSystemEventNotifyData { sender = sender, eventArgs = e });
        eventFiredEvent.Set();
    }

    internal void OnRenamedEventHandler(object sender, RenamedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(string.Format("[inner] old: {0}, new: {1}, event type: {2}", e.OldFullPath, e.FullPath, e.ChangeType.ToString()));
        AddEventData(new FileSystemEventNotifyData { sender = sender, eventArgs = e });
        eventFiredEvent.Set();
    }

    internal void OnErrorHandler(object sender, ErrorEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(string.Format("[inner] exception: {0}", e.GetException().ToString()));
        AddEventData(new FileSystemEventNotifyData { sender = sender, eventArgs = e });
        eventFiredEvent.Set();
    }
    #endregion

    public string UniqueId
    {
        get { return _uniqueId; }
        protected set { _uniqueId = value; }
    }
    private string _uniqueId = System.Guid.NewGuid().ToString().ToUpper();

    public List<FileSystemWatcher> WatcherList
    {
        get { return _watcherList; }
        set { _watcherList = value; }
    }
    private List<FileSystemWatcher> _watcherList = new List<FileSystemWatcher>();

    #region constructors

    /// <param name="path"> path to watch, must be existed path</param>
    /// <param name="filters"> watch filters. 
    /// *.* will monitor all files.
    /// *.bmp will monitor all .bmp files.
    /// if want to monitor multiple files at the same time, separate by |.
    /// e.g. *.txt| *.bmp | *.jpg will monitor .txt & .bmp & .jpg files.
    /// </param>
    /// <param name="groupId"></param>
    public FileSystemWatcherEx(string path
        , string filters = "*.*"
        , bool bRecursive = true
        , string uniqueId = ""
        , FileSystemEventHandler OnCreatedHandler = null
        , FileSystemEventHandler OnDeletedHandler = null
        , FileSystemEventHandler OnChangedHandler = null
        , RenamedEventHandler OnRenamedHandler = null
        , ErrorEventHandler OnErrorHandler = null
        )
    {
        if (!string.IsNullOrEmpty(uniqueId))
        {
            UniqueId = uniqueId;
        }

        if (!string.IsNullOrEmpty(path))
        {
            Path = path;
        }

        Filters = filters;

        if (OnCreatedHandler != null)
        {
            OnCreated += OnCreatedHandler;
        }

        if (OnDeletedHandler != null)
        {
            OnDeleted += OnDeletedHandler;
        }

        if (OnChangedHandler != null)
        {
            OnChanged += OnChangedHandler;
        }

        if (OnRenamedHandler != null)
        {
            OnRenamed += OnRenamedHandler;
        }

        if (OnErrorHandler != null)
        {
            OnError += OnErrorHandler;
        }
    }

    #endregion

    public bool Start()
    {
        if (!Directory.Exists(this.Path))
        {
            return false;
        }

        char[] splitter = { '|' };
        var filterList = Filters.Split(splitter).ToList();
        foreach (var filter in filterList)
        {
            FileSystemWatcher watcher = new FileSystemWatcher();

            watcher.Filter = filter;
            watcher.Path = this.Path;
            watcher.IncludeSubdirectories = this.Recursive;
            watcher.NotifyFilter = this.NotifyFilters;

            watcher.Created += this.OnFileSystemEventHandler;
            watcher.Deleted += this.OnFileSystemEventHandler;
            watcher.Changed += this.OnFileSystemEventHandler;
            watcher.Renamed += this.OnRenamedEventHandler;
            watcher.Error += this.OnErrorHandler;

            watcher.EnableRaisingEvents = true;

            WatcherList.Add(watcher);
        }

        return StartEventFireNotifyThread();
    }

    public bool Stop()
    {
        Enable(false);
        StopEventFireNotifyThread();
        return true;
    }

    public bool Enable(bool bEnable)
    {
        foreach (var watcher in WatcherList)
        {
            watcher.EnableRaisingEvents = false;
        }

        return true;
    }

    #region notify thread

    protected bool StartEventFireNotifyThread()
    {
        if (m_notifyThread == null || !m_notifyThread.IsAlive)
        {
            m_notifyThread = new System.Threading.Thread(HandleEventFireNotifyWorkProc);
            m_notifyThread.Name = string.Format(string.Format("[WatchEx] : {0}-{1}", Filters, Path));
            m_notifyThread.IsBackground = true;
            m_notifyThread.Start();
        }

        return true;
    }

    protected bool StopEventFireNotifyThread()
    {
        bQuit = true;
        eventFiredEvent.Set();
        return true;
    }

    protected void HandleEventFireNotifyWorkProc()
    {
        var cameraEvents = new WaitHandle[]
            {
                eventFiredEvent
            };

        while (!bQuit)
        {
            var waitResult = WaitHandle.WaitAny(cameraEvents, System.Threading.Timeout.Infinite);

            if (TryMergeSameEvent)
            {
                System.Threading.Thread.Sleep(DelayTriggerMs);
            }

            List<FileSystemEventNotifyData> eventDataList;
            lock (eventListLocker)
            {
                eventDataList = m_eventDataList;
                m_eventDataList = new List<FileSystemEventNotifyData>();
            }

            NotifyEvents(eventDataList);
        }
    }

    private void NotifyEvents(List<FileSystemEventNotifyData> eventDataList)
    {
        if (eventDataList == null || eventDataList.Count == 0)
        {
            return;
        }

        eventDataList = eventDataList.Distinct().ToList();
        foreach (var eventData in eventDataList)
        {
            if (bQuit)
            {
                break;
            }

            if (NotifyFileSystemEvent(eventData))
            {
                continue;
            }

            if (NotifyRenamedEvent(eventData))
            {
                continue;
            }

            if (NotifyError(eventData))
            {
                continue;
            }
        }
    }

    private bool NotifyFileSystemEvent(FileSystemEventNotifyData data)
    {
        FileSystemEventArgs e = data.eventArgs as FileSystemEventArgs;
        if (e == null)
        {
            return false;
        }

        if (WaitForFileReadyToAccess)
        {
            WaitUntilCanAccess(data, MaxWaitMs);
        }

        switch (e.ChangeType)
        {
            case WatcherChangeTypes.Created:
                {
                    if (OnCreated != null)
                    {
                        OnCreated(data.sender, e);
                    }
                }
                break;
            case WatcherChangeTypes.Deleted:
                {
                    if (OnDeleted != null)
                    {
                        OnDeleted(data.sender, e);
                    }
                }
                break;
            case WatcherChangeTypes.Changed:
                {
                    if (OnChanged != null)
                    {
                        OnChanged(data.sender, e);
                    }
                }
                break;
            default:
                break;
        }

        return true;
    }

    private void WaitUntilCanAccess(FileSystemEventNotifyData data, int maxWaitMs)
    {
        FileSystemEventArgs e = data.eventArgs as FileSystemEventArgs;
        if (e == null)
        {
            return;
        }

        bool bNeedWait = (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Changed);
        if (!bNeedWait)
        {
            return;
        }

        var logStartTime = System.DateTime.Now;
        var startTime = System.DateTime.Now;
        do
        {
            FileStream stream = null;
            try
            {
                stream = File.Open(e.FullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete | FileShare.Inheritable);
                break;
            }
            catch (IOException ex)
            {
                if (ex.HResult != -2147024864) // 0x80070020: The process cannot access the file, because it is being used by another process
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("!!!![inner] try opening file [{0}] exception. HResult {1}! GiveUp!", e.FullPath, ex.HResult));
                    break;
                }

                var now = System.DateTime.Now;
                if ((now - logStartTime).TotalSeconds > 1) // record exception per second.
                {
                    logStartTime = now;
                    System.Diagnostics.Debug.WriteLine(string.Format("[inner] fail to open file [{0}], try again!", e.FullPath));
                }

                if ((now - startTime).TotalMilliseconds > maxWaitMs)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("[inner] timeout for waitting file [{0}] to be available, wait ms: {1}!", e.FullPath, maxWaitMs));
                    break;
                }

                Thread.Sleep(FileAccessCheckIntervalMs);

                if (bQuit)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("[inner] fail to open file [{0}], unexpected exception {1}!", e.FullPath, ex));
                break;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

        } while (true);
    }

    private bool NotifyRenamedEvent(FileSystemEventNotifyData data)
    {
        RenamedEventArgs e = data.eventArgs as RenamedEventArgs;
        if (e == null)
        {
            return false;
        }

        if (OnRenamed != null)
        {
            OnRenamed(data.sender, e);
        }

        return true;
    }

    private bool NotifyError(FileSystemEventNotifyData data)
    {
        ErrorEventArgs e = data.eventArgs as ErrorEventArgs;
        if (e == null)
        {
            return false;
        }

        if (OnError != null)
        {
            OnError(data.sender, e);
        }

        return true;
    }

    public class FileSystemEventNotifyData
    {
        public object sender;
        public EventArgs eventArgs;

        public override int GetHashCode()
        {
            return 0;
        }

        public override bool Equals(object rhs)
        {
            var data_rhs = rhs as FileSystemEventNotifyData;
            if (data_rhs == null)
            {
                return false;
            }

            return Equals(data_rhs);
        }

        private bool IsSameChangeType(WatcherChangeTypes lhs, WatcherChangeTypes rhs)
        {
            return (lhs == rhs)
                || (lhs == WatcherChangeTypes.Created && rhs == WatcherChangeTypes.Changed)
                || (lhs == WatcherChangeTypes.Changed && rhs == WatcherChangeTypes.Created)
                ;
        }

        public bool Equals(FileSystemEventNotifyData rhs)
        {
            var renameEvent_lhs = eventArgs as RenamedEventArgs;
            var renameEvent_rhs = rhs.eventArgs as RenamedEventArgs;
            if ((renameEvent_lhs != null && renameEvent_rhs != null))
            {
                return renameEvent_lhs.FullPath == renameEvent_rhs.FullPath
                    && renameEvent_lhs.Name == renameEvent_rhs.Name
                    && IsSameChangeType(renameEvent_lhs.ChangeType, renameEvent_rhs.ChangeType)
                    && renameEvent_lhs.OldFullPath == renameEvent_rhs.OldFullPath
                    && renameEvent_lhs.OldName == renameEvent_rhs.OldName;
            }

            var fileSystemEvent_lhs = eventArgs as FileSystemEventArgs;
            var fileSystemEvent_rhs = rhs.eventArgs as FileSystemEventArgs;
            if ((fileSystemEvent_lhs != null && fileSystemEvent_rhs != null))
            {
                return fileSystemEvent_lhs.FullPath == fileSystemEvent_rhs.FullPath
                    && fileSystemEvent_lhs.Name == fileSystemEvent_rhs.Name
                    && IsSameChangeType(fileSystemEvent_lhs.ChangeType, fileSystemEvent_lhs.ChangeType);
            }

            var errorEvent_lhs = eventArgs as ErrorEventArgs;
            var errorEvent_rhs = rhs.eventArgs as ErrorEventArgs;
            if ((renameEvent_lhs != null && renameEvent_rhs != null))
            {
                return true;
            }

            return false;
        }
    }

    public void AddEventData(FileSystemEventNotifyData data)
    {
        lock (eventListLocker)
        {
            m_eventDataList.Add(data);
        }
    }

    private object eventListLocker = new object();
    private List<FileSystemEventNotifyData> m_eventDataList = new List<FileSystemEventNotifyData>();
    private Thread m_notifyThread;
    private AutoResetEvent eventFiredEvent = new AutoResetEvent(false);
    private bool bQuit = false;

    #endregion
}
