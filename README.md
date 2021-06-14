FileSystemWatcherEx
==================

A simple enhanced class based on .net official [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher).

This class attempts to do something better

- Try fire same event only one time,  [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher) may fire multiple times.
- check whether file is ready for read write.  [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher) fire event immediately.
- enhanced filter,  support monitoring multiple extensions. As far as I know,  [FileSystemWatcher](https://docs.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher) does not support.



## Limitation

**DO NOT** keep the file open in your callback, open it and close it as soon as possible. then you should be safe.

I demonstrate a bad callback implementation in demo project. run it with `-p` option to see it.



## Example usage

```c#
public void OnFileChanged(object sender, System.IO.FileSystemEventArgs e)
{
  if (e.ChangeType == System.IO.WatcherChangeTypes.Created 
     || e.ChangeType == System.IO.WatcherChangeTypes.Changed)
  {
    // do something.
  }
}

// monitor .bmp and .jpg and .png files and .docx files begin with a
var filters = "*.bmp|*.jpg|*.png|a*.docx";

var monitorPath = System.AppDomain.CurrentDomain.BaseDirectory;
var fileWatcherEx = new FileSystemWatcherEx(monitorPath
    , filters
    , true
    , ""
    , OnFileChanged
    , OnFileChanged
    , OnFileChanged
);

fileWatcherEx.Start();

// when finish monitoring

fileWatcherEx.Stop();
```



## Demo Project

this demo project will monitor program directory recursively, and display changed .bmp file.

 ![result](E:\working\TestMonitorImage\result.gif)

 

