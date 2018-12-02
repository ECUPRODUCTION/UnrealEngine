﻿// Copyright 1998-2018 Epic Games, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Tools.DotNETCommon;

namespace UnrealBuildTool
{
	/// <summary>
	/// Represents a folder within the master project (e.g. Visual Studio solution)
	/// </summary>
	class QMakefileFolder : MasterProjectFolder
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public QMakefileFolder(ProjectFileGenerator InitOwnerProjectFileGenerator, string InitFolderName)
			: base(InitOwnerProjectFileGenerator, InitFolderName)
		{
		}
	}

	class QMakefileProjectFile : ProjectFile
	{
		public QMakefileProjectFile(FileReference InitFilePath)
			: base(InitFilePath)
		{
		}
	}

	/// <summary>
	/// QMakefile project file generator implementation
	/// </summary>
	class QMakefileGenerator : ProjectFileGenerator
	{
		/// Default constructor
		public QMakefileGenerator(FileReference InOnlyGameProject)
			: base(InOnlyGameProject)
		{
		}

		/// File extension for project files we'll be generating (e.g. ".vcxproj")
		override public string ProjectFileExtension
		{
			get
			{
				return ".pro";
			}
		}

		protected override bool WriteMasterProjectFile(ProjectFile UBTProject)
		{
			bool bSuccess = true;
			return bSuccess;
		}

		/// <summary>
		/// Splits the definition text into macro name and value (if any).
		/// </summary>
		/// <param name="Definition">Definition text</param>
		/// <param name="Key">Out: The definition name</param>
		/// <param name="Value">Out: The definition value or null if it has none</param>
		/// <returns>Pair representing macro name and value.</returns>
		private void SplitDefinitionAndValue(string Definition, out String Key, out String Value)
		{
			int EqualsIndex = Definition.IndexOf('=');
			if (EqualsIndex >= 0)
			{
				Key = Definition.Substring(0, EqualsIndex);
				Value = Definition.Substring(EqualsIndex + 1);
			}
			else
			{
				Key = Definition;
				Value = "";
			}
		}

		/// Adds the include directory to the list, after converting it to relative to UE4 root
		private void AddIncludeDirectory(ref List<string> IncludeDirectories, string IncludeDir, string ProjectDir)
		{
			string FullProjectPath = ProjectFileGenerator.MasterProjectPath.FullName;
			string FullPath = "";
			if (IncludeDir.StartsWith("/") && !IncludeDir.StartsWith(FullProjectPath))
			{
				// Full path to a fulder outside of project
				FullPath = IncludeDir;
			}
			else
			{
				FullPath = Path.GetFullPath(Path.Combine(ProjectDir, IncludeDir));
				FullPath = Utils.MakePathRelativeTo(FullPath, FullProjectPath);
				FullPath = FullPath.TrimEnd('/');
			}

			if (!FullPath.Contains("FortniteGame/") && !FullPath.Contains("ThirdParty/")) // @todo: skipping Fortnite header paths to shorten clang command line for building UE4XcodeHelper
			{
				IncludeDirectories.Add(FullPath);
			}
		}

		// For later when moving all this back to MakefileGenerator.cs
		private void AddDefines(ref List<string> DefinesContent, string define, string value)
		{
		}

		private bool WriteQMakePro()
		{
			// Some more stuff borrowed from Mac side of things.
			List<string> IncludeDirectories = new List<string>();
			List<string> SystemIncludeDirectories = new List<string>();
			List<string> DefinesAndValues = new List<string>();

			// DefineList.Add ("");

			string QMakeIncludesFileName = MasterProjectName + "Includes.pri";
			StringBuilder QMakeIncludesPriFileContent = new StringBuilder();

			string QMakeDefinesFileName = MasterProjectName + "Defines.pri";
			StringBuilder QMakeDefinesPriFileContent = new StringBuilder();

			string GameProjectPath = "";
			string GameProjectFile = "";
			string GameProjectRootPath = "";

			string BuildCommand = "";

			string QMakeGameProjectFile = "";

			foreach (ProjectFile CurProject in GeneratedProjectFiles)
			{

				QMakefileProjectFile QMakeProject = CurProject as QMakefileProjectFile;
				if (QMakeProject == null)
				{
					Log.TraceInformation("QMakeProject == null");
					continue;
				}

				foreach (string CurPath in QMakeProject.IntelliSenseIncludeSearchPaths)
				{
					AddIncludeDirectory(ref IncludeDirectories, CurPath, Path.GetDirectoryName(QMakeProject.ProjectFilePath.FullName));
					// System.Console.WriteLine ("Not empty now? CurPath == ", CurPath);
				}
				foreach (string CurPath in QMakeProject.IntelliSenseSystemIncludeSearchPaths)
				{
					AddIncludeDirectory(ref SystemIncludeDirectories, CurPath, Path.GetDirectoryName(QMakeProject.ProjectFilePath.FullName));
				}

			}

			// Remove duplicate paths from include dir and system include dir list
			IncludeDirectories = IncludeDirectories.Distinct().ToList();
			SystemIncludeDirectories = SystemIncludeDirectories.Distinct().ToList();

			// Iterate through all the defines for the projects that are generated by 
			// UnrealBuildTool.exe
			// !RAKE: move to seperate function
			QMakeDefinesPriFileContent.Append("DEFINES += \\\n");
			foreach (ProjectFile CurProject in GeneratedProjectFiles)
			{
				QMakefileProjectFile QMakeProject = CurProject as QMakefileProjectFile;
				if (QMakeProject == null)
				{
					Log.TraceInformation("QMakeProject == null");
					continue;
				}

				foreach (string CurDefine in QMakeProject.IntelliSensePreprocessorDefinitions)
				{
					String define = "";
					String value = "";

					SplitDefinitionAndValue(CurDefine, out define, out value);

					if (!DefinesAndValues.Contains(define))
					{
						// Log.TraceInformation (CurDefine);
						if (string.IsNullOrEmpty(value))
						{
							DefinesAndValues.Add("\t");
							DefinesAndValues.Add(String.Format("{0}=", define));
							DefinesAndValues.Add(" \\\n");
						}
						else
						{
							DefinesAndValues.Add("\t");
							DefinesAndValues.Add(define);
							DefinesAndValues.Add("=");
							DefinesAndValues.Add(value);
							DefinesAndValues.Add(" \\\n");
						}
					}
				}
			}

			foreach (string Def in DefinesAndValues)
			{
				QMakeDefinesPriFileContent.Append(Def);
			}

			// Iterate through all the include paths that
			// UnrealBuildTool.exe generates
			// !RAKE: Move to seperate function
			QMakeIncludesPriFileContent.Append("INCLUDEPATH += \\\n");
			foreach (string CurPath in IncludeDirectories)
			{
				QMakeIncludesPriFileContent.Append("\t");
				QMakeIncludesPriFileContent.Append(CurPath);
				QMakeIncludesPriFileContent.Append(" \\\n");
			}

			foreach (string CurPath in SystemIncludeDirectories)
			{
				QMakeIncludesPriFileContent.Append("\t");
				QMakeIncludesPriFileContent.Append(CurPath);
				QMakeIncludesPriFileContent.Append(" \\\n");
			}
			QMakeIncludesPriFileContent.Append("\n");

			if (!String.IsNullOrEmpty(GameProjectName))
			{
				GameProjectPath = OnlyGameProject.Directory.FullName;
				GameProjectFile = OnlyGameProject.FullName;
				QMakeGameProjectFile = "gameProjectFile=" + GameProjectFile + "\n";
				BuildCommand = "build=mono $$unrealRootPath/Engine/Binaries/DotNET/UnrealBuildTool.exe\n\n";
			}
			else
			{
				BuildCommand = "build=bash $$unrealRootPath/Engine/Build/BatchFiles/Linux/Build.sh\n";
			}

			string UnrealRootPath = UnrealBuildTool.RootDirectory.FullName;

			string FileName = MasterProjectName + ".pro";

			string QMakeSourcePriFileName = MasterProjectName + "Source.pri";
			string QMakeHeaderPriFileName = MasterProjectName + "Header.pri";
			string QMakeConfigPriFileName = MasterProjectName + "Config.pri";

			StringBuilder QMakeFileContent = new StringBuilder();

			StringBuilder QMakeSourcePriFileContent = new StringBuilder();
			StringBuilder QMakeHeaderPriFileContent = new StringBuilder();
			StringBuilder QMakeConfigPriFileContent = new StringBuilder();

			string QMakeSectionEnd = " \n\n";

			string QMakeSourceFilesList = "SOURCES += \\ \n";
			string QMakeHeaderFilesList = "HEADERS += \\ \n";
			string QMakeConfigFilesList = "OTHER_FILES += \\ \n";
			string QMakeTargetList = "QMAKE_EXTRA_TARGETS += \\ \n";

			if (!String.IsNullOrEmpty(GameProjectName))
			{
				GameProjectRootPath = GameProjectName + "RootPath=" + GameProjectPath + "\n\n";
			}

			QMakeFileContent.Append(
				"# UnrealEngine.pro generated by QMakefileGenerator.cs\n" +
				"# *DO NOT EDIT*\n\n" +
				"TEMPLATE = aux\n" +
				"CONFIG += c++14\n" +
				"CONFIG -= console\n" +
				"CONFIG -= app_bundle\n" +
				"CONFIG -= qt\n\n" +
				"TARGET = UE4 \n\n" +
				"unrealRootPath=" + UnrealRootPath + "\n" +
				GameProjectRootPath +
				QMakeGameProjectFile +
				BuildCommand +
				"args=$(ARGS)\n\n" +
				"include(" + QMakeSourcePriFileName + ")\n" +
				"include(" + QMakeHeaderPriFileName + ")\n" +
				"include(" + QMakeConfigPriFileName + ")\n" +
				"include(" + QMakeIncludesFileName + ")\n" +
				"include(" + QMakeDefinesFileName + ")\n\n"
			);

			// Create SourceFiles, HeaderFiles, and ConfigFiles sections.
			List<FileReference> AllModuleFiles = DiscoverModules(FindGameProjects());
			foreach (FileReference CurModuleFile in AllModuleFiles)
			{
				List<FileReference> FoundFiles = SourceFileSearch.FindModuleSourceFiles(CurModuleFile);
				foreach (FileReference CurSourceFile in FoundFiles)
				{
					string SourceFileRelativeToRoot = CurSourceFile.MakeRelativeTo(UnrealBuildTool.EngineDirectory);
					// Exclude some directories that we don't compile (note that we still want Windows/Mac etc for code navigation)
					if (!SourceFileRelativeToRoot.Contains("Source/ThirdParty/"))
					{
						if (SourceFileRelativeToRoot.EndsWith(".cpp"))
						{
							if (!SourceFileRelativeToRoot.StartsWith("..") && !Path.IsPathRooted(SourceFileRelativeToRoot))
							{
								QMakeSourceFilesList += ("\t\"" + "$$unrealRootPath/Engine/" + SourceFileRelativeToRoot + "\" \\\n");
							}
							else
							{
								if (String.IsNullOrEmpty(GameProjectName))
								{
									QMakeSourceFilesList += ("\t\"" + SourceFileRelativeToRoot.Substring(3) + "\" \\\n");
								}
								else
								{
									QMakeSourceFilesList += ("\t\"$$" + GameProjectName + "RootPath/" + Utils.MakePathRelativeTo(CurSourceFile.FullName, GameProjectPath) + "\" \\\n");
								}
							}
						}
						if (SourceFileRelativeToRoot.EndsWith(".h"))
						{
							if (!SourceFileRelativeToRoot.StartsWith("..") && !Path.IsPathRooted(SourceFileRelativeToRoot))
							{
								// SourceFileRelativeToRoot = "Engine/" + SourceFileRelativeToRoot;
								QMakeHeaderFilesList += ("\t\"" + "$$unrealRootPath/Engine/" + SourceFileRelativeToRoot + "\" \\\n");
							}
							else
							{
								if (String.IsNullOrEmpty(GameProjectName))
								{
									// SourceFileRelativeToRoot = SourceFileRelativeToRoot.Substring (3);
									QMakeHeaderFilesList += ("\t\"" + SourceFileRelativeToRoot.Substring(3) + "\" \\\n");
								}
								else
								{
									QMakeHeaderFilesList += ("\t\"$$" + GameProjectName + "RootPath/" + Utils.MakePathRelativeTo(CurSourceFile.FullName, GameProjectPath) + "\" \\\n");
								}
							}
						}
						if (SourceFileRelativeToRoot.EndsWith(".cs"))
						{
							if (!SourceFileRelativeToRoot.StartsWith("..") && !Path.IsPathRooted(SourceFileRelativeToRoot))
							{
								// SourceFileRelativeToRoot = "Engine/" + SourceFileRelativeToRoot;
								QMakeConfigFilesList += ("\t\"" + "$$unrealRootPath/Engine/" + SourceFileRelativeToRoot + "\" \\\n");

							}
							else
							{
								if (String.IsNullOrEmpty(GameProjectName))
								{
									// SourceFileRelativeToRoot = SourceFileRelativeToRoot.Substring (3);
									QMakeConfigFilesList += ("\t\"" + SourceFileRelativeToRoot.Substring(3) + "\" \\\n");
								}
								else
								{
									QMakeConfigFilesList += ("\t\"$$" + GameProjectName + "RootPath/" + Utils.MakePathRelativeTo(CurSourceFile.FullName, GameProjectPath) + "\" \\\n");
								}
							}
						}
					}
				}

			}

			// Add section end to section strings;
			QMakeSourceFilesList += QMakeSectionEnd;
			QMakeHeaderFilesList += QMakeSectionEnd;
			QMakeConfigFilesList += QMakeSectionEnd;

			// Append sections to the QMakeLists.txt file
			QMakeSourcePriFileContent.Append(QMakeSourceFilesList);
			QMakeHeaderPriFileContent.Append(QMakeHeaderFilesList);
			QMakeConfigPriFileContent.Append(QMakeConfigFilesList);

			string QMakeProjectCmdArg = "";

			foreach (ProjectFile Project in GeneratedProjectFiles)
			{
				foreach (ProjectTarget TargetFile in Project.ProjectTargets)
				{
					if (TargetFile.TargetFilePath == null)
					{
						continue;
					}

					string TargetName = TargetFile.TargetFilePath.GetFileNameWithoutAnyExtensions();		// Remove both ".cs" and ".

					foreach (UnrealTargetConfiguration CurConfiguration in Enum.GetValues(typeof(UnrealTargetConfiguration)))
					{
						if (CurConfiguration != UnrealTargetConfiguration.Unknown && CurConfiguration != UnrealTargetConfiguration.Development)
						{
							if (InstalledPlatformInfo.IsValidConfiguration(CurConfiguration, EProjectType.Code))
							{

								if (TargetName == GameProjectName || TargetName == (GameProjectName + "Editor"))
								{
									QMakeProjectCmdArg = " -project=\"\\\"$$gameProjectFile\\\"\"";
								}
								string ConfName = Enum.GetName(typeof(UnrealTargetConfiguration), CurConfiguration);
								QMakeFileContent.Append(String.Format("{0}-Linux-{1}.commands = $$build {0} Linux {1} {2} $$args\n", TargetName, ConfName, QMakeProjectCmdArg));
								QMakeTargetList += "\t" + TargetName + "-Linux-" + ConfName + " \\\n"; // , TargetName, ConfName);
							}
						}
					}

					if (TargetName == GameProjectName || TargetName == (GameProjectName + "Editor"))
					{
						QMakeProjectCmdArg = " -project=\"\\\"$$gameProjectFile\\\"\"";
					}

					QMakeFileContent.Append(String.Format("{0}.commands = $$build {0} Linux Development {1} $$args\n\n", TargetName, QMakeProjectCmdArg));
					QMakeTargetList += "\t" + TargetName + " \\\n";
				}
			}

			QMakeFileContent.Append(QMakeTargetList.TrimEnd('\\'));

			string FullFileName = Path.Combine(MasterProjectPath.FullName, FileName);

			string FullQMakeDefinesFileName = Path.Combine(MasterProjectPath.FullName, QMakeDefinesFileName);
			string FullQMakeIncludesFileName = Path.Combine(MasterProjectPath.FullName, QMakeIncludesFileName);
			string FullQMakeSourcePriFileName = Path.Combine(MasterProjectPath.FullName, QMakeSourcePriFileName);
			string FullQMakeHeaderPriFileName = Path.Combine(MasterProjectPath.FullName, QMakeHeaderPriFileName);
			string FullQMakeConfigPriFileName = Path.Combine(MasterProjectPath.FullName, QMakeConfigPriFileName);

			WriteFileIfChanged(FullQMakeDefinesFileName, QMakeDefinesPriFileContent.ToString());
			WriteFileIfChanged(FullQMakeIncludesFileName, QMakeIncludesPriFileContent.ToString());
			WriteFileIfChanged(FullQMakeSourcePriFileName, QMakeSourcePriFileContent.ToString());

			WriteFileIfChanged(FullQMakeHeaderPriFileName, QMakeHeaderPriFileContent.ToString());
			WriteFileIfChanged(FullQMakeConfigPriFileName, QMakeConfigPriFileContent.ToString());

			return WriteFileIfChanged(FullFileName, QMakeFileContent.ToString());
		}

		/// ProjectFileGenerator interface
		//protected override bool WriteMasterProjectFile( ProjectFile UBTProject )
		protected override bool WriteProjectFiles()
		{
			return WriteQMakePro();
		}

		/// ProjectFileGenerator interface
		public override MasterProjectFolder AllocateMasterProjectFolder(ProjectFileGenerator InitOwnerProjectFileGenerator, string InitFolderName)
		{
			return new QMakefileFolder(InitOwnerProjectFileGenerator, InitFolderName);
		}

		/// ProjectFileGenerator interface
		/// <summary>
		/// Allocates a generator-specific project file object
		/// </summary>
		/// <param name="InitFilePath">Path to the project file</param>
		/// <returns>The newly allocated project file object</returns>
		protected override ProjectFile AllocateProjectFile(FileReference InitFilePath)
		{
			return new QMakefileProjectFile(InitFilePath);
		}

		/// ProjectFileGenerator interface
		public override void CleanProjectFiles(DirectoryReference InMasterProjectDirectory, string InMasterProjectName, DirectoryReference InIntermediateProjectFilesDirectory)
		{
		}
	}
}
