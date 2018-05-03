# FMListProperties
FMListProperties is a simple windows command-line tool to list all Windows Property System properties on one or more files. For ISO Base Media Files (.MP4, .MOV, .M4A and others) it also reports certain properties that Windows Property System leaves out.

Example:

    FMListProperties MyVideoFile.mp4

Use the `-h` command-line option to print help text including all of the available options.

FMListProperties uses the following [CodeBits](http://www.filemeta.org/CodeBit.html):

* [WinShellPropertyStore](https://github.com/FileMeta/WinShellPropertyStore)
* [IsomCoreMetadata](https://github.com/FileMeta/IsomCoreMetadata)
* [ConsoleHelper](https://github.com/FileMeta/ConsoleHelper)

## Source Code
Written in C# using Visual Studio 2017.

## About CodeBits
A [CodeBit](http://FileMeta.org/CodeBit.html) is a way to share common code that's lighter weight than NuGet. Each CodeBit consists of a single source code file. A structured comment at the beginning of the file indicates where to find the master copy so that automated tools can retrieve and update CodeBits to the latest version.