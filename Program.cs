using System;
using System.Collections.Generic;
using System.IO;

namespace RenameFilesAndFolders
{
    /// <summary>
    /// This console app iterates the content of a directory renaming directorys files, and the content of files.
    /// </summary>
    class Program
    {
        static Dictionary<string, string> _parameters;
        static string _supersededString;
        static string _newString;
        static string _directoryRoot;
        static string _fileRegex;
        static string _directoryRegex;
        static bool _renameDirectories;
        static bool _renameFiles;
        static bool _recursive;
        
        static Tuple<string, string>[] _validFlags = new [] {
            Tuple.Create("-noFiles", "Flag ensuring files will not be renamed."),
            Tuple.Create("-noDirectories", "Flag ensuring directories will not be renamed."),
            Tuple.Create("-noRecursive", "Flag ensuring only the top directory will be iterated. No Sub Directories.")
        };
        static Tuple<string, string>[] _validOptions = new [] {
            Tuple.Create("-d", "Optionally specify the root directory of the rename operation. Default is the directory of the executable."),
            Tuple.Create("-rf", "Optional regex filter for file names to be renamed."),
            Tuple.Create("-rd", "Optional regex filter for directories to be renamed.")
        };
        
        /// <summary>
        /// 
        /// </summary>
        static int Main(string[] args)
        {
            
            // parse the parameters
            try { _parameters = ParseParameters(args); }
            catch(Exception ex) { System.Console.Write(ex.Message); PrintUsage(); return -1; }
            
            string val;
            _renameDirectories = (_parameters.TryGetValue("-noDirectories", out val)) ? false : true;
            _renameFiles = (_parameters.TryGetValue("-noFiles", out val)) ? false : true;
            _recursive = (_parameters.TryGetValue("-noRecursive", out val)) ? false : true;
            
            _supersededString = _parameters.TryGetValue("supersededString", out val) ? val : null;
            _newString = _parameters.TryGetValue("newString", out val) ? val : null;
            if(string.IsNullOrEmpty(_supersededString)) {
                Console.WriteLine("Missing 'superseded string'.");
                PrintUsage();
                return -1;
            }
            if(_newString == null) {
                Console.WriteLine("Missing 'replacement' string. To replace with nothing, write \"\".");
                PrintUsage();
                return -1;
            }
            
            if (_parameters.TryGetValue("-d", out val)) {
                _directoryRoot = val;
            } else {
                _directoryRoot = System.Reflection.Assembly.GetEntryAssembly().Location;
                _directoryRoot = System.IO.Path.GetDirectoryName(_directoryRoot);
            }
            if(string.IsNullOrEmpty(_directoryRoot) || !System.IO.Directory.Exists(_directoryRoot)) {
                Console.Write("Root directory option is not valid '"+_directoryRoot+"'.");
                PrintUsage();
                return -1;
            }
            _fileRegex = _parameters.TryGetValue("-fr", out val) ? val : "*";
            _directoryRegex = _parameters.TryGetValue("-dr", out val) ? val : "*";
            
            List<string> errors = new List<string>();
            
            try {
                
                // iterate the directories
                DirectoryInfo directoryInfo = new DirectoryInfo(_directoryRoot);
                TraverseFileSystem(directoryInfo, errors);
                
            } catch(Exception ex) {
                Console.WriteLine(ex);
                return -1;
            }
            
            if(errors.Count > 0) {
                foreach(var error in errors) {
                    Console.WriteLine("Error during rename; " + error);
                }
                Console.WriteLine("Operation complete with errors.");
            } else {
                Console.WriteLine("Operation complete.");
            }
            
            return 0;
        }
        
        private static void PrintUsage() {
            
            Console.WriteLine(@"
  ============== RenameFilesAndFolders =============
  
  Usage: RenameFilesAndFolders.exe [flags] [options] {superseded string} {new string}
  
  Flags;");
            foreach(var flag in _validFlags) {
                Console.WriteLine("    " + flag.Item1 + " : " + flag.Item2);
            }
            Console.WriteLine("  Options;");
            foreach(var option in _validOptions) {
                Console.WriteLine("    " + option.Item1 + " {value} : " + option.Item2);
            }
            Console.WriteLine(@"
  ==================================================");
        }
        
        
        
        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<string, string> ParseParameters(string[] args) {
            
            Dictionary<string, string> _parameters = new Dictionary<string, string>();
            
            if(args.Length > 2) {
                _parameters.Add("supersededString", args[args.Length-2]);
                _parameters.Add("newString", args[args.Length-1]);
            }
            
            List<string> unrecognizedFlags = new List<string>();
            List<string> unrecognizedOption = new List<string>();
            
            for(int i = 0; i < args.Length-2; i += 2) {
                
                if(string.IsNullOrEmpty(args[i])) continue;
                
                bool isValid = false;
                foreach(var validFlag in _validFlags) {
                    if(validFlag.Item1.Equals(args[i])) {
                        isValid = true;
                        break;
                    }
                }
                if(isValid) {
                    _parameters.Add(args[i], null);
                    continue;
                }
                foreach(var validOption in _validOptions) {
                    if(validOption.Item1.Equals(args[i])) {
                        isValid = true;
                        break;
                    }
                }
                if(isValid && i < args.Length-1) {
                    _parameters.Add(args[i], args[i+1]);
                    continue;
                }
                unrecognizedFlags.Add(args[i]);
            }
            
            // throw if unrecognized
            if(unrecognizedFlags.Count > 0) {
                string error = "";
                bool first = true;
                foreach(var flag in unrecognizedFlags) {
                    if(first) first = false;
                    else error += ", ";
                    error += flag;
                }
                throw new Exception("There were unrecognized parameters; " + error + '.');
            }
            
            // iterate the parameter values and remove quotations
            Dictionary<string, string> newParameters = new Dictionary<string, string>();
            foreach(var parameter in _parameters) {
                if(parameter.Value != null &&
                    parameter.Value.Length > 1 &&
                    parameter.Value.StartsWith("\"") &&
                    parameter.Value.EndsWith("\"")) {
                        newParameters.Add(parameter.Key, parameter.Value.Substring(1, parameter.Value.Length-2));
                    }
            }
            
            foreach(var newParameter in newParameters) {
                _parameters[newParameter.Key] = newParameter.Value;
            }
            
            return _parameters;
        }
    
        /// <summary>
        /// Iterate the directories.
        /// </summary>
        static void TraverseFileSystem(System.IO.DirectoryInfo currentDirectory, List<string> errors)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try {
                if(_recursive) subDirs = currentDirectory.GetDirectories(_directoryRegex, SearchOption.TopDirectoryOnly);
                else subDirs = new System.IO.DirectoryInfo[0];
            } catch (UnauthorizedAccessException ex) {
                errors.Add(ex.Message);
            } catch (System.IO.DirectoryNotFoundException ex) {
                errors.Add(ex.Message);
                return;
            } catch(Exception ex) {
                errors.Add(ex.Message);
                return;
            }
            
            if (subDirs != null) {
                
                // iterate the subdirectories
                foreach (System.IO.DirectoryInfo dirInfo in subDirs) {
                    // enter the sub-directory
                    TraverseFileSystem(dirInfo, errors);
                }
                
                if(_renameFiles) {
                    try {
                        files = currentDirectory.GetFiles(_fileRegex, SearchOption.TopDirectoryOnly);
                    } catch (UnauthorizedAccessException ex) {
                        // This code just writes out the message and continues to recurse.
                        // You may decide to do something different here. For example, you
                        // can try to elevate your privileges and access the file again.
                        errors.Add(ex.Message);
                    }
                    catch (System.IO.DirectoryNotFoundException ex) {
                        errors.Add(ex.Message);
                        return;
                    } catch(Exception ex) {
                        errors.Add(ex.Message);
                        return;
                    }
                    
                    if(files != null) {
                        
                        // iterate files
                        foreach (System.IO.FileInfo fi in files) {
                            
                            // get the new file name
                            string newFileName = currentDirectory.FullName +
                                fi.FullName.Substring(currentDirectory.FullName.Length, fi.FullName.Length - currentDirectory.FullName.Length)
                                           .Replace(_supersededString, _newString);
                            
                            if(!fi.FullName.Equals(newFileName)) {
                                Console.WriteLine("Renaming '"+fi.FullName+"\nTo       '"+newFileName+"'");
                                
                                try {
                                    // rename the file
                                    File.Move(fi.FullName, newFileName);
                                } catch(Exception ex) {
                                    errors.Add("Failed to rename file '"+fi.FullName+"' to '"+newFileName+"'. " + ex.Message);
                                }
                            }
                            
                        }
                        
                    }
                }
                
                if(_renameDirectories) {
                    // replace the directory path
                    int index = currentDirectory.FullName.LastIndexOf(Path.DirectorySeparatorChar);
                    if(index == -1) {
                        errors.Add("Something bad happened.");
                        return;
                    }
                    
                    string newDirectoryPath = currentDirectory.FullName.Substring(0, index) +
                            currentDirectory.FullName.Substring(index, currentDirectory.FullName.Length - index)
                            .Replace(_supersededString, _newString);
                    
                    if(!newDirectoryPath.Equals(currentDirectory.FullName)) {
                        Console.WriteLine("Renaming '"+currentDirectory.FullName+"\nTo       '"+newDirectoryPath+"'");
                        
                        try {
                            // rename this directory
                            Directory.Move(currentDirectory.FullName, newDirectoryPath);
                        } catch(Exception ex) {
                            errors.Add("Failed to rename directory '"+currentDirectory.FullName+"' to '"+newDirectoryPath+"'. " + ex.Message);
                        }
                    }
                    
                }
                
            }            
        }
    }
}
