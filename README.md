# RenameFilesAndFolders

Simple dotnet core app to quickly manipulate file names and directories recursively. Created out of necessity.

## Usage
```
Usage: RenameFilesAndFolders.exe [flags] [options] {old string} {new string}

Flags;
  -help : Print the application usage and exit.
  -noFiles : Flag ensuring files will not be renamed.
  -noDirectories : Flag ensuring directories will not be renamed.
  -noRecursive : Flag ensuring only the top directory will be iterated. No Sub Directories.
Options;
  -d {value} : Optional root directory of the file system traversal. Default is the directory of the executable.
  -r {value} : Optional regex filter to be applied to both files and directories. Exclusive to -rf and -rd.
  -rf {value} : Optional regex filter for file names to be renamed.
  -rd {value} : Optional regex filter for directories to be traversed and renamed.
```
